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
        string input = @"fun mix(a: Boolean, b: Int, c: Long, d: Double): Double {
    val x = a && (b > 0)
    val y = c + b
    val z = d + y
    if (x){
        return z;
    }
    else {
        return -z;
    }
}
";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("double mix(bool a, int b, long c, double d)"));
        Assert.That(result, Does.Contain("auto x = a && (b > 0);"));
        Assert.That(result, Does.Contain("auto y = c + b;"));
        Assert.That(result, Does.Contain("auto z = d + y;"));
        Assert.That(result, Does.Contain("if (x)"));
        Assert.That(result, Does.Contain("return z;"));
        Assert.That(result, Does.Contain("else"));
        Assert.That(result, Does.Contain("return -z;"));
    }

    [Test]
    public void FunctionExpression_3_and_double()
    {
        string input = @"fun compute(a: Long, b: Float, c: Double): Double {
    val x = a * b
    val y = x / c
    return y + 10.5
}
";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("double compute(long a, float b, double c)"));
        Assert.That(result, Does.Contain("auto x = a * b;"));
        Assert.That(result, Does.Contain("auto y = x / c;"));
        Assert.That(result, Does.Contain("return y + 10.5;"));
    }

    [Test]
    public void FunctionExpression_4()
    {
        string input = @"fun safeDivide(a: Int, b: Int): Double {
    try {
        return a.toDouble() / b
    } catch (e: Exception) {
        return -1.0
    }
}
";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("double safeDivide(int a, int b)"));
        Assert.That(result, Does.Contain("try"));
        Assert.That(result, Does.Contain("/* Unsupported statement construction: return a.toDouble() / b */"));
        Assert.That(result, Does.Contain("catch (const std::exception& e)"));
        Assert.That(result, Does.Contain("return -1.0;"));
    }

    [Test]
    public void FunctionExpression_5_plus_string()
    {
        string input = @"fun classify(x: Int, y: Double, flag: Boolean): String {
    if (flag && x > 0) return ""positive""
    if (!flag && y < 0.0) return ""negative""
    return ""unknown""
}
";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("std::string classify(int x, double y, bool flag)"));
        Assert.That(result, Does.Contain("if (flag && x > 0)"));
        Assert.That(result, Does.Contain("return \"positive\";"));
        Assert.That(result, Does.Contain("if (!flag && y < 0.0)"));
        Assert.That(result, Does.Contain("return \"negative\";"));
        Assert.That(result, Does.Contain("return \"unknown\";"));
    }
}
