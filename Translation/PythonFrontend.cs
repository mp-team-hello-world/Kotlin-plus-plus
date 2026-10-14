using Antlr4.Runtime; // Или Antlr4.Runtime в зависимости от вашей версии ANTLR

namespace Kotlin_plus_plus;

public sealed class PythonFrontend : ISourceFrontend
{
    public ParseResult Parse(string sourceCode)
    {
        // 1. Лексер и парсер Python (предполагаем, что у вас сгенерированы PythonLexer и PythonParser)
        var inputStream = new AntlrInputStream(sourceCode);
        var lexer = new PythonLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new PythonParser(tokenStream);

        // Предположим, корневое правило вашей питоновской грамматики называется file_input или root
        var tree = parser.root(); 

        // 2. Превращаем дерево ANTLR Python в общее IR-дерево
        var treeDto = ParseTreeConverter.Convert(tree, sourceCode);

        // Язык-специфичное дерево ANTLR -> язык-независимое IR
        var program = (ProgramNode)new PythonAstBuilder().Visit(tree);

        return new ParseResult(program, treeDto);
    }
}