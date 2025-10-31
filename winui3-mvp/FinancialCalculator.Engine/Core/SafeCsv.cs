using System;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace FinancialCalculator.Engine.Core
{
    /// <summary>
    /// SafeCsv centralizes CsvHelper configuration to provide:
    /// - InvariantCulture for numeric parsing
    /// - Robust header normalization and trimming
    /// - Suppression of MissingField and BadData exceptions
    /// - DetectDelimiter and IgnoreBlankLines
    /// - TypeConverterOptions for double/decimal with NumberStyles.Any
    /// Use this factory instead of constructing CsvReader directly.
    /// </summary>
    public static class SafeCsv
    {
        public static CsvReader Create(TextReader reader)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,     // Do not throw on missing fields
                BadDataFound = null,          // Ignore malformed tokens; callers can log if needed
                DetectDelimiter = true,       // Auto-detect common delimiters
                TrimOptions = TrimOptions.Trim,
                IgnoreBlankLines = true,
                PrepareHeaderForMatch = args =>
                {
                    // Normalize headers for resilient matching
                    var h = args.Header ?? string.Empty;
                    h = h.Trim();
                    h = h.Replace(" ", string.Empty)
                         .Replace("\t", string.Empty)
                         .Replace("_", string.Empty)
                         .Replace("-", string.Empty);
                    return h;
                }
            };

            var csv = new CsvReader(reader, config);

            // Culture-safe numeric conversions
            var numberOptions = new TypeConverterOptions
            {
                NumberStyles = NumberStyles.Any,
                CultureInfo = CultureInfo.InvariantCulture
            };

            csv.Context.TypeConverterOptionsCache.AddOptions<double>(numberOptions);
            csv.Context.TypeConverterOptionsCache.AddOptions<double?>(numberOptions);
            csv.Context.TypeConverterOptionsCache.AddOptions<decimal>(new TypeConverterOptions
            {
                NumberStyles = NumberStyles.Any,
                CultureInfo = CultureInfo.InvariantCulture
            });
            csv.Context.TypeConverterOptionsCache.AddOptions<decimal?>(new TypeConverterOptions
            {
                NumberStyles = NumberStyles.Any,
                CultureInfo = CultureInfo.InvariantCulture
            });

            return csv;
        }
    }
}