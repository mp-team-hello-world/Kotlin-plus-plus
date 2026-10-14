using System.Collections.Generic;
using Antlr4.Runtime.Tree;

namespace Kotlin_plus_plus;

/// <summary>
/// Превращает дерево ANTLR (грамматика Kotlin) в язык-независимое IR.
/// Здесь живёт ВСЁ, что связано с синтаксисом Kotlin; ничего про C++ или Python.
/// (Это бывший CppGeneratorVisitor, из которого вынесена генерация кода.)
/// </summary>
public class KotlinAstBuilder : KotlinParserBaseVisitor<AstNode>
{
    // ---------- вспомогательные ----------

    private Expr ToExpr(IParseTree tree) => tree == null ? null : Visit(tree) as Expr;

    private Stmt ToStmt(IParseTree tree) => tree == null ? null : Visit(tree) as Stmt;

    private List<Stmt> BuildStatements(KotlinParser.StatementsContext statements)
    {
        var result = new List<Stmt>();
        if (statements == null) return result;

        foreach (var statement in statements.statement())
        {
            if (Visit(statement) is Stmt stmt) result.Add(stmt);
        }
        return result;
    }

    private static TypeRef ParseType(string kotlinType) => kotlinType switch
    {
        "Int" => new TypeRef(DataType.Int, kotlinType),
        "Boolean" => new TypeRef(DataType.Bool, kotlinType),
        "Long" => new TypeRef(DataType.Long, kotlinType),
        "Unit" => new TypeRef(DataType.Void, kotlinType),
        "Char" => new TypeRef(DataType.Char, kotlinType),
        "Float" => new TypeRef(DataType.Float, kotlinType),
        "Double" => new TypeRef(DataType.Double, kotlinType),
        "String" => new TypeRef(DataType.String, kotlinType),
        _ => new TypeRef(DataType.Unknown, kotlinType)
    };

    // ---------- корень и инструкции ----------

    // root: statements EOF;
    public override AstNode VisitRoot(KotlinParser.RootContext context)
    {
        return new ProgramNode(BuildStatements(context.statements()));
    }

    // statement: variableDeclaration | ifExpression | forStatement | block | expression | ...
    public override AstNode VisitStatement(KotlinParser.StatementContext context)
    {
        if (context.expression() != null)
        {
            var node = Visit(context.expression());
            if (node is Stmt stmt) return stmt;               // присваивания, return/break, try — это инструкции
            if (node is Expr expr) return new ExprStmt(expr); // обычное выражение как инструкция
            return null;
        }

        // Остальные варианты (if, for, while, объявления, блок...) — ребёнок и есть инструкция
        for (int i = 0; i < context.ChildCount; i++)
        {
            if (Visit(context.GetChild(i)) is Stmt child) return child;
        }
        return null;
    }

    // variableDeclaration: (VAL | VAR) ID ASSIGNMENT expression;
    public override AstNode VisitVariableDeclaration(KotlinParser.VariableDeclarationContext context)
    {
        if (context.expression() == null || context.ASSIGNMENT() == null)
        {
            return new UnsupportedStmt("Unsupported variable declaration syntax", AntlrText.Raw(context));
        }

        string exprText = context.expression().GetText();
        if (exprText.StartsWith("listOf") || exprText.StartsWith("mapOf"))
        {
            return new UnsupportedStmt(
                "Unsupported variable declaration (contains unimplemented features)",
                AntlrText.Raw(context));
        }

        string name = context.ID()?.GetText() ?? "unknownVar";
        bool isConst = context.VAL() != null;
        return new VarDeclStmt(isConst, name, ToExpr(context.expression()));
    }

    public override AstNode VisitNormalAssignment(KotlinParser.NormalAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "=", ToExpr(context.expression()));

    public override AstNode VisitAddAssignment(KotlinParser.AddAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "+=", ToExpr(context.expression()));

    public override AstNode VisitSubAssignment(KotlinParser.SubAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "-=", ToExpr(context.expression()));

    public override AstNode VisitMultAssignment(KotlinParser.MultAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "*=", ToExpr(context.expression()));

    public override AstNode VisitDivAssignment(KotlinParser.DivAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "/=", ToExpr(context.expression()));

    // block: LCURL statements RCURL;
    public override AstNode VisitBlock(KotlinParser.BlockContext context)
        => new BlockStmt(BuildStatements(context.statements()));

    public override AstNode VisitControlStructureBody(KotlinParser.ControlStructureBodyContext context)
    {
        if (context.block() != null) return Visit(context.block());
        if (context.statement() != null) return Visit(context.statement());
        return null;
    }

    // ifExpression: IF LPAREN expression RPAREN controlStructureBody (ELSE controlStructureBody)?
    public override AstNode VisitIfExpression(KotlinParser.IfExpressionContext context)
    {
        var bodies = context.controlStructureBody();
        Stmt thenBranch = ToStmt(bodies[0]);
        Stmt elseBranch = bodies.Length > 1 ? ToStmt(bodies[1]) : null;
        return new IfStmt(ToExpr(context.expression()), thenBranch, elseBranch);
    }

    public override AstNode VisitForStatement(KotlinParser.ForStatementContext context)
        => new ForRangeStmt(
            context.ID().GetText(),
            ToExpr(context.expression(0)),
            ToExpr(context.expression(1)),
            ToStmt(context.controlStructureBody()));

    public override AstNode VisitWhileStatement(KotlinParser.WhileStatementContext context)
        => new WhileStmt(ToExpr(context.expression()), ToStmt(context.controlStructureBody()));

    public override AstNode VisitDoWhileStatement(KotlinParser.DoWhileStatementContext context)
        => new DoWhileStmt(ToStmt(context.controlStructureBody()), ToExpr(context.expression()));

