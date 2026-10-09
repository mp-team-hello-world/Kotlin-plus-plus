using Kotlin_plus_plus;

public static class Translator
{
    // 1. Старое поведение — используется тестами. НЕ ТРОГАЕМ сигнатуру.
    public static string Translate(string code)
    {
        var (targetCode, _) = TranslateWithTree(code, "kotlin", "cpp");
        return targetCode;
    }

    // 2. Старый метод для обратной совместимости.
    public static (string CppCode, TreeNodeDto Tree) TranslateWithTree(string code)
    {
        return TranslateWithTree(code, "kotlin", "cpp");
    }

    // 3. НОВЫЙ метод, который принимает языки и обращается к фабрике.
    public static (string TargetCode, TreeNodeDto Tree) TranslateWithTree(string code, string from, string to)
    {
        var strategy = TranslationFactory.GetStrategy(from, to);
        return strategy.Translate(code);
    }
}