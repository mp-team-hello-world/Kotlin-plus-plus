namespace Kotlin_plus_plus;

/// <summary>
/// Целевой язык. Берёт готовое IR и выдаёт текст программы.
/// Знает ТОЛЬКО свой язык; про исходные языки не знает ничего.
/// Реализация должна быть потокобезопасной: состояние (отступы, накопленные строки)
/// создаётся внутри Generate, а не хранится в полях backend'а.
/// </summary>
public interface ITargetBackend
{
    string Generate(ProgramNode program);
}
