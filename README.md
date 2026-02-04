# VBT to C# Converter (Starter AI)

This is a lightweight, rules-based "AI" converter that translates common VBT patterns into C#. It is intentionally small, deterministic, and easy to extend with more rules as you learn C#.

## What it does
- Converts common VBT keywords (If/Else/End If, For/Next, While/End While).
- Converts `Dim` declarations to C# types (`int`, `double`, `string`, etc.).
- Converts `Sub` and `Function` signatures to C# methods.
- Converts `True/False`, `And/Or/Not`, `Nothing`, and `&` concatenation.

## Run
```bash
# Convert a file
 dotnet run -- input.vbt output.cs

# Convert from stdin
 cat input.vbt | dotnet run -- --stdin output.cs
```

## Example
**Input (VBT):**
```
Sub Example(name As String)
    Dim count As Integer
    If count > 0 Then
        Print name & "!"
    Else
        Print "empty"
    End If
End Sub
```

**Output (C#):**
```
static void Example(string name)
{
    int count;
    if (count > 0) {
        Print name + "!";
    } else {
        Print "empty";
    }
}
```

## Notes
- This tool focuses on structure; you may still need to adjust API equivalents depending on your target tester environment.
- Add new rules in `VbtToCSharpConverter.cs` to support more syntax.
