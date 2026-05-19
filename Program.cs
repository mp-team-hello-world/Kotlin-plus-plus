using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Antlr4.Runtime;
using Kotlin_plus_plus;

var builder = WebApplication.CreateBuilder(args);

// 1. Настраиваем CORS (Политика безопасности)
// Браузер заблокирует запрос с GitHub Pages, если мы явно не разрешим этот домен.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://mp-team-hello-world.github.io")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Настраиваем порт, который будет слушать приложение локально
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000); // Теперь сервер всегда встает на http://localhost:5000
});

var app = builder.Build();

// Включаем CORS перед обработкой запросов
app.UseCors();

// 2. Создаем эндпоинт POST, на который сайт будет слать код
app.MapPost("/translate", (TranslateRequest request) =>
{
    // Проверяем, пришел ли вообще код
    if (string.IsNullOrWhiteSpace(request.Code))
    {
        return Results.BadRequest(new { error = "Входной код пуст" });
    }

    try
    {
        // Вставляем вашу логику ANTLR: берем код из запроса (request.Code)
        var inputStream = new AntlrInputStream(request.Code);
        var lexer = new KotlinLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new KotlinParser(tokenStream);

        var tree = parser.root();

        var visitor = new CppGeneratorVisitor();
        visitor.Visit(tree);
        
        // Возвращаем результат обратно на сайт в JSON-формате
        return Results.Ok(new { cppCode = visitor.GetResult() });
    }
    catch (Exception ex)
    {
        // Если парсер сломался — возвращаем статус 400 и текст ошибки
        return Results.BadRequest(new { error = ex.Message });
    }
});

// Запускаем веб-сервер
app.Run();

// Специальная DTO-модель: .NET сам превратит JSON с сайта { "code": "..." } в этот объект
public record TranslateRequest(string Code);