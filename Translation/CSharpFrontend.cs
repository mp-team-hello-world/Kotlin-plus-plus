using Antlr4.Runtime;

namespace Kotlin_plus_plus;

public sealed class CSharpFrontend : ISourceFrontend
{
    public ParseResult Parse(string sourceCode)
    {
        var inputStream = new AntlrInputStream(sourceCode);
        var lexer = new CSharpLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new CSharpParser(tokenStream);

        var tree = parser.root();

        // Дерево для визуализации (ParseTreeConverter работает с любым ANTLR-деревом)
        var treeDto = ParseTreeConverter.Convert(tree, sourceCode);

        // Язык-специфичное дерево ANTLR -> язык-независимое IR
        var program = (ProgramNode)new CSharpAstBuilder().Visit(tree);

        return new ParseResult(program, treeDto);
    }
}