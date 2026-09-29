using Antlr4.Runtime;
using Kotlin_plus_plus;
using System.Text.Json;

public static class Translator
{
    // Старое поведение — используется тестами (TestTranslator/*.cs) и везде,
    // где нужен только C++ код. НЕ ТРОГАЕМ сигнатуру, чтобы ничего не сломать.
    public static string Translate(string code)
    {
        var (cppCode, _) = TranslateInternal(code);
        return cppCode;
    }

    // Новый метод — используется эндпоинтом /translate, отдаёт код + дерево разбора.
    public static (string CppCode, TreeNodeDto Tree) TranslateWithTree(string code)
    {
        return TranslateInternal(code);
    }

    private static (string CppCode, TreeNodeDto Tree) TranslateInternal(string code)
    {
        var inputStream = new AntlrInputStream(code);
        var lexer = new KotlinLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new KotlinParser(tokenStream);

        var tree = parser.root();

        var treeDto = ParseTreeConverter.Convert(tree);

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(treeDto, jsonOptions);


        var visitor = new CppGeneratorVisitor();
        visitor.Visit(tree);

        return (visitor.GetResult(), treeDto);
    }
}