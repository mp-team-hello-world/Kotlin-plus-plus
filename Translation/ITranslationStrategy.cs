
namespace Kotlin_plus_plus;

public interface ITranslationStrategy
{
    (string TargetCode, TreeNodeDto Tree) Translate(string sourceCode);
}