    public override AstNode VisitFunctionDeclaration(KotlinParser.FunctionDeclarationContext context)
    {
        var ids = context.ID();
        string name = ids[0].GetText();

        var parameters = new List<Parameter>();
        if (context.parameter() != null)
        {
            foreach (var param in context.parameter())
            {
                string paramName = param.ID(0)?.GetText() ?? "p";
                string paramType = param.ID(1)?.GetText() ?? "Unit";
                parameters.Add(new Parameter(paramName, ParseType(paramType)));
            }
        }

        // Тип возвращаемого значения — последний ID в объявлении (если ID больше одного)
        TypeRef returnType = ids.Length > 1
            ? ParseType(ids[ids.Length - 1].GetText())
            : TypeRef.Void;

        var functionBody = context.functionBody();
        BlockStmt body = null;
        bool isExpressionBody = false;

        if (functionBody != null && functionBody.block() != null)
        {
            body = Visit(functionBody.block()) as BlockStmt;
        }
        else if (functionBody != null && functionBody.ASSIGNMENT() != null)
        {
            // fun f() = expr   ==>   { return expr; }
            body = new BlockStmt(new List<Stmt> { new ReturnStmt(ToExpr(functionBody.expression())) });
            isExpressionBody = true;
        }

        return new FunctionDecl(name, parameters, returnType, body, isExpressionBody);
    }

    // ---------- прыжки и исключения ----------

    public override AstNode VisitJumpExpression(KotlinParser.JumpExpressionContext context)
    {
        if (context.RETURN() != null) return new ReturnStmt(ToExpr(context.expression()));
        if (context.CONTINUE() != null) return new ContinueStmt();
        if (context.BREAK() != null) return new BreakStmt();
        if (context.THROW() != null) return new ThrowStmt(ToExpr(context.expression()));
        return null;
    }

    public override AstNode VisitTryExpression(KotlinParser.TryExpressionContext context)
    {
        var catches = new List<CatchClause>();

        foreach (var catchBlock in context.catchBlock())
        {
            string paramName = "e";
            string exceptionType = null;

            var ids = catchBlock.ID();
            if (ids.Length == 1)
            {
                paramName = ids[0].GetText();              // catch (e) — тип не указан
            }
            else if (ids.Length >= 2)
            {
                paramName = ids[0].GetText();
                exceptionType = ids[1].GetText();
            }

            catches.Add(new CatchClause(paramName, exceptionType, Visit(catchBlock.block()) as BlockStmt));
        }

        BlockStmt finallyBlock = context.finallyBlock() != null
            ? Visit(context.finallyBlock().block()) as BlockStmt
            : null;

        return new TryStmt(Visit(context.block()) as BlockStmt, catches, finallyBlock);
    }

    // ---------- то, что не переводится ----------

    public override AstNode VisitUnparsedStatement(KotlinParser.UnparsedStatementContext context)
    {
        string raw = AntlrText.Raw(context);
        return string.IsNullOrWhiteSpace(raw)
            ? null
            : new UnsupportedStmt("Unsupported statement construction", raw);
    }

    // Вызовы функций пока передаются как исходный текст (как и раньше)
    public override AstNode VisitFunctionCall(KotlinParser.FunctionCallContext context)
        => new RawExpr(AntlrText.Raw(context));

    // ---------- выражения ----------

    public override AstNode VisitIdentifier(KotlinParser.IdentifierContext context)
        => new IdentifierExpr(context.ID().GetText());

    public override AstNode VisitIntLiteral(KotlinParser.IntLiteralContext context)
        => new LiteralExpr(LiteralKind.Int, context.INT().GetText());

    public override AstNode VisitDoubleLiteral(KotlinParser.DoubleLiteralContext context)
        => new LiteralExpr(LiteralKind.Double, context.DOUBLE().GetText());

    public override AstNode VisitStringLiteral(KotlinParser.StringLiteralContext context)
        => new LiteralExpr(LiteralKind.String, context.STRING_LITERAL().GetText());

    public override AstNode VisitParens(KotlinParser.ParensContext context)
        => new ParenExpr(ToExpr(context.expression()));

    public override AstNode VisitUnaryMinus(KotlinParser.UnaryMinusContext context)
        => new UnaryExpr("-", ToExpr(context.expression()));

    public override AstNode VisitUnaryNot(KotlinParser.UnaryNotContext context)
        => new UnaryExpr("!", ToExpr(context.expression()));

    public override AstNode VisitMulDiv(KotlinParser.MulDivContext context)
        => new BinaryExpr(context.GetChild(1).GetText(), ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    public override AstNode VisitAddSub(KotlinParser.AddSubContext context)
        => new BinaryExpr(context.GetChild(1).GetText(), ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    public override AstNode VisitComparison(KotlinParser.ComparisonContext context)
        => new BinaryExpr(context.GetChild(1).GetText(), ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    // В грамматике есть две пары меток для && и || — обрабатываем обе одинаково
    public override AstNode VisitAndOp(KotlinParser.AndOpContext ctx)
        => new BinaryExpr("&&", ToExpr(ctx.expression(0)), ToExpr(ctx.expression(1)));

    public override AstNode VisitOrOp(KotlinParser.OrOpContext ctx)
        => new BinaryExpr("||", ToExpr(ctx.expression(0)), ToExpr(ctx.expression(1)));

    public override AstNode VisitAndLogical(KotlinParser.AndLogicalContext ctx)
        => new BinaryExpr("&&", ToExpr(ctx.expression(0)), ToExpr(ctx.expression(1)));

    public override AstNode VisitOrLogical(KotlinParser.OrLogicalContext ctx)
        => new BinaryExpr("||", ToExpr(ctx.expression(0)), ToExpr(ctx.expression(1)));
}
