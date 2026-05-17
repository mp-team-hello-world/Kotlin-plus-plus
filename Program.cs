using System;
using Antlr4.Runtime;
using Kotlin_plus_plus;

class Program
{
    static void Main(string[] args)
    {
        string kotlinCode = @"val x = 10
        if (x > 5) {
            val y = x
        }
        ";

        var inputStream = new AntlrInputStream(kotlinCode);

        var lexer = new KotlinLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);

        var parser = new KotlinParser(tokenStream);

        var tree = parser.root();

        var visitor = new CppGeneratorVisitor();

        visitor.Visit(tree);
        
        Console.WriteLine("=== Сгенерированный C++ код ===");
        Console.WriteLine(visitor.GetResult());
    }
}