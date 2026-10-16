using System.Collections.Generic;
using Antlr4.Runtime.Tree;

namespace Kotlin_plus_plus;

/// <summary>
/// Построитель IR-дерева для Python на основе сгенерированных классов PythonParser.
/// </summary>
public class PythonAstBuilder : PythonParserBaseVisitor<AstNode>
{
    private Expr ToExpr(IParseTree tree) => tree == null ? null : Visit(tree) as Expr;

    private Stmt ToStmt(IParseTree tree) => tree == null ? null : Visit(tree) as Stmt;

    private List<Stmt> BuildStatements(PythonParser.StatementsContext context)
    {
        var result = new List<Stmt>();
        if (context == null) return result;

        foreach (var stmtCtx in context.statement())
        {
            if (Visit(stmtCtx) is Stmt stmt)
            {
                result.Add(stmt);
            }
        }
        return result;
    }

    private static TypeRef ParseType(string typeName) => typeName switch
    {
        "int" => new TypeRef(DataType.Int, typeName),
        "bool" => new TypeRef(DataType.Bool, typeName),
        "float" => new TypeRef(DataType.Float, typeName),
        "str" => new TypeRef(DataType.String, typeName),
        _ => new TypeRef(DataType.Unknown, typeName)
    };

    // --- Корень программы ---
    public override AstNode VisitRoot(PythonParser.RootContext context)
    {
        return new ProgramNode(BuildStatements(context.statements()));
    }

    // --- Инструкции ---
    public override AstNode VisitStatement(PythonParser.StatementContext context)
    {
        if (context.compoundStatement() != null)
            return Visit(context.compoundStatement());
        if (context.simpleStatement() != null)
            return Visit(context.simpleStatement());
        return null;
    }

    public override AstNode VisitCompoundStatement(PythonParser.CompoundStatementContext context)
    {
        if (context.functionDef() != null) return Visit(context.functionDef());
        if (context.ifStatement() != null) return Visit(context.ifStatement());
        if (context.forStatement() != null) return Visit(context.forStatement());
        if (context.whileStatement() != null) return Visit(context.whileStatement());
        if (context.tryStatement() != null) return Visit(context.tryStatement());
        return null;
    }

    public override AstNode VisitSimpleStatement(PythonParser.SimpleStatementContext context)
    {
        foreach (var small in context.smallStatement())
        {
            var node = Visit(small);
            if (node is Stmt stmt) return stmt;
            if (node is Expr expr) return new ExprStmt(expr);
        }
        return null;
    }

    public override AstNode VisitSmallStatement(PythonParser.SmallStatementContext context)
    {
        if (context.assignment() != null) return Visit(context.assignment());
        if (context.typeDeclaration() != null) return Visit(context.typeDeclaration());
        if (context.jumpStatement() != null) return Visit(context.jumpStatement());
        if (context.PASS() != null) return null; // pass пропускаем
        if (context.expression() != null)
        {
            var node = Visit(context.expression());
            if (node is Stmt stmt) return stmt;
            if (node is Expr expr) return new ExprStmt(expr);
        }
        if (context.unparsedStatement() != null) return Visit(context.unparsedStatement());
        return null;
    }

    // --- Присваивания и объявления ---
    public override AstNode VisitNormalAssignment(PythonParser.NormalAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "=", ToExpr(context.expression()));

    public override AstNode VisitAddAssignment(PythonParser.AddAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "+=", ToExpr(context.expression()));

    public override AstNode VisitSubAssignment(PythonParser.SubAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "-=", ToExpr(context.expression()));

    public override AstNode VisitMultAssignment(PythonParser.MultAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "*=", ToExpr(context.expression()));

    public override AstNode VisitDivAssignment(PythonParser.DivAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "/=", ToExpr(context.expression()));

    public override AstNode VisitTypeDeclaration(PythonParser.TypeDeclarationContext context)
    {
        string name = context.ID().GetText();
        var initExpr = context.expression() != null ? ToExpr(context.expression()) : null;
        return new VarDeclStmt(false, name, initExpr);
    }

    // --- Управляющие конструкции ---
    public override AstNode VisitIfStatement(PythonParser.IfStatementContext context)
    {
        var exprs = context.expression();
        var blocks = context.block();

        // Рекурсивная сборка цепочки if -> else if (elif) -> else
        Stmt BuildIfChain(int index)
        {
            if (index >= exprs.Length)
            {
                // Если кончились условия, значит остался блок else (если он есть)
                // Блоки elif/if занимают индексы 0..exprs.Length-1, а else идет последним
                return blocks.Length > index ? ToStmt(blocks[index]) : null;
            }

            var condition = ToExpr(exprs[index]);
            var thenBranch = ToStmt(blocks[index]);
            
            // Следующая ветка — это либо следующий elif, либо финальный else
            var elseBranch = BuildIfChain(index + 1);

            return new IfStmt(condition, thenBranch, elseBranch);
        }

        return BuildIfChain(0);
    }

    public override AstNode VisitWhileStatement(PythonParser.WhileStatementContext context)
    {
        return new WhileStmt(ToExpr(context.expression()), ToStmt(context.block()));
    }

    public override AstNode VisitForStatement(PythonParser.ForStatementContext context)
    {
        string varName = context.ID().GetText();
        
        // Пытаемся понять, что за выражение идет после "in"
        var exprCtx = context.expression();
        Expr startExpr = new LiteralExpr(LiteralKind.Int, "0");
        Expr endExpr = ToExpr(exprCtx); // по умолчанию пока так

        // Проверяем, не вызов ли это range(...)
        // В сгенерированном парсере вызов функции попадает под FunctionCallContext
        if (exprCtx is PythonParser.FunctionCallContext callCtx)
        {
            string funcName = callCtx.expression(0)?.GetText() ?? "";
            if (funcName.Contains("range"))
            {
                // Если аргументов у range() больше одного, можно разобрать их отдельно
                var args = callCtx.expression();
                if (args.Length == 2) // range(stop) -> args[0] это сам range, args[1] это stop
                {
                    endExpr = ToExpr(args[1]);
                }
                else if (args.Length >= 3) // range(start, stop)
                {
                    startExpr = ToExpr(args[1]);
                    endExpr = ToExpr(args[2]);
                }
            }
        }

        return new ForRangeStmt(varName, startExpr, endExpr, ToStmt(context.block()));
    }

