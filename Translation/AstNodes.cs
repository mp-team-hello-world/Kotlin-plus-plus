using System.Collections.Generic;

namespace Kotlin_plus_plus;

// =====================================================================================
//  ЯЗЫКОНЕЗАВИСИМОЕ ПРОМЕЖУТОЧНОЕ ПРЕДСТАВЛЕНИЕ (IR / AST)
//
//  Дерево ANTLR — это дерево КОНКРЕТНОЙ грамматики (Kotlin). По нему нельзя писать
//  визиторы "в любой язык": оно завязано на KotlinParser. Поэтому конвейер такой:
//
//      исходный текст ──[Frontend: ANTLR + KotlinAstBuilder]──►  IR  ──[Backend]──► целевой код
//
//  Frontend знает только исходный язык, Backend — только целевой. Друг про друга они
//  не знают, общий язык у них — вот эти классы. Поэтому N исходных + M целевых языков
//  = N + M классов, а не N × M копий визитора.
//
//  Договорённости IR (backend'ы на них полагаются):
//    * Операторы хранятся каноническими C-подобными символами: + - * / % < > <= >= == !=
//      && || (бинарные), - ! (унарные). Backend сам превращает их в свой синтаксис
//      (например, Python: && -> and, ! -> not).
//    * Поля, помеченные "может быть null", допускают null.
// =====================================================================================

public enum DataType { Int, Long, Float, Double, Bool, Char, String, Void, Unknown }

public enum LiteralKind { Int, Double, String }

/// <summary>Тип в IR. SourceName — как тип назывался в исходнике (нужен для Unknown).</summary>
public sealed record TypeRef(DataType Kind, string SourceName)
{
    public static readonly TypeRef Void = new(DataType.Void, "Unit");
}

public sealed record Parameter(string Name, TypeRef Type);

/// <summary>catch-блок. ExceptionType == null — перехват "всего" (catch без типа).</summary>
public sealed record CatchClause(string ParamName, string ExceptionType, BlockStmt Body);

public abstract record AstNode;

/// <summary>Вся программа: список инструкций верхнего уровня (функции — тоже инструкции).</summary>
public sealed record ProgramNode(IReadOnlyList<Stmt> Statements) : AstNode;

// ------------------------------------------ Visitors ------------------------------------------
// Интерфейсы, а не switch по типам: если появится новый узел IR, ВСЕ backend'ы
// перестанут компилироваться, пока не научатся его обрабатывать. Забыть узел нельзя.

public interface IExprVisitor<T>
{
    T VisitLiteral(LiteralExpr node);
    T VisitIdentifier(IdentifierExpr node);
    T VisitParen(ParenExpr node);
    T VisitUnary(UnaryExpr node);
    T VisitBinary(BinaryExpr node);
    T VisitRaw(RawExpr node);
}

public interface IStmtVisitor
{
    void VisitExprStmt(ExprStmt node);
    void VisitVarDecl(VarDeclStmt node);
    void VisitAssign(AssignStmt node);
    void VisitBlock(BlockStmt node);
    void VisitIf(IfStmt node);
    void VisitForRange(ForRangeStmt node);
    void VisitWhile(WhileStmt node);
    void VisitDoWhile(DoWhileStmt node);
    void VisitFunction(FunctionDecl node);
    void VisitReturn(ReturnStmt node);
    void VisitBreak(BreakStmt node);
    void VisitContinue(ContinueStmt node);
    void VisitThrow(ThrowStmt node);
    void VisitTry(TryStmt node);
    void VisitUnsupported(UnsupportedStmt node);
}

// ------------------------------------------ Выражения ------------------------------------------

public abstract record Expr : AstNode
{
    public abstract T Accept<T>(IExprVisitor<T> visitor);
}

/// <summary>Литерал. Text — как записан в исходнике (для строки — вместе с кавычками).</summary>
public sealed record LiteralExpr(LiteralKind Kind, string Text) : Expr
{
    public override T Accept<T>(IExprVisitor<T> v) => v.VisitLiteral(this);
}

public sealed record IdentifierExpr(string Name) : Expr
{
    public override T Accept<T>(IExprVisitor<T> v) => v.VisitIdentifier(this);
}

public sealed record ParenExpr(Expr Inner) : Expr
{
    public override T Accept<T>(IExprVisitor<T> v) => v.VisitParen(this);
}

