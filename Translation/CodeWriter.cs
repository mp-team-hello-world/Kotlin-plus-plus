using System.Collections.Generic;
using System.Linq;

namespace Kotlin_plus_plus;

/// <summary>Накопитель строк с отступами — общий для всех backend'ов.</summary>
public sealed class CodeWriter
{
    private readonly List<string> _lines = new();
    private readonly string _indentUnit;
    private int _level;

    public CodeWriter(string indentUnit = "    ")
    {
        _indentUnit = indentUnit;
    }

    public IReadOnlyList<string> Lines => _lines;

    /// <summary>Сколько строк уже записано (удобно, чтобы понять, добавилось ли что-то в тело).</summary>
    public int Count => _lines.Count;

    public void Indent() => _level++;

    public void Dedent() => _level--;

    public void Line(string text) => _lines.Add(string.Concat(Enumerable.Repeat(_indentUnit, _level)) + text);
}
