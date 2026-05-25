using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Kotlin_plus_plus;

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

        finalOutput.Add("using namespace std;");
        finalOutput.Add("");

        finalOutput.AddRange(_output);

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
        foreach (var statement in context.statement())
        {
            Visit(statement);
        }
        return null;
    }

    // statement: variableDeclaration | ifExpression | forStatement | block | expression
    public override string VisitStatement(KotlinParser.StatementContext context)
    {
        if (context.expression() != null)
        {
            string exprResult = Visit(context.expression());
            if (!string.IsNullOrEmpty(exprResult))
            {
                if (exprResult.StartsWith("/*") && exprResult.EndsWith("*/"))
                {
                    AddLine(exprResult);
                }
                else
                {
                    AddLine($"{exprResult};");
                }
            }
            return null;
        }

        // Для всех остальных (if, for, while, объявлений переменных) вызываем стандартный обход
        return base.VisitStatement(context);
    }

    public override string VisitIdentifier(KotlinParser.IdentifierContext context)
    {
        return context.ID().GetText();
    }

    public override string VisitIntLiteral(KotlinParser.IntLiteralContext context)
    {
        return context.INT().GetText();
    }

    public override string VisitParens(KotlinParser.ParensContext context)
    {
        return $"({Visit(context.expression())})";
    }

    public override string VisitUnaryMinus(KotlinParser.UnaryMinusContext context)
    {
        return $"-{Visit(context.expression())}";
    }

    public override string VisitMulDiv(KotlinParser.MulDivContext context)
    {
        return $"{Visit(context.expression(0))} {context.GetChild(1).GetText()} {Visit(context.expression(1))}";
    }

    public override string VisitAddSub(KotlinParser.AddSubContext context)
    {
        return $"{Visit(context.expression(0))} {context.GetChild(1).GetText()} {Visit(context.expression(1))}";
    }

    public override string VisitComparison(KotlinParser.ComparisonContext context)
    {
        return $"{Visit(context.expression(0))} {context.GetChild(1).GetText()} {Visit(context.expression(1))}";
    }

    // variableDeclaration: (VAL | VAR) ID ASSIGNMENT expression;
    public override string VisitVariableDeclaration(KotlinParser.VariableDeclarationContext context)
    {
        if (context.expression() == null || context.ASSIGNMENT() == null)
        {
            // Берем оригинальный текст с пробелами
            int a = context.Start.TokenIndex;
            int b = context.Stop.TokenIndex;
            var interval = Antlr4.Runtime.Misc.Interval.Of(context.Start.StartIndex, context.Stop.StopIndex);
            string rawStatement = context.Start.InputStream.GetText(interval).Trim().Replace("\r", "").Replace("\n", " ");

            AddLine($"/* Unsupported variable declaration syntax: {rawStatement} */");
            return null;
        }

        string exprText = context.expression().GetText();

        if (exprText.StartsWith("listOf") ||
            exprText.StartsWith("mapOf"))
        {
            // Исправлено: забираем сырой текст с сохранением всех пробелов
            var interval = Antlr4.Runtime.Misc.Interval.Of(context.Start.StartIndex, context.Stop.StopIndex);
            string rawStatement = context.Start.InputStream.GetText(interval).Trim().Replace("\r", "").Replace("\n", " ");

            AddLine($"/* Unsupported variable declaration (contains unimplemented features): {rawStatement} */");
            return null;
        }

        string varName = context.ID()?.GetText() ?? "unknownVar";
        string modifier = context.VAL() != null ? "const auto" : "auto";

        string value = Visit(context.expression());
        AddLine($"{modifier} {varName} = {value};");

        return null;
    }

    public override string VisitNormalAssignment(KotlinParser.NormalAssignmentContext context)
    {
        var id = context.ID().GetText();
        var expr = Visit(context.expression());
        AddLine($"{id} = {expr};");
        return null;
    }

    public override string VisitAddAssignment(KotlinParser.AddAssignmentContext context)
    {
        var id = context.ID().GetText();
        var expr = Visit(context.expression());
        AddLine($"{id} += {expr};");
        return null;
    }

    public override string VisitSubAssignment(KotlinParser.SubAssignmentContext context)
    {
        var id = context.ID().GetText();
        var expr = Visit(context.expression());
        AddLine($"{id} -= {expr};");
        return null;
    }

    public override string VisitMultAssignment(KotlinParser.MultAssignmentContext context)
    {
        var id = context.ID().GetText();
        var expr = Visit(context.expression());
        AddLine($"{id} *= {expr};");
        return null;
    }

    public override string VisitDivAssignment(KotlinParser.DivAssignmentContext context)
    {
        var id = context.ID().GetText();
        var expr = Visit(context.expression());
        AddLine($"{id} /= {expr};");
        return null;
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

    public override string VisitControlStructureBody(KotlinParser.ControlStructureBodyContext context)
    {
        if (context.block() != null)
            Visit(context.block());
        else if (context.statement() != null)
        {
            _indentLevel++;
            Visit(context.statement());
            _indentLevel--;
        }
        return null;
    }

    // ifExpression: IF LPAREN expression RPAREN controlStructureBody (ELSE controlStructureBody)?
    public override string VisitIfExpression(KotlinParser.IfExpressionContext context)
    {
        AddLine($"if ({Visit(context.expression())})");
        Visit(context.controlStructureBody(0));

        if (context.controlStructureBody().Length > 1)
        {
            AddLine("else");
            Visit(context.controlStructureBody(1));
        }
        return null;
    }


    public override string VisitForStatement(KotlinParser.ForStatementContext context)
    {
        AddLine($"for (int {context.ID().GetText()} = {Visit(context.expression(0))}; {context.ID().GetText()} <= {Visit(context.expression(1))}; {context.ID().GetText()}++)");
        Visit(context.controlStructureBody());
        return null;
    }

    public override string VisitWhileStatement(KotlinParser.WhileStatementContext context)
    {
        AddLine($"while ({Visit(context.expression())})");
        if (context.controlStructureBody() != null)
            Visit(context.controlStructureBody());
        else
            AddLine(";");
        return null;
    }

    public override string VisitDoWhileStatement(KotlinParser.DoWhileStatementContext context)
    {
        AddLine("do");
        if (context.controlStructureBody() != null)
            Visit(context.controlStructureBody());
        else
            AddLine(";");
        AddLine($"while ({Visit(context.expression())});");
        return null;
    }

    public override string VisitFunctionDeclaration(KotlinParser.FunctionDeclarationContext context)
    {
        string funcName = context.ID()[0].GetText();

        // Параметры
        var parameters = new List<string>();
        if (context.parameter() != null)
        {
            foreach (var param in context.parameter())
            {
                string name = param.ID(0)?.GetText() ?? "p";
                string type = ConvertType(param.ID(1)?.GetText() ?? "Unit");
                parameters.Add($"{type} {name}");
            }
        }

        // Возвращаемый тип
        string returnType;
        if (funcName == "main")
        {
            // main всегда должен быть int в C++
            returnType = "int";
        }
        else if (context.ID().Length > 1)
        {
            string kotlinReturnType = context.ID()[context.ID().Length - 1].GetText();
            returnType = ConvertType(kotlinReturnType);
        }
        else
        {
            returnType = "void";
        }

        AddLine($"{returnType} {funcName}({string.Join(", ", parameters)})");

        if (funcName == "main")
        {
            // Если это main, мы не просто визитим тело, а гарантируем return 0 в конце блока
            // Для этого заглянем внутрь блока функции
            var body = context.functionBody();
            if (body.block() != null)
            {
                AddLine("{");
                _indentLevel++;
                Visit(body.block().statements());
                AddLine("return 0;");
                _indentLevel--;
                AddLine("}");
            }
            else
            {
                Visit(body);
            }
        }
        else
        {
            Visit(context.functionBody());
        }
        AddLine("");
        return null;
    }

    public override string VisitFunctionBody(KotlinParser.FunctionBodyContext context)
    {
        if (context.block() != null)
        {
            Visit(context.block());
        }
        else if (context.ASSIGNMENT() != null)
        {
            AddLine("{");
            _indentLevel++;
            AddLine($"return {Visit(context.expression())};");
            _indentLevel--;
            AddLine("}");
        }
        return null;
    }

    private string ConvertType(string kotlinType) => kotlinType switch
    {
        "Int" => "int",
        "Boolean" => "bool",
        "Unit" => "void",
        _ => "auto"
    };

    // ==================== ПРЫЖКИ ====================

    public override string VisitJumpExpression(KotlinParser.JumpExpressionContext context)
    {
        if (context.RETURN() != null)
            AddLine(context.expression() != null ? $"return {Visit(context.expression())};" : "return;");
        else if (context.CONTINUE() != null)
            AddLine("continue;");
        else if (context.BREAK() != null)
            AddLine("break;");
        else if (context.THROW() != null)
            AddLine($"// throw {Visit(context.expression())}");
        return null;
    }

    // ==================== ОБРАБОТКА ИСКЛЮЧЕНИЙ ====================

    public override string VisitTryExpression(KotlinParser.TryExpressionContext context)
    {
        AddLine("try");
        Visit(context.block());

        foreach (var catchBlock in context.catchBlock())
        {
            string paramName = "e";
            string kotlinType = null;

            if (catchBlock.ID().Length >= 1)
            {
                // Если грамматика даёт ID(0) как имя и ID(1) как тип
                if (catchBlock.ID().Length == 1)
                {
                    // Возможно только имя без типа: catch (e)
                    paramName = catchBlock.ID(0).GetText();
                }
                else if (catchBlock.ID().Length >= 2)
                {
                    paramName = catchBlock.ID(0).GetText();
                    kotlinType = catchBlock.ID(1).GetText();
                }
            }

            // Маппинг Kotlin типа на C++ тип
            string cppType;
            if (string.IsNullOrEmpty(kotlinType))
            {
                // Если тип не указан — используем универсальный catch
                AddLine("catch (...)");
                Visit(catchBlock.block());
                continue;
            }
            else
            {
                // Exception -> std::exception
                cppType = kotlinType switch
                {
                    "Exception" => "const std::exception&",
                    "IOException" => "const std::ios_base::failure&",
                    // добавим другие при необхоимости
                    _ => "const std::exception&"
                };
            }

            AddLine($"catch ({cppType} {paramName})");
            Visit(catchBlock.block());
        }

        if (context.finallyBlock() != null)
        {
            AddLine("// finally");
            Visit(context.finallyBlock().block());
        }
        return null;
    }

    public override string VisitFunctionCall(KotlinParser.FunctionCallContext context)
    {
        // Исправлено: забираем сырой текст с сохранением всех пробелов
        var interval = Antlr4.Runtime.Misc.Interval.Of(context.Start.StartIndex, context.Stop.StopIndex);
        string rawCode = context.Start.InputStream.GetText(interval).Trim().Replace("\r", "").Replace("\n", " ");
        return $"/* Unimplemented function call: {rawCode} */";
    }

    public override string VisitUnparsedStatement(KotlinParser.UnparsedStatementContext context)
    {
        // Исправлено: сохраняем пробелы между val и переменными
        var interval = Antlr4.Runtime.Misc.Interval.Of(context.Start.StartIndex, context.Stop.StopIndex);
        string rawCode = context.Start.InputStream.GetText(interval).Trim().Replace("\r", "").Replace("\n", " ");

        if (!string.IsNullOrWhiteSpace(rawCode))
        {
            AddLine($"/* Unsupported statement construction: {rawCode} */");
        }

        return null;
    }
}
