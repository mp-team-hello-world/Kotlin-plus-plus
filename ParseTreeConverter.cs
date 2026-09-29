using Antlr4.Runtime.Tree;
using System.Collections.Generic;

namespace Kotlin_plus_plus;

public static class ParseTreeConverter
{
    public static TreeNodeDto Convert(IParseTree tree)
    {
        if (tree == null) return null;

        // Получаем имя узла (убираем суффикс "Context" для красоты)
        string nodeName = tree.GetType().Name.Replace("Context", "");

        // Если это лист (токен, например '+' или 'val')
        if (tree.ChildCount == 0)
        {
            string tokenText = tree.GetText();
            if (tokenText == "\r" || tokenText == "\n" || tokenText == "\r\n") return null;
            nodeName = $"'{tokenText}'";
        }

        var dto = new TreeNodeDto { Name = nodeName };

        // Рекурсивно добавляем всех детей
        for (int i = 0; i < tree.ChildCount; i++)
        {
            var childDto = Convert(tree.GetChild(i));
            if (childDto != null)
            {
                dto.Children.Add(childDto);
            }
        }

        return dto;
    }
}