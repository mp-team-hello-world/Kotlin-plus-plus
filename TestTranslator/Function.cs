using NUnit.Framework;
public class FunctionTests
{
    [Test]
    public void FunctionExpression_1()
    {
        string input = "fun sum(a: Int, b: Int): Int = a + b";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("int sum(int a, int b)"));
        Assert.That(result, Does.Contain("return a + b;"));
    }

    [Test]
    public void FunctionExpression_2()
    {
        string input = "fun sum(a: Int, b: Int): Int = a + b";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("int sum(int a, int b)"));
        Assert.That(result, Does.Contain("return a + b;").Or.Contain("/* Unsupported"));
    }
}
