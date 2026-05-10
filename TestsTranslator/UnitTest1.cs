using NUnit.Framework;

public class ParserTests
{
    [Test]
    public void AddFunction_ShouldGenerateCorrectCpp()
    {
        var input = "def add(a, b): return a + b";
        var result = TranslatorProgram.Translate(input);

        Assert.That(result, Does.Contain("auto add"));
        Assert.That(result, Does.Contain("return a + b;"));
    }
}
