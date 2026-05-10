# Про NUnit

Предлагаю каждую группу тестов (например на циклы, условия и тд ) размещать в отдельном файле. Каждый метод с `[Test]` — один тест.

## 1.   `[Test]` — обычный тест
```csharp
[Test]
public void Should_Parse_Function() {}
Assert.That — универсальная проверка
Assert.That(result, Is.EqualTo("expected"));
Assert.That(result, Does.Contain("auto"));
Assert.That(result, Is.Not.Null);
```

## 2.	Assert.Throws — проверка ошибок
Если программа должна ругаться:
```csharp
Assert.Throws<ParseException>(() => TranslatorProgram.Translate("def :"));
```

## 3.	`[SetUp]` — выполняется перед каждым тестом
Если нужно подготовить состояние:
```csharp
[SetUp]
public void Init()
{
    translator = new TranslatorProgram();
}
```

## 4.	`[TestCase]` — параметризованные тесты
Для циклов, условий, арифметики:
```csharp
[TestCase("a + b", "a + b;")]
[TestCase("a - b", "a - b;")]
public void Arithmetic_Should_Translate(string input, string expected)
{
    var result = TranslatorProgram.Translate(input);
    Assert.That(result, Does.Contain(expected));
}
```
