using System;
using Antlr4.Runtime.Tree;

namespace Kotlin_plus_plus;

public static class ParseTreePrinter
{
    public static void Print(IParseTree tree, string indent = "", bool isLast = true)
    {
        if (tree == null) return;

        string nodeName = tree.GetType().Name.Replace("Context", "");

        if (tree.ChildCount == 0)
        {
            string tokenText = tree.GetText();
            if (tokenText == "\r" || tokenText == "\n" || tokenText == "\r\n") return;
            nodeName = $"'{tokenText}'";
        }

        string marker = isLast ? "└── " : "├── ";
        Console.WriteLine($"{indent}{marker}{nodeName}");

        string childIndent = indent + (isLast ? "    " : "│   ");

        for (int i = 0; i < tree.ChildCount; i++)
        {
            var child = tree.GetChild(i);
            bool isLastChild = (i == tree.ChildCount - 1);
            Print(child, childIndent, isLastChild);
        }
    }
}