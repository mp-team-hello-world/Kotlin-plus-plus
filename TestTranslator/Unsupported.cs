
using NUnit.Framework;
public class UnsupportedCodeTests
{

    [Test]
    public void Unsupported_Function_Call_1()
    {
        string input = "val x = 5\nval y = 6\nval z = x & y\nprintln(123)";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("auto x = 5;"));
        Assert.That(result, Does.Contain("auto y = 6;"));
        Assert.That(result, Does.Contain("/* Unsupported statement construction: val z = x & y */"));
    }

    [Test]
    public void Unsupported_Code_1()
    {
        string input = "while (i in 1..x) {val y = i}";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("/* Unsupported statement construction: while (i in 1..x) */"));
    }

    [Test]
    public void Unsupported_Code_2()
    {
        string input = "for (i in 10 downTo 1 step 2) { i = i - 1 }";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("/* Unsupported statement construction: for (i in 10 downTo 1 step 2) */"));
    }
}
