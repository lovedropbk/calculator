using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FinancialCalculator.WinUI3.Services
{
    /// <summary>
    /// Sanitizes date/time values in complex objects to ensure they are in UTC ISO 8601 format
    /// </summary>
    public static class DateTimeSanitizer
    {
        // Regex to match datetime strings with timezone offsets
        private static readonly Regex DateTimeWithTimezoneRegex = new Regex(
            @"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?[\+\-]\d{2}:\d{2}$",
            RegexOptions.Compiled);

        /// <summary>
        /// Recursively sanitizes all date/time values in a dictionary to UTC format
        /// </summary>
        public static Dictionary<string, object>? SanitizeDictionary(Dictionary<string, object>? dict)
        {
            if (dict == null) return null;

            var result = new Dictionary<string, object>();
            
            foreach (var kvp in dict)
            {
                result[kvp.Key] = SanitizeValue(kvp.Value);
            }
            
            return result;
        }

        /// <summary>
        /// Sanitizes a single value, handling strings, dictionaries, lists, and JsonElements
        /// </summary>
        private static object SanitizeValue(object value)
        {
            if (value == null) return null!;

            switch (value)
            {
                case string strValue:
                    return SanitizeDateTimeString(strValue);
                    
                case Dictionary<string, object> dictValue:
                    return SanitizeDictionary(dictValue)!;
                    
                case List<object> listValue:
                    var sanitizedList = new List<object>();
                    foreach (var item in listValue)
                    {
                        sanitizedList.Add(SanitizeValue(item));
                    }
                    return sanitizedList;
                    
                case JsonElement jsonElement:
                    return SanitizeJsonElement(jsonElement);
                    
                default:
                    return value;
            }
        }

        /// <summary>
        /// Sanitizes a JsonElement recursively
        /// </summary>
        private static object SanitizeJsonElement(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    var str = element.GetString() ?? string.Empty;
                    return SanitizeDateTimeString(str);
                    
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object>();
                    foreach (var prop in element.EnumerateObject())
                    {
                        dict[prop.Name] = SanitizeJsonElement(prop.Value);
                    }
                    return dict;
                    
                case JsonValueKind.Array:
                    var list = new List<object>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(SanitizeJsonElement(item));
                    }
                    return list;
                    
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intVal))
                        return intVal;
                    if (element.TryGetInt64(out var longVal))
                        return longVal;
                    return element.GetDouble();
                    
                case JsonValueKind.True:
                    return true;
                    
                case JsonValueKind.False:
                    return false;
                    
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                default:
                    return null!;
            }
        }

        /// <summary>
        /// Sanitizes a datetime string by converting it to UTC ISO 8601 format
        /// </summary>
        private static string SanitizeDateTimeString(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            // Check if this looks like a datetime with timezone offset
            if (DateTimeWithTimezoneRegex.IsMatch(value) || 
                value.Contains("T") && (value.Contains("+") || value.Contains("Z")))
            {
                try
                {
                    // Try to parse the datetime
                    if (DateTime.TryParse(value, out var dt))
                    {
                        // Convert to UTC and format in ISO 8601 with Z suffix
                        return dt.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'");
                    }
                }
                catch
                {
                    // If parsing fails, return the original value
                    Logger.Debug($"Failed to sanitize datetime string: {value}");
                }
            }

            return value;
        }
    }
}