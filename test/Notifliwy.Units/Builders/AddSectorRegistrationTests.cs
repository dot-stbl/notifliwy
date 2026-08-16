using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notifliwy.Builders;
using Notifliwy.Config;
using Notifliwy.Config.Interfaces;
using Notifliwy.Config.Internals;
using Notifliwy.Connectors;
using Notifliwy.Contexts.Interfaces;
using Notifliwy.Dependency;
using Notifliwy.Exporters.Interfaces;
using Notifliwy.Exceptions;
using Notifliwy.Graph;
using Notifliwy.Graph.Interfaces;
using Notifliwy.Graph.Internals;
using Notifliwy.Mapper.Interfaces;
using Notifliwy.Transform.Interfaces;
using Notifliwy.Units.Graph;
using Shouldly;
using Xunit;

namespace Notifliwy.Units.Builders;

/// <summary>
/// Unit tests for the 3.2 registration surface: config-class sectors,
/// inline graph sectors and removal of the 3.1 fluent API.
/// </summary>
public class AddSectorRegistrationTests
{
    private sealed class MarkerDependency
    {
        public Guid Identity { get; } = Guid.NewGuid();
    }

    private sealed class RegistrationNotification
    {
        public int Value { get; set; }
    }

    private sealed class RegistrationEvent
    {
        public int Value { get; init; }
    }

