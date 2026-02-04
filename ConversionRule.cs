using System.Text.RegularExpressions;

namespace DriverDrowsinessDetection;

public sealed record ConversionRule(string Name, Regex Pattern, MatchEvaluator Replacement);
