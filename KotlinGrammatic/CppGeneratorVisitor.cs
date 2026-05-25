using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Kotlin_plus_plus;

// TODO: фабрика? ООП

public class CppGeneratorVisitor : KotlinParserBaseVisitor<string>
{
    private int _indentLevel = 0;
    private List<string> _output = new List<string>();
    
    private string Indent => new string(' ', _indentLevel * 4);
    
    private void AddLine(string line)
    {
        _output.Add(Indent + line);
    }
    
    public string GetResult()
    {
        // Формируем финальный вывод
        var finalOutput = new List<string>();
        
        // Заголовки
        finalOutput.Add("#include <iostream>");
        finalOutput.Add("#include <string>");
        finalOutput.Add("");
        finalOutput.Add("using namespace std;");
        finalOutput.Add("");
        
        // main функция
        finalOutput.Add("int main() {");
        
        // Тело с отступами
        foreach (var line in _output)
        {
            finalOutput.Add("    " + line);
        }
        
        finalOutput.Add("    return 0;");
        finalOutput.Add("}");
        
        return string.Join("\n", finalOutput);
    }
    
    // root: statements EOF;
    public override string VisitRoot(KotlinParser.RootContext context)
    {
        // Собираем все statements
        Visit(context.statements());
        return null;  // Не возвращаем ничего, всё уже в _output
    }
    
    // statements: (statement (SEMICOLON? NL?)*)*
    public override string VisitStatements(KotlinParser.StatementsContext context)
    {
        for (int i = 0; i < context.ChildCount; i++)
        {
            var child = context.GetChild(i);
            if (child is KotlinParser.StatementContext statement)
            {
                Visit(statement);
            }
        }
        return null;
    }
    
    // statement: variableDeclaration | ifExpression | forStatement | block | expression
    public override string VisitStatement(KotlinParser.StatementContext context)
    {
        if (context.variableDeclaration() != null)
        {
            Visit(context.variableDeclaration());
        }
        else if (context.ifExpression() != null)
        {
            Visit(context.ifExpression());
        }
        else if (context.forStatement() != null)
        {
            Visit(context.forStatement());
        }
        else if (context.block() != null)
        {
            Visit(context.block());
        }
        else if (context.expression() != null)
        {
            string expr = Visit(context.expression());
            if (!string.IsNullOrEmpty(expr))
            {
                AddLine($"{expr};");
            }
        }
        return null;
    }
    
    // variableDeclaration: (VAL | VAR) ID ASSIGNMENT expression;
    public override string VisitVariableDeclaration(KotlinParser.VariableDeclarationContext context)
    {
        string varName = context.ID().GetText();
        string value = Visit(context.expression());
        
        AddLine($"auto {varName} = {value};");
        return null;
    }
    
    // expression: ID | INT | expression (LANGLE|RANGLE|LE|GE|EQEQ|EXCL_EQ) expression | LPAREN expression RPAREN
    public override string VisitExpression(KotlinParser.ExpressionContext context)
    {
        // Случай 1: просто переменная
        if (context.ID() != null)
        {
            return context.ID().GetText();
        }
        // Случай 2: просто число
        else if (context.INT() != null)
        {
            return context.INT().GetText();
        }
        // Случай 3: выражение в скобках: ( expression )
        else if (context.LPAREN() != null)
        {
            var expressions = context.expression();
            if (expressions != null && expressions.Length > 0)
            {
                return Visit(expressions[0]);
            }
            
            for (int i = 0; i < context.ChildCount; i++)
            {
                if (context.GetChild(i) is KotlinParser.ExpressionContext innerExpr)
                {
                    return Visit(innerExpr);
                }
            }
            
            return "";
        }
        // Случай 4: бинарная операция
        else if (context.ChildCount == 3)
        {
            var leftChild = context.GetChild(0) as KotlinParser.ExpressionContext;
            var rightChild = context.GetChild(2) as KotlinParser.ExpressionContext;
            
            if (leftChild != null && rightChild != null)
            {
                string left = Visit(leftChild);
                string op = context.GetChild(1).GetText();
                string right = Visit(rightChild);
                return $"{left} {op} {right}";
            }
        }
        
        return "";
    }
    
    // block: LCURL statements RCURL;
    public override string VisitBlock(KotlinParser.BlockContext context)
    {
        AddLine("{");
        _indentLevel++;
        Visit(context.statements());
        _indentLevel--;
        AddLine("}");
        return null;
    }
    
    // ifExpression: IF LPAREN expression RPAREN controlStructureBody (ELSE controlStructureBody)?
    public override string VisitIfExpression(KotlinParser.IfExpressionContext context)
    {
        string condition = Visit(context.expression());
        AddLine($"if ({condition})");
        
        Visit(context.controlStructureBody(0));
        
        if (context.controlStructureBody().Length > 1)
        {
            AddLine("else");
            Visit(context.controlStructureBody(1));
        }
        
        return null;
    }
    
    // controlStructureBody: block | statement
    public override string VisitControlStructureBody(KotlinParser.ControlStructureBodyContext context)
    {
        if (context.block() != null)
        {
            Visit(context.block());
        }
        else if (context.statement() != null)
        {
            _indentLevel++;
            Visit(context.statement());
            _indentLevel--;
        }
        return null;
    }
    
    // forStatement: FOR LPAREN ID IN expression DOTDOT expression RPAREN controlStructureBody;
    public override string VisitForStatement(KotlinParser.ForStatementContext context)
    {
        string varName = context.ID().GetText();
        string start = Visit(context.expression(0));
        string end = Visit(context.expression(1));
        
        AddLine($"for (int {varName} = {start}; {varName} <= {end}; {varName}++)");
        Visit(context.controlStructureBody());
        
        return null;
    }
}