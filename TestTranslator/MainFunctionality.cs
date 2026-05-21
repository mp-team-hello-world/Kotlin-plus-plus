
using NUnit.Framework;
public class MainFunctionalityTests
{
    [Test]
    public void Val()
    {
        string input = "val x = 10";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("auto x = 10;"));
    }

    [Test]
    public void Var()
    {
        string input = "var y = 10";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("auto y = 10;"));
    }
    [Test]
    public void If_1()
    {
        string input = "if (x > 5) x";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("if (x > 5)"));
        Assert.That(result, Does.Contain("x;"));
    }

    [Test]
    public void Else_1()
    {
        string input = "if (x > 5) x else 0";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("else"));
    }
    [Test]
    public void For_Loop_1()
    {
        string input = "for (i in 1..3) i";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("for (int i = 1; i <= 3; i++)"));
    }

    [Test]
    public void For_Loop_2()
    {
        string input = "for (i in 1..x) { i <= x }";
        string result = Translator.Translate(input);
        Assert.That(result, Does.Contain("for (int i = 1; i <= x; i++)"));
        Assert.That(result, Does.Contain("i <= x"));
    }

    [Test]
    public void Main_Functionality_1()
    {
        string input = "if (x > 5) {for (i in 1..x) {val y = i}} else {val z = 0}";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("if (x > 5)"));
        Assert.That(result, Does.Contain("for (int i = 1; i <= x; i++)"));
        Assert.That(result, Does.Contain("auto y = i;"));
        Assert.That(result, Does.Contain("auto z = 0;"));
    }
}
