using Antlr4.Runtime;
using NUnit.Framework;
using Kotlin_plus_plus;

public class NewLanguagesGrammarTests
{
    [Test]
    public void CSharpGrammar_ParsesBasicCode()
    {
        string code = @"
int sum(int a, int b)
{
    return a + b;
}

var x = 10;
if (x > 5)
{
    x += 1;
}
";
        var inputStream = new AntlrInputStream(code);
        var lexer = new CSharpLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new CSharpParser(tokenStream);

        var tree = parser.root();

        Assert.That(tree, Is.Not.Null);
        Assert.That(parser.NumberOfSyntaxErrors, Is.EqualTo(0));
    }

    [Test]
    public void PythonGrammar_ParsesBasicCode()
    {
        string code = @"x = 10
if x > 5:
    x += 1
def sum(a: int, b: int) -> int:
    return a + b
";
        var inputStream = new AntlrInputStream(code);
        var lexer = new PythonLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new PythonParser(tokenStream);

        var tree = parser.root();

        Assert.That(tree, Is.Not.Null);
        Assert.That(parser.NumberOfSyntaxErrors, Is.EqualTo(0));
    }
}

