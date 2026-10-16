using System.Collections.Generic;
using Antlr4.Runtime.Tree;

namespace Kotlin_plus_plus;

/// <summary>
/// Превращает дерево ANTLR (грамматика C#) в язык-независимое IR.
/// Здесь живёт всё, что связано с синтаксисом C#; ничего про целевые языки.
/// </summary>
public class CSharpAstBuilder : CSharpParserBaseVisitor<AstNode>
{
    // ---------- вспомогательные ----------

    private Expr ToExpr(IParseTree tree) => tree == null ? null : Visit(tree) as Expr;

    private Stmt ToStmt(IParseTree tree) => tree == null ? null : Visit(tree) as Stmt;

    private List<Stmt> BuildStatements(CSharpParser.StatementsContext statements)
    {
        var result = new List<Stmt>();
        if (statements == null) return result;

        foreach (var statement in statements.statement())
        {
            if (Visit(statement) is Stmt stmt) result.Add(stmt);
        }
        return result;
    }

    private static TypeRef ParseType(string csharpType) => csharpType switch
    {
        "int" => new TypeRef(DataType.Int, csharpType),
        "long" => new TypeRef(DataType.Long, csharpType),
        "float" => new TypeRef(DataType.Float, csharpType),
        "double" => new TypeRef(DataType.Double, csharpType),
        "bool" => new TypeRef(DataType.Bool, csharpType),
        "char" => new TypeRef(DataType.Char, csharpType),
        "string" => new TypeRef(DataType.String, csharpType),
        "void" => new TypeRef(DataType.Void, csharpType),
        _ => new TypeRef(DataType.Unknown, csharpType)
    };

    // i++ / ++i / i-- / --i на уровне инструкции -> IncDecStmt.
    // Если операнд не простая переменная (obj.count++) — null, и выражение уйдёт как есть.
    private IncDecStmt TryIncDec(CSharpParser.ExpressionContext expression)
    {
        CSharpParser.ExpressionContext operand = null;
        bool isIncrement = false;

        switch (expression)
        {
            case CSharpParser.PostIncrementContext postInc:
                operand = postInc.expression();
                isIncrement = true;
                break;
            case CSharpParser.PreIncrementContext preInc:
                operand = preInc.expression();
                isIncrement = true;
                break;
            case CSharpParser.PostDecrementContext postDec:
                operand = postDec.expression();
                break;
            case CSharpParser.PreDecrementContext preDec:
                operand = preDec.expression();
                break;
        }

        return operand is CSharpParser.IdentifierContext id
            ? new IncDecStmt(id.ID().GetText(), isIncrement)
            : null;
    }

    // Выражение, которое используется как инструкция (x++; foo(); и т.п.)
    private Stmt ExpressionAsStatement(CSharpParser.ExpressionContext expression)
    {
        var incDec = TryIncDec(expression);
        if (incDec != null) return incDec;

        return Visit(expression) is Expr expr ? new ExprStmt(expr) : null;
    }

    private AstNode BuildVarDecl(
        bool isConst,
        CSharpParser.TypeContext type,
        ITerminalNode id,
        CSharpParser.ExpressionContext init)
    {
        // var x = ...  -> тип не указан (null)
        TypeRef declaredType = type != null ? ParseType(type.GetText()) : null;
        return new VarDeclStmt(isConst, id.GetText(), ToExpr(init), declaredType);
    }

    // ---------- корень и инструкции ----------

    // root: statements EOF;
    public override AstNode VisitRoot(CSharpParser.RootContext context)
        => new ProgramNode(BuildStatements(context.statements()));

    public override AstNode VisitStatement(CSharpParser.StatementContext context)
    {
        if (context.expression() != null)
        {
            return ExpressionAsStatement(context.expression());
        }

        // Остальные варианты (объявление, присваивание, if, for, блок...) — ребёнок и есть инструкция.
        // Одиночная ';' и прочие терминалы дают null и пропускаются.
        for (int i = 0; i < context.ChildCount; i++)
        {
            if (Visit(context.GetChild(i)) is Stmt child) return child;
        }
        return null;
    }

    // variableDeclaration: (CONST? type | VAR) ID (ASSIGNMENT expression)? SEMICOLON;
    public override AstNode VisitVariableDeclaration(CSharpParser.VariableDeclarationContext context)
        => BuildVarDecl(context.CONST() != null, context.type(), context.ID(), context.expression());

    public override AstNode VisitVariableDeclarationNoSemi(CSharpParser.VariableDeclarationNoSemiContext context)
        => BuildVarDecl(context.CONST() != null, context.type(), context.ID(), context.expression());

    public override AstNode VisitNormalAssignment(CSharpParser.NormalAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "=", ToExpr(context.expression()));

    public override AstNode VisitAddAssignment(CSharpParser.AddAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "+=", ToExpr(context.expression()));

    public override AstNode VisitSubAssignment(CSharpParser.SubAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "-=", ToExpr(context.expression()));