    public override AstNode VisitFunctionDef(PythonParser.FunctionDefContext context)
    {
        string name = context.ID().GetText();
        var parameters = new List<Parameter>();

        if (context.parameterList() != null)
        {
            foreach (var p in context.parameterList().parameter())
            {
                string pName = p.ID().GetText();
                string pType = p.type() != null ? p.type().GetText() : "int";
                parameters.Add(new Parameter(pName, ParseType(pType)));
            }
        }

        TypeRef returnType = context.type() != null ? ParseType(context.type().GetText()) : TypeRef.Void;
        var body = ToStmt(context.block()) as BlockStmt ?? new BlockStmt(new List<Stmt>());

        // Передаем IsExpressionBody = false, так как функции в Python имеют блок с телом
        return new FunctionDecl(name, parameters, returnType, body, IsExpressionBody: false);
    }

    public override AstNode VisitBlock(PythonParser.BlockContext context)
    {
        if (context.statements() != null)
        {
            return new BlockStmt(BuildStatements(context.statements()));
        }
        else if (context.simpleStatement() != null)
        {
            var stmt = ToStmt(context.simpleStatement());
            return new BlockStmt(stmt != null ? new List<Stmt> { stmt } : new List<Stmt>());
        }
        return new BlockStmt(new List<Stmt>());
    }

    public override AstNode VisitJumpStatement(PythonParser.JumpStatementContext context)
    {
        if (context.RETURN() != null) return new ReturnStmt(ToExpr(context.expression()));
        if (context.BREAK() != null) return new BreakStmt();
        if (context.CONTINUE() != null) return new ContinueStmt();
        if (context.RAISE() != null) return new ThrowStmt(ToExpr(context.expression()));
        return null;
    }

    public override AstNode VisitTryStatement(PythonParser.TryStatementContext context)
    {
        var body = ToStmt(context.block()) as BlockStmt;
        var catches = new List<CatchClause>();

        foreach (var cb in context.exceptBlock())
        {
            string exType = cb.type() != null ? cb.type().GetText() : "Exception";
            string exParam = cb.ID() != null ? cb.ID().GetText() : "e";
            catches.Add(new CatchClause(exParam, exType, ToStmt(cb.block()) as BlockStmt));
        }

        BlockStmt finallyBlock = context.finallyBlock() != null 
            ? ToStmt(context.finallyBlock().block()) as BlockStmt 
            : null;

        return new TryStmt(body, catches, finallyBlock);
    }

    // --- Выражения ---
    public override AstNode VisitParens(PythonParser.ParensContext context)
        => new ParenExpr(ToExpr(context.expression()));

    public override AstNode VisitFunctionCall(PythonParser.FunctionCallContext context)
        => new RawExpr(AntlrText.Raw(context));

    public override AstNode VisitMemberAccess(PythonParser.MemberAccessContext context)
        => new RawExpr(AntlrText.Raw(context));

    public override AstNode VisitUnaryMinus(PythonParser.UnaryMinusContext context)
        => new UnaryExpr("-", ToExpr(context.expression()));

    public override AstNode VisitUnaryNot(PythonParser.UnaryNotContext context)
        => new UnaryExpr("!", ToExpr(context.expression()));

    public override AstNode VisitMulDiv(PythonParser.MulDivContext context)
        => new BinaryExpr(context.GetChild(1).GetText(), ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    public override AstNode VisitAddSub(PythonParser.AddSubContext context)
        => new BinaryExpr(context.GetChild(1).GetText(), ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    public override AstNode VisitComparison(PythonParser.ComparisonContext context)
        => new BinaryExpr(context.GetChild(1).GetText(), ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    public override AstNode VisitAndLogical(PythonParser.AndLogicalContext context)
        => new BinaryExpr("&&", ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    public override AstNode VisitOrLogical(PythonParser.OrLogicalContext context)
        => new BinaryExpr("||", ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    public override AstNode VisitIdentifier(PythonParser.IdentifierContext context)
        => new IdentifierExpr(context.ID().GetText());

    public override AstNode VisitIntLiteral(PythonParser.IntLiteralContext context)
        => new LiteralExpr(LiteralKind.Int, context.INT().GetText());

    public override AstNode VisitFloatLiteral(PythonParser.FloatLiteralContext context)
        => new LiteralExpr(LiteralKind.Double, context.FLOAT().GetText());

    public override AstNode VisitStringLiteral(PythonParser.StringLiteralContext context)
        => new LiteralExpr(LiteralKind.String, context.STRING().GetText());

    public override AstNode VisitTrueLiteral(PythonParser.TrueLiteralContext context)
        => new LiteralExpr(LiteralKind.Int, "true");

    public override AstNode VisitFalseLiteral(PythonParser.FalseLiteralContext context)
        => new LiteralExpr(LiteralKind.Int, "false");

    public override AstNode VisitNoneLiteral(PythonParser.NoneLiteralContext context)
        => new LiteralExpr(LiteralKind.Int, "nullptr");

    public override AstNode VisitUnparsedStatement(PythonParser.UnparsedStatementContext context)
        => new UnsupportedStmt("Unparsed python statement", AntlrText.Raw(context));
}