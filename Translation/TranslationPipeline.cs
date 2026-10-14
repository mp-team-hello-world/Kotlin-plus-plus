namespace Kotlin_plus_plus;

/// <summary>
/// Универсальная стратегия перевода: любой frontend + любой backend.
/// Заменяет класс-на-каждую-пару (KotlinToCppStrategy, KotlinToPythonStrategy, ...).
/// </summary>
public sealed class TranslationPipeline : ITranslationStrategy
{
    private readonly ISourceFrontend _frontend;
    private readonly ITargetBackend _backend;

    public TranslationPipeline(ISourceFrontend frontend, ITargetBackend backend)
    {
        _frontend = frontend;
        _backend = backend;
    }

    public (string TargetCode, TreeNodeDto Tree) Translate(string sourceCode)
    {
        var parsed = _frontend.Parse(sourceCode);
        string targetCode = _backend.Generate(parsed.Program);
        return (targetCode, parsed.ParseTree);
    }
}
