using Antlr4.Runtime;
using Kotlin_plus_plus;
public static class Translator
{
    public static string Translate(string code)
    {
        var inputStream = new AntlrInputStream(code);
        var lexer = new KotlinLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new KotlinParser(tokenStream);

        var tree = parser.root();

        var visitor = new CppGeneratorVisitor();
        visitor.Visit(tree);

        return visitor.GetResult();
    }
}
