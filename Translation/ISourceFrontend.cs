namespace Kotlin_plus_plus;

/// <summary>Результат разбора: IR для генерации кода + дерево разбора для визуализации на сайте.</summary>
public sealed record ParseResult(ProgramNode Program, TreeNodeDto ParseTree);

/// <summary>
/// Исходный язык. Реализация = ANTLR-грамматика + построитель IR.
/// Знает ТОЛЬКО свой язык; про целевые языки не знает ничего.
/// </summary>
public interface ISourceFrontend
{
    ParseResult Parse(string sourceCode);
}
