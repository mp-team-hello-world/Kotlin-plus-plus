using System;
using System.Collections.Generic;

namespace Kotlin_plus_plus;

/// <summary>
/// Реестр языков. Добавить язык = написать класс и добавить ОДНУ строку ниже.
/// Любая пара "from -> to" из зарегистрированных языков работает автоматически.
/// </summary>
public static class TranslationFactory
{
    // Исходные языки (ANTLR-грамматика + построитель IR)
    private static readonly Dictionary<string, Func<ISourceFrontend>> Frontends =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["kotlin"] = () => new KotlinFrontend(),
            ["python"] = () => new PythonFrontend(),
            ["kt"] = () => new KotlinFrontend(),
            ["py"] = () => new PythonFrontend(),
            ["csharp"] = () => new CSharpFrontend(),
            ["sharp"] = () => new CSharpFrontend()
        };

    // Целевые языки (генератор кода из IR)
    private static readonly Dictionary<string, Func<ITargetBackend>> Backends =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["cpp"] = () => new CppBackend()
            // ["python"] = () => new PythonBackend(),
        };

    public static IReadOnlyCollection<string> SourceLanguages => Frontends.Keys;

    public static IReadOnlyCollection<string> TargetLanguages => Backends.Keys;

    public static ITranslationStrategy GetStrategy(string fromLanguage, string toLanguage)
    {
        if (fromLanguage == null
            || toLanguage == null
            || !Frontends.TryGetValue(fromLanguage.Trim(), out var createFrontend)
            || !Backends.TryGetValue(toLanguage.Trim(), out var createBackend))
        {
            throw new NotSupportedException($"Перевод из {fromLanguage} в {toLanguage} пока не поддерживается.");
        }

        return new TranslationPipeline(createFrontend(), createBackend());
    }
}
