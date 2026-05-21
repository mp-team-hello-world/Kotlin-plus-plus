
using NUnit.Framework;
public class UnsupportedCodeTests
{
    [Test]
    public void Unsupported_Code_1()
    {
        string input = "print(123)";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("// unsupported: print(123)"));
    }
    [Test]
    public void Unsupported_Code_2()
    {
        string input = "while (i in 1..x) {val y = i}";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("// unsupported: while (i in 1..x) {val y = i}"));
    }
}
