using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Kotlin_plus_plus;

/// <summary>Общие помощники для любых ANTLR-frontend'ов.</summary>
public static class AntlrText
{
    /// <summary>
    /// Исходный текст узла с сохранением пробелов; переводы строк заменены на пробел,
    /// чтобы текст влезал в однострочный комментарий.
    /// </summary>
    public static string Raw(ParserRuleContext context)
    {
        var interval = Interval.Of(context.Start.StartIndex, context.Stop.StopIndex);
        return context.Start.InputStream.GetText(interval).Trim().Replace("\r", "").Replace("\n", " ");
    }
}
