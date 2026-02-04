using System.Text;
using System.Text.RegularExpressions;

namespace DriverDrowsinessDetection;

public sealed class VbtToCSharpConverter
{
    private static readonly IReadOnlyList<ConversionRule> Rules = new List<ConversionRule>
    {
        new(
            name: "Comments",
            pattern: new Regex("'(?<comment>.*)$", RegexOptions.Compiled),
            replacement: match => $"//{match.Groups["comment"].Value}")
        ,
        new(
            name: "Boolean",
            pattern: new Regex("\\b(True|False)\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            replacement: match => match.Value.Equals("True", StringComparison.OrdinalIgnoreCase) ? "true" : "false")
        ,
        new(
            name: "If",
            pattern: new Regex("^\\s*If\\s+(?<cond>.+?)\\s*(Then)?\\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            replacement: match => $"if ({match.Groups["cond"].Value}) {{")
        ,
        new(
            name: "ElseIf",
            pattern: new Regex("^\\s*ElseIf\\s+(?<cond>.+?)\\s*(Then)?\\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            replacement: match => $"}} else if ({match.Groups["cond"].Value}) {{")
        ,
        new(
            name: "Else",
            pattern: new Regex("^\\s*Else\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            replacement: _ => "} else {")
        ,
        new(
            name: "EndIf",
            pattern: new Regex("^\\s*End If\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            replacement: _ => "}")
        ,
        new(
            name: "While",
            pattern: new Regex("^\\s*While\\s+(?<cond>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            replacement: match => $"while ({match.Groups["cond"].Value}) {{")
        ,
        new(
            name: "EndWhile",
            pattern: new Regex("^\\s*End While\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            replacement: _ => "}")
        ,
        new(
            name: "For",
            pattern: new Regex("^\\s*For\\s+(?<var>\\w+)\\s*=\\s*(?<start>[^\\s]+)\\s+To\\s+(?<end>[^\\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            replacement: match => $"for (var {match.Groups["var"].Value} = {match.Groups["start"].Value}; {match.Groups["var"].Value} <= {match.Groups["end"].Value}; {match.Groups["var"].Value}++) {{")
        ,
        new(
            name: "Next",
            pattern: new Regex("^\\s*Next\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            replacement: _ => "}")
        ,
        new(
            name: "Dim",
            pattern: new Regex("^\\s*Dim\\s+(?<name>\\w+)(\\s+As\\s+(?<type>\\w+))?", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            replacement: match =>
            {
                var type = match.Groups["type"].Success
                    ? MapType(match.Groups["type"].Value)
                    : "var";
                return $"{type} {match.Groups["name"].Value}";
            })
        ,
        new(
            name: "Function",
            pattern: new Regex("^\\s*Function\\s+(?<name>\\w+)\\((?<args>[^)]*)\\)\\s+As\\s+(?<type>\\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            replacement: match => $"static {MapType(match.Groups["type"].Value)} {match.Groups["name"].Value}({ConvertArgs(match.Groups["args"].Value)}) {{")
        ,
        new(
            name: "Sub",
            pattern: new Regex("^\\s*Sub\\s+(?<name>\\w+)\\((?<args>[^)]*)\\)", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            replacement: match => $"static void {match.Groups["name"].Value}({ConvertArgs(match.Groups["args"].Value)}) {{")
        ,
        new(
            name: "EndSub",
            pattern: new Regex("^\\s*End Sub\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            replacement: _ => "}")
        ,
        new(
            name: "EndFunction",
            pattern: new Regex("^\\s*End Function\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            replacement: _ => "}")
        ,
        new(
            name: "Return",
            pattern: new Regex("^\\s*Return\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            replacement: _ => "return")
        ,
        new(
            name: "Concat",
            pattern: new Regex("\\s*&\\s*", RegexOptions.Compiled),
            replacement: _ => " + ")
        ,
        new(
            name: "Nothing",
            pattern: new Regex("\\bNothing\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            replacement: _ => "null")
        ,
        new(
            name: "AndOr",
            pattern: new Regex("\\b(And|Or|Not)\\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            replacement: match => match.Value.Equals("And", StringComparison.OrdinalIgnoreCase)
                ? "&&"
                : match.Value.Equals("Or", StringComparison.OrdinalIgnoreCase)
                    ? "||"
                    : "!")
    };

    public string Convert(string vbtSource)
    {
        var builder = new StringBuilder();
        var indentLevel = 0;

        foreach (var line in vbtSource.Replace("\r", string.Empty).Split('\n'))
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                builder.AppendLine();
                continue;
            }

            var convertedLine = ApplyRules(trimmedLine);
            convertedLine = EnsureSemicolon(convertedLine);
            if (convertedLine.TrimStart().StartsWith("}", StringComparison.Ordinal) && indentLevel > 0)
            {
                indentLevel--;
            }

            builder.AppendLine($"{new string(' ', indentLevel * 4)}{convertedLine}");

            if (ShouldIncreaseIndent(convertedLine))
            {
                indentLevel++;
            }
        }

        return builder.ToString();
    }

    private static string ApplyRules(string line)
    {
        var result = line;
        foreach (var rule in Rules)
        {
            result = rule.Pattern.Replace(result, rule.Replacement);
        }

        return result.TrimEnd();
    }

    private static bool ShouldIncreaseIndent(string convertedLine)
    {
        var line = convertedLine.Trim();
        return line.EndsWith("{", StringComparison.Ordinal);
    }

    private static string EnsureSemicolon(string convertedLine)
    {
        var line = convertedLine.Trim();
        if (string.IsNullOrWhiteSpace(line)
            || line.EndsWith("{", StringComparison.Ordinal)
            || line.EndsWith("}", StringComparison.Ordinal)
            || line.StartsWith("if ", StringComparison.Ordinal)
            || line.StartsWith("else if ", StringComparison.Ordinal)
            || line.StartsWith("else", StringComparison.Ordinal)
            || line.StartsWith("for ", StringComparison.Ordinal)
            || line.StartsWith("while ", StringComparison.Ordinal))
        {
            return convertedLine;
        }

        return convertedLine.EndsWith(";", StringComparison.Ordinal) ? convertedLine : $"{convertedLine};";
    }

    private static string ConvertArgs(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            return string.Empty;
        }

        var convertedArgs = args.Split(',')
            .Select(arg => arg.Trim())
            .Select(arg =>
            {
                var parts = arg.Split(new[] { " As " }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 2)
                {
                    return $"{MapType(parts[1])} {parts[0]}";
                }

                return $"object {parts[0]}";
            });

        return string.Join(", ", convertedArgs);
    }

    private static string MapType(string vbtType) => vbtType.ToLowerInvariant() switch
    {
        "integer" => "int",
        "double" => "double",
        "single" => "float",
        "string" => "string",
        "boolean" => "bool",
        "date" => "DateTime",
        _ => "object"
    };
}
