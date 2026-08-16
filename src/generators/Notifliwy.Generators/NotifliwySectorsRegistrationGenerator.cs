using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Notifliwy.Generators;

/// <summary>
/// Source generator (Generator B) emitting the sector registration extension for
/// assemblies marked with <c>[assembly: NotifliwySectors]</c>. It discovers every
/// accessible, concrete, closed <c>INotificationSectorConfig&lt;TNotification,TEvent&gt;</c>
/// implementation at compile time and generates a
/// <c>NotifliwySectorsRegistration.AddNotifliwySectors(this NotificationServerBuilder)</c>
/// extension issuing one direct <c>AddSector&lt;TConfig&gt;()</c> call per config —
/// zero runtime reflection.
/// </summary>
/// <remarks>
/// <para>
/// Skipped silently: abstract classes (bases), open generics, and classes not
/// visible to generated code (<see langword="private"/>/<see langword="protected"/>
/// or nested inside inaccessible types). The runtime reflection fallback is
/// <c>AddSectorsFromAssembly</c>, which logs a startup warning.
/// </para>
/// <para>
/// The generator is deliberately a <b>registration</b> generator only. Sector
/// graphs are described by <c>ISectorGraphBuilder</c> calls that execute at plan
/// materialization (runtime), so the compiled hot path is implemented by the
/// runtime plan compiler in the core library (<c>SectorGraphCompiler</c>), not by
/// source generation.
/// </para>
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class NotifliwySectorsRegistrationGenerator : IIncrementalGenerator
{
    private const string AttributeFullName = "Notifliwy.Config.NotifliwySectorsAttribute";

    private const string ConfigInterfaceFullName = "Notifliwy.Config.Interfaces.INotificationSectorConfig`2";

    private const string GeneratedFileName = "NotifliwySectorsRegistration.g.cs";

    private static readonly SymbolDisplayFormat FullyQualifiedFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.None,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configs = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (syntaxContext, _) => FindConfigCandidate(syntaxContext))
            .Where(static candidate => candidate is not null)
            .Select(static (candidate, _) => candidate!.Value)
            .Collect();

        var assemblyMarked = context.CompilationProvider
            .Select(static (compilation, _) => IsAssemblyMarked(compilation));

        context.RegisterSourceOutput(
            assemblyMarked.Combine(configs),
            static (productionContext, input) =>
            {
                if (input.Left)
                {
                    EmitRegistration(productionContext, input.Right);
                }
            });
    }

    /// <summary>
    /// Candidate config class as an equatable fully-qualified name, or
    /// <see langword="null"/> when the class is not a registerable config.
    /// </summary>
    private static ConfigCandidate? FindConfigCandidate(GeneratorSyntaxContext syntaxContext)
    {
        if (syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node) is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        var configInterface = syntaxContext.SemanticModel.Compilation
            .GetTypeByMetadataName(ConfigInterfaceFullName);

        if (configInterface is null)
        {
            // core library not referenced — nothing to discover
            return null;
        }

        var implementsConfig = typeSymbol
            .AllInterfaces
            .Any(candidate => candidate.OriginalDefinition
                .Equals(configInterface, SymbolEqualityComparer.Default));

        if (!implementsConfig
                || typeSymbol.IsAbstract
                || typeSymbol.IsGenericType
                || !IsVisibleToGeneratedCode(typeSymbol))
        {
            return null;
        }

        return new ConfigCandidate(typeSymbol.ToDisplayString(FullyQualifiedFormat));
    }

    private static bool IsAssemblyMarked(Compilation compilation)
    {
        return compilation.Assembly
            .GetAttributes()
            .Any(attribute => string.Equals(
                attribute.AttributeClass?.ToDisplayString(),
                AttributeFullName,
                StringComparison.Ordinal));
    }

    private static bool IsVisibleToGeneratedCode(INamedTypeSymbol typeSymbol)
    {
        for (var current = (INamedTypeSymbol?)typeSymbol; current is not null; current = current.ContainingType)
        {
            if (current.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
            {
                return false;
            }
        }

        return true;
    }

    private static void EmitRegistration(
        SourceProductionContext productionContext,
        ImmutableArray<ConfigCandidate> candidates)
    {
        var registrationCalls = candidates
            .Select(candidate => candidate.FullyQualifiedName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => $"            serverBuilder.AddSector<{name}>();")
            .ToArray();

        var source = new StringBuilder()
            .AppendLine("// <auto-generated />")
            .AppendLine("// Generated by the Notifliwy sector registration source generator from [assembly: NotifliwySectors].")
            .AppendLine()
            .AppendLine("namespace Notifliwy.Generated")
            .AppendLine("{")
            .AppendLine("    /// <summary>")
            .AppendLine("    /// Source-generated registration for every sector config class in this assembly - zero runtime reflection.")
            .AppendLine("    /// </summary>")
            .AppendLine("    public static class NotifliwySectorsRegistration")
            .AppendLine("    {")
            .AppendLine("        /// <summary>")
            .AppendLine("        /// Register all sector config classes discovered by the Notifliwy source generator.")
            .AppendLine("        /// </summary>")
            .AppendLine("        public static global::Notifliwy.Builders.NotificationServerBuilder AddNotifliwySectors(")
            .AppendLine("            this global::Notifliwy.Builders.NotificationServerBuilder serverBuilder)")
            .AppendLine("        {");

        foreach (var registrationCall in registrationCalls)
        {
            source.AppendLine(registrationCall);
        }

        source
            .AppendLine("            return serverBuilder;")
            .AppendLine("        }")
            .AppendLine("    }")
            .AppendLine("}")
            .AppendLine();

        productionContext.AddSource(
            GeneratedFileName,
            SourceText.From(source.ToString(), Encoding.UTF8));
    }

    /// <summary>
    /// Equatable candidate model: the fully-qualified (global::) name of one
    /// registerable sector config class.
    /// </summary>
    /// <param name="fullyQualifiedName">global-qualified display name of the config class</param>
    private readonly record struct ConfigCandidate(string FullyQualifiedName);
}
