using System;
using Antlr4.Runtime;
using Kotlin_plus_plus;

class Program
{
    static void Main(string[] args)
    {
        string kotlinCode = @"
        fun testAllFeatures(limit: Int): Int {
    var count = 0
    
    for (i in 1..limit) {
        if (i > 10) {
            break
        } else {
            count = count + i
        }
    }
    while (count < 100) {
        try {
            count = count + 10
        } catch (e: Exception) {
            return -1
        } finally {
            count = count + 1
        }
    }

    return count
}
        ";

        var inputStream = new AntlrInputStream(kotlinCode);

        var lexer = new KotlinLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);

        var parser = new KotlinParser(tokenStream);

        var tree = parser.root();

        Console.WriteLine(tree.ToStringTree(parser));
    }
}