using System;
using System.Linq;
using System.Text;

namespace Notifliwy.Extensions;

/// <summary>
/// Extensions converter class to more cases
/// </summary>
internal static class StringExtensions
{
    private static readonly char[] Delimiters = [' ', '-', '_', '.'];

    private static string SymbolsPipe(
        string source,
        char mainDelimiter,
        Func<char, bool, char[]> newWordSymbolHandler)
    {
        var builder = new StringBuilder();

        var disableFrontDelimiter = true;
        var nextSymbolStartsNewWord = true;
        
        foreach (var symbol in source)
        {
            if (Delimiters.Contains(symbol))
            {
                if (symbol == mainDelimiter)
                {
                    builder.Append(symbol);
                    disableFrontDelimiter = true;
                }

                nextSymbolStartsNewWord = true;
            }
            else if (!char.IsLetterOrDigit(symbol))
            {
                builder.Append(symbol);
                disableFrontDelimiter = true;
                nextSymbolStartsNewWord = true;
            }
            else
            {
                if (nextSymbolStartsNewWord || char.IsUpper(symbol))
                {
                    builder.Append(newWordSymbolHandler(symbol, disableFrontDelimiter));
                    disableFrontDelimiter = false;
                    nextSymbolStartsNewWord = false;
                }
                else
                {
                    builder.Append(symbol);
                }
            }
        }

        return builder.ToString();
    }
    
    /// <summary>
    /// Convert <paramref name="source"/> to <c>DotCase</c>
    /// </summary>
    /// <example>example.word</example>
    /// <param name="source">compute string</param>
    /// <exception cref="ArgumentNullException">if null</exception>
    public static string ToDotCase(this string source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return SymbolsPipe(
            source,
            mainDelimiter: '.',
            newWordSymbolHandler: (s, disableFrontDelimiter) 
                => disableFrontDelimiter ? [char.ToLowerInvariant(s)] : ['.', char.ToLowerInvariant(s)]);
    }
    
    /// <summary>
    /// Convert <paramref name="source"/> to <c>CamelCase</c>
    /// </summary>
    /// <example>exampleWord</example>
    /// <param name="source">compute string</param>
    /// <exception cref="ArgumentNullException">if null</exception>
    public static string ToCamelCase(this string source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return SymbolsPipe(
            source,
            mainDelimiter: '\0',
            newWordSymbolHandler: (s, disableFrontDelimiter) 
                => disableFrontDelimiter ? [char.ToLowerInvariant(s)] : [char.ToUpperInvariant(s)]);
    }
    
    /// <summary>
    /// Convert <paramref name="source"/> to <c>KebabCase</c>
    /// </summary>
    /// <example>example-word</example>
    /// <param name="source">compute string</param>
    /// <exception cref="ArgumentNullException">if null</exception>
    public static string ToKebabCase(this string source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return SymbolsPipe(
            source,
            mainDelimiter: '-',
            newWordSymbolHandler: (s, disableFrontDelimiter) 
                => disableFrontDelimiter ? [char.ToLowerInvariant(s)] : ['-', char.ToLowerInvariant(s)]);
    }
    
    /// <summary>
    /// Convert <paramref name="source"/> to <c>SnakeCase</c>
    /// </summary>
    /// <example>example_word</example>
    /// <param name="source">compute string</param>
    /// <exception cref="ArgumentNullException">if null</exception>
    public static string ToSnakeCase(this string source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return SymbolsPipe(
            source,
            mainDelimiter: '_',
            newWordSymbolHandler: (s, disableFrontDelimiter) 
                => disableFrontDelimiter ? [char.ToLowerInvariant(s)] : ['_', char.ToLowerInvariant(s)]);
    }
    
    /// <summary>
    /// Convert <paramref name="source"/> to <c>PascalCase</c>
    /// </summary>
    /// <example>ExampleWord</example>
    /// <param name="source">compute string</param>
    /// <exception cref="ArgumentNullException">if null</exception>
    public static string ToPascalCase(this string source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return SymbolsPipe(
            source,
            mainDelimiter: '\0',
            newWordSymbolHandler: (s, _) => [char.ToUpperInvariant(s)]);
    }
    
    /// <summary>
    /// Convert <paramref name="source"/> to <c>TrainCase</c>
    /// </summary>
    /// <example>Example-Word</example>
    /// <param name="source">compute string</param>
    /// <exception cref="ArgumentNullException">if null</exception>
    public static string ToTrainCase(this string source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        return SymbolsPipe(
            source,
            mainDelimiter: '-',
            newWordSymbolHandler: (s, disableFrontDelimiter) 
                => disableFrontDelimiter ? [char.ToUpperInvariant(s)] : ['-', char.ToUpperInvariant(s)]);
    }
}