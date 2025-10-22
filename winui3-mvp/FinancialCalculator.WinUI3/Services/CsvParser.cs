using System.Collections.Generic;
using System.Text;

namespace FinancialCalculator.WinUI3.Services;

public static class CsvParser
{
    public static string[] SplitCsvLine(string line)
    {
        var result = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    // Escaped quote
                    currentField.Append('\"');
                    i++;
                }
                else
                {
                    // Toggle quotes
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // End of field
                result.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        result.Add(currentField.ToString());

        return result.ToArray();
    }
}