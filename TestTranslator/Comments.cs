using NUnit.Framework;
public class CommentsTests
{
    [Test]
    public void Comment_1()
    {
        string input = "val x = 10 // hello world";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("auto x = 10;"));
    }

    [Test]
    public void Comment_2()
    {
        string input = "val x = 10 * 100 / 30 % 7 /* hello world \n 3883883 \t */";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("auto x = 10 * 100 / 30 % 7;"));
    }

}
