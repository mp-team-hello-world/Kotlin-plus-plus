using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System.Collections.Generic;

namespace Kotlin_plus_plus;

public static class ParseTreeConverter
{
    // Старая сигнатура остаётся рабочей (вдруг где-то ещё используется) —
    // просто не заполняет Source.
    public static TreeNodeDto Convert(IParseTree tree)
    {
        return Convert(tree, null);
    }

    // sourceCode — исходный Kotlin-код целиком. По нему для каждого узла
    // вырезается точный кусок текста, который этот узел покрывает
    // (по индексам начального/конечного токена, а не склейкой GetText(),
    // чтобы сохранить оригинальные пробелы и переносы строк).
    public static TreeNodeDto Convert(IParseTree tree, string sourceCode)
    {
        if (tree == null) return null;

        string nodeName = tree.GetType().Name.Replace("Context", "");
        string sourceText;

        if (tree.ChildCount == 0)
        {
            string tokenText = tree.GetText();
            if (tokenText == "\r" || tokenText == "\n" || tokenText == "\r\n") return null;
            nodeName = $"'{tokenText}'";
            sourceText = tokenText;
        }
        else
        {
            sourceText = ExtractSource(tree, sourceCode);
        }

        var dto = new TreeNodeDto { Name = nodeName, Source = sourceText ?? string.Empty };

        for (int i = 0; i < tree.ChildCount; i++)
        {
            var childDto = Convert(tree.GetChild(i), sourceCode);
            if (childDto != null)
            {
                dto.Children.Add(childDto);
            }
        }

        return dto;
    }

    private static string ExtractSource(IParseTree tree, string sourceCode)
    {
        if (string.IsNullOrEmpty(sourceCode) || tree is not ParserRuleContext ctx) return null;
        if (ctx.Start == null || ctx.Stop == null) return null;

        int start = ctx.Start.StartIndex;
        int stop = ctx.Stop.StopIndex;
        if (start < 0 || stop < start || stop >= sourceCode.Length) return null;

        return sourceCode.Substring(start, stop - start + 1);
    }
}