    public override AstNode VisitMultAssignment(CSharpParser.MultAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "*=", ToExpr(context.expression()));

    public override AstNode VisitDivAssignment(CSharpParser.DivAssignmentContext context)
        => new AssignStmt(context.ID().GetText(), "/=", ToExpr(context.expression()));

    // block: LCURL statements RCURL;
    public override AstNode VisitBlock(CSharpParser.BlockContext context)
        => new BlockStmt(BuildStatements(context.statements()));

    // controlStructureBody: block | statement;
    public override AstNode VisitControlStructureBody(CSharpParser.ControlStructureBodyContext context)
    {
        if (context.block() != null) return Visit(context.block());
        if (context.statement() != null) return Visit(context.statement());
        return null;
    }

    // ifStatement: IF LPAREN expression RPAREN controlStructureBody (ELSE controlStructureBody)?
    public override AstNode VisitIfStatement(CSharpParser.IfStatementContext context)
    {
        var bodies = context.controlStructureBody();
        Stmt thenBranch = ToStmt(bodies[0]);
        Stmt elseBranch = bodies.Length > 1 ? ToStmt(bodies[1]) : null;
        return new IfStmt(ToExpr(context.expression()), thenBranch, elseBranch);
    }

    // forStatement: FOR LPAREN forInit? SEMICOLON expression? SEMICOLON forUpdate? RPAREN controlStructureBody
    public override AstNode VisitForStatement(CSharpParser.ForStatementContext context)
        => new ForStmt(
            ToStmt(context.forInit()),
            ToExpr(context.expression()),
            ToStmt(context.forUpdate()),
            ToStmt(context.controlStructureBody()));

    // forInit: variableDeclarationNoSemi | assignment | expression;
    public override AstNode VisitForInit(CSharpParser.ForInitContext context)
    {
        if (context.variableDeclarationNoSemi() != null) return Visit(context.variableDeclarationNoSemi());
        if (context.assignment() != null) return Visit(context.assignment());
        return context.expression() != null ? ExpressionAsStatement(context.expression()) : null;
    }

    // forUpdate: assignment | expression;
    public override AstNode VisitForUpdate(CSharpParser.ForUpdateContext context)
    {
        if (context.assignment() != null) return Visit(context.assignment());
        return context.expression() != null ? ExpressionAsStatement(context.expression()) : null;
    }

    // foreachStatement: FOREACH LPAREN (VAR | type) ID IN expression RPAREN controlStructureBody
    public override AstNode VisitForeachStatement(CSharpParser.ForeachStatementContext context)
    {
        TypeRef variableType = context.type() != null ? ParseType(context.type().GetText()) : null;
        return new ForEachStmt(
            context.ID().GetText(),
            variableType,
            ToExpr(context.expression()),
            ToStmt(context.controlStructureBody()));
    }

    public override AstNode VisitWhileStatement(CSharpParser.WhileStatementContext context)
        => new WhileStmt(ToExpr(context.expression()), ToStmt(context.controlStructureBody()));

    public override AstNode VisitDoWhileStatement(CSharpParser.DoWhileStatementContext context)
        => new DoWhileStmt(ToStmt(context.controlStructureBody()), ToExpr(context.expression()));

    // functionDeclaration: modifier* type ID LPAREN parameterList? RPAREN (block | ARROW expression SEMICOLON);
    public override AstNode VisitFunctionDeclaration(CSharpParser.FunctionDeclarationContext context)
    {
        // Модификаторы (public, static...) в IR не нужны.
        // Точка входа C# называется Main, а в IR — main (см. соглашения в AstNodes.cs).
        string name = context.ID().GetText();
        if (name == "Main") name = "main";

        TypeRef returnType = ParseType(context.type().GetText());

        var parameters = new List<Parameter>();
        if (context.parameterList() != null)
        {
            foreach (var param in context.parameterList().parameter())
            {
                parameters.Add(new Parameter(param.ID().GetText(), ParseType(param.type().GetText())));
            }
        }

        BlockStmt body = null;
        bool isExpressionBody = false;

        if (context.block() != null)
        {
            body = Visit(context.block()) as BlockStmt;
        }
        else if (context.ARROW() != null)
        {
            // int F() => expr;  ==>  { return expr; }      void F() => expr;  ==>  { expr; }
            Expr expr = ToExpr(context.expression());
            Stmt single = returnType.Kind == DataType.Void ? new ExprStmt(expr) : new ReturnStmt(expr);
            body = new BlockStmt(new List<Stmt> { single });
            isExpressionBody = true;
        }

        return new FunctionDecl(name, parameters, returnType, body, isExpressionBody);
    }

    // ---------- прыжки и исключения ----------