public sealed record UnaryExpr(string Op, Expr Operand) : Expr
{
    public override T Accept<T>(IExprVisitor<T> v) => v.VisitUnary(this);
}

public sealed record BinaryExpr(string Op, Expr Left, Expr Right) : Expr
{
    public override T Accept<T>(IExprVisitor<T> v) => v.VisitBinary(this);
}

/// <summary>
/// Кусок исходного кода "как есть" (сейчас так передаются вызовы функций — грамматика
/// не разбирает аргументы). Следующий шаг развития: заменить на CallExpr(name, args),
/// тогда backend'ы смогут мапить функции (println -> print / std::cout).
/// </summary>
public sealed record RawExpr(string SourceText) : Expr
{
    public override T Accept<T>(IExprVisitor<T> v) => v.VisitRaw(this);
}

// ------------------------------------------ Инструкции ------------------------------------------

public abstract record Stmt : AstNode
{
    public abstract void Accept(IStmtVisitor visitor);
}

public sealed record ExprStmt(Expr Expression) : Stmt
{
    public override void Accept(IStmtVisitor v) => v.VisitExprStmt(this);
}

/// <summary>val/var. IsConst == true для val.</summary>
public sealed record VarDeclStmt(bool IsConst, string Name, Expr Init) : Stmt
{
    public override void Accept(IStmtVisitor v) => v.VisitVarDecl(this);
}

/// <summary>Присваивание. Op: "=", "+=", "-=", "*=", "/=".</summary>
public sealed record AssignStmt(string Target, string Op, Expr Value) : Stmt
{
    public override void Accept(IStmtVisitor v) => v.VisitAssign(this);
}

public sealed record BlockStmt(IReadOnlyList<Stmt> Statements) : Stmt
{
    public override void Accept(IStmtVisitor v) => v.VisitBlock(this);
}

/// <summary>Then/Else — любая инструкция (BlockStmt или одиночная). Else может быть null.</summary>
public sealed record IfStmt(Expr Condition, Stmt Then, Stmt Else) : Stmt
{
    public override void Accept(IStmtVisitor v) => v.VisitIf(this);
}

/// <summary>for (v in Start..EndInclusive) — конец диапазона ВКЛЮЧИТЕЛЬНО.</summary>
public sealed record ForRangeStmt(string Variable, Expr Start, Expr EndInclusive, Stmt Body) : Stmt
{
    public override void Accept(IStmtVisitor v) => v.VisitForRange(this);
}

/// <summary>Body может быть null (пустое тело).</summary>
public sealed record WhileStmt(Expr Condition, Stmt Body) : Stmt
{
    public override void Accept(IStmtVisitor v) => v.VisitWhile(this);
}

public sealed record DoWhileStmt(Stmt Body, Expr Condition) : Stmt
{
    public override void Accept(IStmtVisitor v) => v.VisitDoWhile(this);
}

/// <summary>
/// Body может быть null. Для функции с телом-выражением (fun f() = expr)
/// Body = { return expr; }, а IsExpressionBody = true.
/// </summary>
public sealed record FunctionDecl(
    string Name,
    IReadOnlyList<Parameter> Parameters,
    TypeRef ReturnType,
    BlockStmt Body,
    bool IsExpressionBody) : Stmt
{
    public override void Accept(IStmtVisitor v) => v.VisitFunction(this);
}

/// <summary>Value == null — голый return.</summary>
public sealed record ReturnStmt(Expr Value) : Stmt
{
    public override void Accept(IStmtVisitor v) => v.VisitReturn(this);
}

public sealed record BreakStmt : Stmt
{
    public override void Accept(IStmtVisitor v) => v.VisitBreak(this);
}

public sealed record ContinueStmt : Stmt
{
    public override void Accept(IStmtVisitor v) => v.VisitContinue(this);
}

public sealed record ThrowStmt(Expr Value) : Stmt
{
    public override void Accept(IStmtVisitor v) => v.VisitThrow(this);
}

/// <summary>Finally может быть null.</summary>
public sealed record TryStmt(BlockStmt Body, IReadOnlyList<CatchClause> Catches, BlockStmt Finally) : Stmt
{
    public override void Accept(IStmtVisitor v) => v.VisitTry(this);
}

/// <summary>Конструкция, которую frontend не смог/не захотел перевести. Backend оформляет её комментарием.</summary>
public sealed record UnsupportedStmt(string Description, string SourceText) : Stmt
{
    public override void Accept(IStmtVisitor v) => v.VisitUnsupported(this);
}
