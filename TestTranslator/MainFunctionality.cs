
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
        string input = "if (x > 5 and y < 10) x else 0";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("if (x > 5 && y < 10)"));
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
        string input = "var x = 10\nfor (i in 1..x) { i = 5 }"; // не работает без \n или ;
        string result = Translator.Translate(input);
        Assert.That(result, Does.Contain("auto x = 10;"));
        Assert.That(result, Does.Contain("for (int i = 1; i <= x; i++)"));
        Assert.That(result, Does.Contain("i = 5"));
    }

    [Test]
    public void While_Loop_1()
    {
        string input = "var count = 0; while (count < 100 ) { count = count + 1 }"; // не работает без \n или ;
        string result = Translator.Translate(input);
        Assert.That(result, Does.Contain("while (count < 100)"));
        Assert.That(result, Does.Contain("count < 100"));
        Assert.That(result, Does.Contain("{\n    count = count + 1;\n}"));
    }

    [Test]
    public void Main_Functionality_1_if_for_else()
    {
        string input = "if (x > 5) {for (i in 1..x) {val y = i}} else {val z = 0}";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("if (x > 5)"));
        Assert.That(result, Does.Contain("for (int i = 1; i <= x; i++)"));
        Assert.That(result, Does.Contain("auto y = i;"));
        Assert.That(result, Does.Contain("auto z = 0;"));
    }

    [Test]
    public void Main_Functionality_2_try_catch()
    {
        string input = @"fun testAllFeatures(limit: Int): Int {
    var count = 0
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
}";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("int testAllFeatures(int limit)"));
        Assert.That(result, Does.Contain("auto count = 0;"));
        Assert.That(result, Does.Contain("while (count < 100)"));
        Assert.That(result, Does.Contain("try"));
        Assert.That(result, Does.Contain("catch (const std::exception& e)"));
        Assert.That(result, Does.Contain("return -1;"));
        Assert.That(result, Does.Contain("// finally"));
        Assert.That(result, Does.Contain("count = count + 1;"));
        Assert.That(result, Does.Contain("return count;"));
    }

    [Test]
    public void Function_with_Try_Catch_Exception_1()
    {
        string input = @"fun complex(n: Int): Int {
    var s = 0
    for (i in 1..n) {
        try {
            s += i
        } catch (e: Exception) {
            return -1
        }
    }
    return s
}";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("int complex(int n)"));
        Assert.That(result, Does.Contain("for (int i = 1; i <= n; i++)"));
        Assert.That(result, Does.Contain("try"));
        Assert.That(result, Does.Contain("catch (const std::exception& e)"));
        Assert.That(result, Does.Contain("return -1;"));
        Assert.That(result, Does.Contain("return s;"));
    }

    [Test]
    public void Function_with_Try_Catch_Exception_2()
    {
        string input = @"try {
    readFile()
} catch (io: IOException) {
    handleIo(io)
}";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("catch (const std::ios_base::failure& io)"));
    }

    [Test]
    public void Main_Functionality_3_final()
    {
        string input = @"fun main() {
    val score = processData(20)
}

fun processData(limit: Int): Int {
    var result = 0

    for (i in 1..limit) {
        if (i > 10 && limit > 1000) {
            break
        } else {
            result = result + i * i
        }
    }

    while (result < 5000) {
        try {
            result *= 2
        } catch (e: Exception) {
            return -1
        } finally {
            result += 1
        }
    }

    return result
}";
        string result = Translator.Translate(input);

        Assert.That(result, Does.Contain("int main()"));
        Assert.That(result, Does.Contain("auto score = processData(20);"));
        Assert.That(result, Does.Contain("int processData(int limit)"));
        Assert.That(result, Does.Contain("auto result = 0;"));
        Assert.That(result, Does.Contain("for (int i = 1; i <= limit; i++)"));
        Assert.That(result, Does.Contain("if (i > 10 && limit > 1000)"));
        Assert.That(result, Does.Contain("break;"));
        Assert.That(result, Does.Contain("result = result + i * i;"));
        Assert.That(result, Does.Contain("while (result < 5000)"));
        Assert.That(result, Does.Contain("try"));
        Assert.That(result, Does.Contain("catch (const std::exception& e)"));
        Assert.That(result, Does.Contain("return -1;"));
        Assert.That(result, Does.Contain("// finally"));
        Assert.That(result, Does.Contain("result += 1;"));
        Assert.That(result, Does.Contain("return result;"));
    }
}