    private sealed class DoublerMapper : INotificationMapper<RegistrationNotification, RegistrationEvent>
    {
        public ValueTask<RegistrationNotification> ConvertAsync(
            RegistrationEvent inputEvent,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RegistrationNotification { Value = inputEvent.Value * 2 });
        }
    }

    private sealed class CollectionExporter(List<RegistrationNotification> exported) : INotificationExporter<RegistrationNotification>
    {
        public ValueTask ThrowAsync(
            RegistrationNotification notification,
            CancellationToken cancellationToken = default)
        {
            exported.Add(notification);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingExporter : INotificationExporter<RegistrationNotification>
    {
        public ValueTask ThrowAsync(
            RegistrationNotification notification,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("branch failed");
        }
    }

    /// <summary>
    /// Config class taking a constructor dependency to prove DI-based instantiation.
    /// </summary>
    private sealed class ValidConfig(MarkerDependency marker) : INotificationSectorConfig<RegistrationNotification, RegistrationEvent>
    {
        public MarkerDependency ResolvedMarker => marker;

        public void Configure(ISectorGraphBuilder<RegistrationNotification, RegistrationEvent> graph)
        {
            graph
                .Map<DoublerMapper>()
                .Export<CollectionExporter>();
        }
    }

    private sealed class BestEffortConfig : INotificationSectorConfig<RegistrationNotification, RegistrationEvent>
    {
        public BranchPolicy? DefaultBranchPolicy => BranchPolicy.BestEffort;

        public void Configure(ISectorGraphBuilder<RegistrationNotification, RegistrationEvent> graph)
        {
            graph
                .Map((inputEvent, cancellationToken) =>
                    ValueTask.FromResult(new RegistrationNotification { Value = inputEvent.Value }))
                .Branch(
                    branch => branch.Export<ThrowingExporter>(),
                    branch => branch.Export<CollectionExporter>());
        }
    }

    private sealed class FailFastConfig : INotificationSectorConfig<RegistrationNotification, RegistrationEvent>
    {
        public void Configure(ISectorGraphBuilder<RegistrationNotification, RegistrationEvent> graph)
        {
            graph
                .Map((inputEvent, cancellationToken) =>
                    ValueTask.FromResult(new RegistrationNotification { Value = inputEvent.Value }))
                .Branch(
                    branch => branch.Export<ThrowingExporter>(),
                    branch => branch.Export<CollectionExporter>());
        }
    }

    private sealed class CompiledConfig : INotificationSectorConfig<RegistrationNotification, RegistrationEvent>
    {
        public SectorExecution Execution => SectorExecution.Compiled;

        public void Configure(ISectorGraphBuilder<RegistrationNotification, RegistrationEvent> graph)
        {
            graph.Map<DoublerMapper>().Export<CollectionExporter>();
        }
    }

    private sealed class BrokenConfig : INotificationSectorConfig<RegistrationNotification, RegistrationEvent>
    {
        public void Configure(ISectorGraphBuilder<RegistrationNotification, RegistrationEvent> graph)
        {
            // Map node missing on purpose: plan validation must reject this sector
            graph.Export<CollectionExporter>();
        }
    }

    private sealed class NotAConfig
    {
    }

    [Fact]
    public void AddSector_ByConfig_RegistersSectorConnectorAndResolvableConfigWithCtorDeps()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<MarkerDependency>();

        // Act
        services.AddNotifliwyServer(serverBuilder => serverBuilder.AddSector<ValidConfig>());
        var serviceProvider = services.BuildServiceProvider();

        // Assert - sector is registered and the connector hosted service is wired
        // for the bound event type (descriptor scan: resolving it would require an input pipe)
        var sector = serviceProvider.GetService<INotificationSector<RegistrationEvent>>();
        sector.ShouldNotBeNull();

        services
            .Any(descriptor => descriptor.ImplementationType == typeof(NotificationConnector<RegistrationEvent>))
            .ShouldBeTrue();

        // Assert - config class itself is resolvable from DI with its constructor dependency
        var config = serviceProvider.GetRequiredService<ValidConfig>();
        config.ResolvedMarker.ShouldBe(serviceProvider.GetRequiredService<MarkerDependency>());
    }

    [Fact]
    public async Task AddSector_Inline_MatchesConfigClassBehaviour()
    {
        // Arrange - inline sector with the same graph shape as <see cref="ValidConfig"/>
        var exportedInline = new List<RegistrationNotification>();
        var exportedConfig = new List<RegistrationNotification>();

        var inlineServices = new ServiceCollection();
        inlineServices.AddLogging();
        inlineServices.AddSingleton(new CollectionExporter(exportedInline));
        inlineServices.AddNotifliwyServer(serverBuilder => serverBuilder
            .AddSector<RegistrationNotification, RegistrationEvent>(graph => graph
                .Map<DoublerMapper>()
                .Export<CollectionExporter>()));

        var configServices = new ServiceCollection();
        configServices.AddLogging();
        configServices.AddSingleton<MarkerDependency>();
        configServices.AddSingleton(new CollectionExporter(exportedConfig));
        configServices.AddNotifliwyServer(serverBuilder => serverBuilder
            .AddSector<ValidConfig>());

        var inputEvent = new RegistrationEvent { Value = 21 };

        // Act
        await using (var provider = inlineServices.BuildServiceProvider())
        {
            await provider
                .GetRequiredService<INotificationSector<RegistrationEvent>>()
                .PassThroughAsync(inputEvent);
        }

        await using (var provider = configServices.BuildServiceProvider())
        {
            await provider
                .GetRequiredService<INotificationSector<RegistrationEvent>>()
                .PassThroughAsync(inputEvent);
        }

        // Assert - both registration styles produce the same observable result
        exportedInline.Count.ShouldBe(1);
        exportedConfig.Count.ShouldBe(1);
        exportedInline[0].Value.ShouldBe(42);
        exportedConfig[0].Value.ShouldBe(42);
    }

    [Fact]
    public void AddSector_ConfigDefaults_AreAutoExecutionAndNoBranchPolicy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<MarkerDependency>();

        // Act
        services.AddNotifliwyServer(serverBuilder => serverBuilder.AddSector<ValidConfig>());
        var serviceProvider = services.BuildServiceProvider();

        var plan = serviceProvider.GetRequiredService<SectorGraphPlan<RegistrationNotification, RegistrationEvent>>();

        // Assert
        plan.Execution.ShouldBe(SectorExecution.Auto);
        plan.DefaultBranchPolicy.ShouldBeNull();
    }

    [Fact]
    public void AddSector_CompiledExecution_WithUnprovableNode_FailsFastAtSectorResolution()
    {
        // Arrange - DoublerMapper is parameterless (compile-safe), but CollectionExporter
        // is neither registered nor parameterless, so the graph cannot be proven compile-safe
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddNotifliwyServer(serverBuilder => serverBuilder.AddSector<CompiledConfig>());

        using var serviceProvider = services.BuildServiceProvider();

        // Act + Assert - Compiled mode no longer falls back: the captive-dependency
        // guard throws when the sector (and with it the executor) is first resolved
        var exception = Should.Throw<SectorCaptiveDependencyException>(
            () => serviceProvider.GetRequiredService<INotificationSector<RegistrationEvent>>());

        exception.Message.ShouldContain("RegistrationNotification/RegistrationEvent");
        exception.Message.ShouldContain(nameof(CollectionExporter));
    }

    [Fact]
    public async Task AddSector_CompiledExecution_WithCompileSafeNodes_RunsOnCompiledPath()
    {
        // Arrange - the exporter instance is singleton-registered, so every node is
        // compile-safe and the compiled path activates instead of throwing
        var exported = new List<RegistrationNotification>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new CollectionExporter(exported));

        services.AddNotifliwyServer(serverBuilder => serverBuilder.AddSector<CompiledConfig>());

        using var serviceProvider = services.BuildServiceProvider();

        var executor = serviceProvider
            .GetRequiredService<SectorGraphExecutor<RegistrationNotification, RegistrationEvent>>();

        executor.Decision.Mode.ShouldBe(SectorExecutionMode.Compiled);

        // Act
        await executor.ExecuteAsync(new RegistrationEvent { Value = 21 });

        // Assert
        exported.Count.ShouldBe(1);
        exported[0].Value.ShouldBe(42);
    }

    [Fact]
    public async Task AddSector_SectorLevelDefaultBranchPolicy_ReachesExecutorAsBestEffort()
    {
        // Arrange
        var exported = new List<RegistrationNotification>();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new CollectionExporter(exported));

        services.AddNotifliwyServer(serverBuilder => serverBuilder.AddSector<BestEffortConfig>());
        var serviceProvider = services.BuildServiceProvider();

        var executor = serviceProvider
            .GetRequiredService<SectorGraphExecutor<RegistrationNotification, RegistrationEvent>>();

        // Act - the failing branch is skipped, the surviving branch still exports
        await executor.ExecuteAsync(new RegistrationEvent { Value = 7 });

        // Assert
        exported.Count.ShouldBe(1);
        exported[0].Value.ShouldBe(7);
    }

    [Fact]
    public async Task AddSector_WithoutSectorPolicy_FanOutStaysFailFast()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddNotifliwyServer(serverBuilder => serverBuilder.AddSector<FailFastConfig>());
        var serviceProvider = services.BuildServiceProvider();

        var executor = serviceProvider
            .GetRequiredService<SectorGraphExecutor<RegistrationNotification, RegistrationEvent>>();

        // Act + Assert - no sector default: FailFast rethrows the branch fault
        await Should.ThrowAsync<InvalidOperationException>(
            async () => await executor.ExecuteAsync(new RegistrationEvent { Value = 7 }));
    }

    [Fact]
    public void AddSector_InvalidConfigGraph_FailsAtSectorResolution()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddNotifliwyServer(serverBuilder => serverBuilder.AddSector<BrokenConfig>());

        // Act + Assert - plan materializes when the sector is resolved (connector
        // startup in a hosted app), surfacing the validation exception
        using var serviceProvider = services.BuildServiceProvider();

        Should.Throw<SectorGraphValidationException>(() =>
            serviceProvider.GetRequiredService<INotificationSector<RegistrationEvent>>());
    }

    [Fact]
    public void AddSector_ClassWithoutConfigContract_ThrowsAtRegistration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act + Assert
        Should.Throw<InvalidOperationException>(() =>
            services.AddNotifliwyServer(serverBuilder => serverBuilder.AddSector<NotAConfig>()));
    }

    [Fact]
    public void RemovedFluent3_1_Api_IsAbsentFromAssembly()
    {
        // Arrange
        var assembly = typeof(NotificationServerBuilder).Assembly;

        // Act
        var typeNames = assembly
            .GetTypes()
            .Select(type => type.Name)
            .ToArray();

        // Assert - the 3.1 fluent sector API is gone
        typeNames.ShouldNotContain("NotificationSectorBuilder");
        typeNames.ShouldNotContain("INotificationSectorBuilder");
        typeNames.ShouldNotContain("INotificationPipeline");
        typeNames.ShouldNotContain("NotificationPipeline");
        typeNames.ShouldNotContain("PipelineBuilder");
        typeNames.ShouldNotContain("SectorBlock");
        typeNames.ShouldNotContain("ISectorBlock");
        typeNames.ShouldNotContain("MultiplyServiceInstance");
        typeNames.ShouldNotContain("NotificationConditionProcessor");

        // AddNotification entry point itself is gone
        typeof(NotificationServerBuilder)
            .GetMethods()
            .ShouldNotContain(method => method.Name == "AddNotification");
    }

    [Fact]
    public async Task AddSectorsFromAssembly_DiscoversPublicConfigs_AndProcessesEvents()
    {
        // Arrange
        AssemblyScanSinks.Exports.Clear();

        var services = new ServiceCollection();
        services.AddLogging();

        // Act - opt-in reflection fallback over this test assembly
        services.AddNotifliwyServer(serverBuilder => serverBuilder
            .AddSectorsFromAssembly(typeof(AssemblyScanConfig).Assembly));

        await using var serviceProvider = services.BuildServiceProvider();

        await serviceProvider
            .GetRequiredService<INotificationSector<ScanEvent>>()
            .PassThroughAsync(new ScanEvent { Value = 5 });

        // Assert - the public config was discovered, registered and executed
        AssemblyScanSinks.Exports.Count.ShouldBe(1);
        AssemblyScanSinks.Exports.Single().Value.ShouldBe(15);
    }

    [Fact]
    public void AddSectorsFromAssembly_LogsReflectionFallbackWarningAtStartup()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        var spy = new SpyLogger<SectorAssemblyScanNotice>();
        services.AddSingleton<ILogger<SectorAssemblyScanNotice>>(spy);

        services.AddNotifliwyServer(serverBuilder => serverBuilder
            .AddSectorsFromAssembly(typeof(AssemblyScanConfig).Assembly));

        using var serviceProvider = services.BuildServiceProvider();

        // Act - resolving the sector constructs the executor, which forces the
        // one-shot assembly-scan notice and its warning
        _ = serviceProvider.GetRequiredService<INotificationSector<ScanEvent>>();

        // Assert
        spy.Entries.ShouldContain(entry =>
            entry.Level == LogLevel.Warning
            && entry.Message.Contains("reflection fallback"));
    }
}