    // jumpStatement: RETURN expression? ; | THROW expression ; | BREAK ; | CONTINUE ;
    public override AstNode VisitJumpStatement(CSharpParser.JumpStatementContext context)
    {
        if (context.RETURN() != null) return new ReturnStmt(ToExpr(context.expression()));
        if (context.THROW() != null) return new ThrowStmt(ToExpr(context.expression()));
        if (context.BREAK() != null) return new BreakStmt();
        if (context.CONTINUE() != null) return new ContinueStmt();
        return null;
    }

    // tryStatement: TRY block (catchBlock)+ (finallyBlock)? | TRY block finallyBlock;
    public override AstNode VisitTryStatement(CSharpParser.TryStatementContext context)
    {
        var catches = new List<CatchClause>();

        foreach (var catchBlock in context.catchBlock())
        {
            // catch { } — тип не указан (ловим всё); catch (Exception) — имя не указано
            string exceptionType = catchBlock.type()?.GetText();
            string paramName = catchBlock.ID()?.GetText() ?? "e";
            catches.Add(new CatchClause(paramName, exceptionType, Visit(catchBlock.block()) as BlockStmt));
        }

        BlockStmt finallyBlock = context.finallyBlock() != null
            ? Visit(context.finallyBlock().block()) as BlockStmt
            : null;

        return new TryStmt(Visit(context.block()) as BlockStmt, catches, finallyBlock);
    }

    // ---------- то, что не переводится ----------

    public override AstNode VisitUnparsedStatement(CSharpParser.UnparsedStatementContext context)
    {
        string raw = AntlrText.Raw(context);
        return string.IsNullOrWhiteSpace(raw)
            ? null
            : new UnsupportedStmt("Unsupported statement construction", raw);
    }

    // Вызовы и обращения к членам (Console.WriteLine(...)) пока передаются как исходный текст
    public override AstNode VisitFunctionCall(CSharpParser.FunctionCallContext context)
        => new RawExpr(AntlrText.Raw(context));

    public override AstNode VisitMemberAccess(CSharpParser.MemberAccessContext context)
        => new RawExpr(AntlrText.Raw(context));

    // i++ внутри более сложного выражения (a[i++], x = i++) — как исходный текст.
    // На уровне инструкции они превращаются в IncDecStmt (см. TryIncDec).
    public override AstNode VisitPreIncrement(CSharpParser.PreIncrementContext context)
        => new RawExpr(AntlrText.Raw(context));

    public override AstNode VisitPreDecrement(CSharpParser.PreDecrementContext context)
        => new RawExpr(AntlrText.Raw(context));

    public override AstNode VisitPostIncrement(CSharpParser.PostIncrementContext context)
        => new RawExpr(AntlrText.Raw(context));

    public override AstNode VisitPostDecrement(CSharpParser.PostDecrementContext context)
        => new RawExpr(AntlrText.Raw(context));

    // ---------- выражения ----------

    public override AstNode VisitParens(CSharpParser.ParensContext context)
        => new ParenExpr(ToExpr(context.expression()));

    public override AstNode VisitUnaryMinus(CSharpParser.UnaryMinusContext context)
        => new UnaryExpr("-", ToExpr(context.expression()));

    public override AstNode VisitUnaryNot(CSharpParser.UnaryNotContext context)
        => new UnaryExpr("!", ToExpr(context.expression()));

    public override AstNode VisitMulDiv(CSharpParser.MulDivContext context)
        => new BinaryExpr(context.GetChild(1).GetText(), ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    public override AstNode VisitAddSub(CSharpParser.AddSubContext context)
        => new BinaryExpr(context.GetChild(1).GetText(), ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    public override AstNode VisitComparison(CSharpParser.ComparisonContext context)
        => new BinaryExpr(context.GetChild(1).GetText(), ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    public override AstNode VisitAndLogical(CSharpParser.AndLogicalContext context)
        => new BinaryExpr("&&", ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    public override AstNode VisitOrLogical(CSharpParser.OrLogicalContext context)
        => new BinaryExpr("||", ToExpr(context.expression(0)), ToExpr(context.expression(1)));

    public override AstNode VisitIdentifier(CSharpParser.IdentifierContext context)
        => new IdentifierExpr(context.ID().GetText());

    public override AstNode VisitIntLiteral(CSharpParser.IntLiteralContext context)
        => new LiteralExpr(LiteralKind.Int, context.INT().GetText());

    public override AstNode VisitDoubleLiteral(CSharpParser.DoubleLiteralContext context)
        => new LiteralExpr(LiteralKind.Double, context.DOUBLE().GetText());

    public override AstNode VisitStringLiteral(CSharpParser.StringLiteralContext context)
        => new LiteralExpr(LiteralKind.String, context.STRING_LITERAL().GetText());

    public override AstNode VisitCharLiteral(CSharpParser.CharLiteralContext context)
        => new LiteralExpr(LiteralKind.Char, context.CHAR_LITERAL().GetText());

    public override AstNode VisitBoolLiteral(CSharpParser.BoolLiteralContext context)
        => new LiteralExpr(LiteralKind.Bool, context.BOOL_LITERAL().GetText());
}