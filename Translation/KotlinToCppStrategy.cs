using Antlr4.Runtime;

namespace Kotlin_plus_plus;

public class KotlinToCppStrategy : ITranslationStrategy
{
    public (string TargetCode, TreeNodeDto Tree) Translate(string sourceCode)
    {
        var inputStream = new AntlrInputStream(sourceCode);
        var lexer = new KotlinLexer(inputStream);
        var tokenStream = new CommonTokenStream(lexer);
        var parser = new KotlinParser(tokenStream);

        var tree = parser.root();
        
        // Используем существующий конвертер[cite: 4]
        var treeDto = ParseTreeConverter.Convert(tree, sourceCode); 

        var visitor = new CppGeneratorVisitor();
        visitor.Visit(tree);

        return (visitor.GetResult(), treeDto);
    }
}