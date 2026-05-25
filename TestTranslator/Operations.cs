
using NUnit.Framework;
public class OperationsTests
{
    [Test]
    public void Assignment_operations_1()
    {
        string input = "var x = 5\nvar y = 0\ny += x\ny *= x\ny -= x\ny /= x";
        string result = Translator.Translate(input);
        Assert.That(result, Does.Contain("auto x = 5;"));
        Assert.That(result, Does.Contain("y += x;"));
        Assert.That(result, Does.Contain("y *= x;"));
        Assert.That(result, Does.Contain("y -= x;"));
        Assert.That(result, Does.Contain("y /= x;"));
    }

    [Test]
    public void Logical_operations_1()
    {
        string input = "var x = 5; var y = 11; if (x > 3 && y < 10 || x == 5) { y = 1; }";
        string result = Translator.Translate(input);
        Assert.That(result, Does.Contain("auto x = 5;"));
        Assert.That(result, Does.Contain("auto y = 11;"));
        Assert.That(result, Does.Contain("if (x > 3 && y < 10 || x == 5)"));
        Assert.That(result, Does.Contain("y = 1;"));
    }
}
