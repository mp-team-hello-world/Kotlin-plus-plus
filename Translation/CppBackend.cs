using System.Collections.Generic;
using System.Linq;

namespace Kotlin_plus_plus;

/// <summary>IR -> C++. Формат вывода намеренно совпадает с прежним CppGeneratorVisitor.</summary>
public sealed class CppBackend : ITargetBackend
{
    public string Generate(ProgramNode program) => new Generator().Generate(program);

    private sealed class Generator : IStmtVisitor, IExprVisitor<string>
    {
        private readonly CodeWriter _w = new();
        private readonly List<string> _prototypes = new();
        private bool _hasExceptions;

        public string Generate(ProgramNode program)
        {
            EmitProgram(program);
            return BuildResult();
        }

        private string BuildResult()
        {
            var result = new List<string>();

            if (_hasExceptions)
            {
                result.Add("#include <exception>");
                result.Add("");
            }

            result.Add("using namespace std;");
            result.Add("");
            result.AddRange(_prototypes);
            result.Add("");
            result.AddRange(_w.Lines);

            return string.Join("\n", result);
        }

        private void Emit(Stmt stmt) => stmt?.Accept(this);

        private string Render(Expr expr) => expr == null ? string.Empty : expr.Accept(this);

        // ---------- программа: main и прототипы ----------

        private void EmitProgram(ProgramNode program)
        {
            var statements = program.Statements;

            if (statements.Count == 0)
            {
                EmitEmptyMain();
                return;
            }

            bool hasMain = statements.OfType<FunctionDecl>().Any(f => f.Name == "main");

            if (hasMain)
            {
                // main уже есть — выводим всё в исходном порядке
                foreach (var stmt in statements) Emit(stmt);
                return;
            }

            // main нет: сначала функции, потом всё остальное заворачиваем в int main()
            foreach (var function in statements.OfType<FunctionDecl>()) Emit(function);

            _w.Line("int main()");
            _w.Line("{");
            _w.Indent();
            foreach (var stmt in statements)
            {
                if (stmt is not FunctionDecl) Emit(stmt);
            }
            _w.Line("return 0;");
            _w.Dedent();
            _w.Line("}");
        }

        private void EmitEmptyMain()
        {
            _w.Line("int main()");
            _w.Line("{");
            _w.Indent();
            _w.Line("return 0;");
            _w.Dedent();
            _w.Line("}");
        }

        // Тело if/for/while: блок — как есть, одиночная инструкция — с отступом
        private void EmitBody(Stmt body)
        {
            if (body == null) return;

            if (body is BlockStmt)
            {
                Emit(body);
            }
            else
            {
                _w.Indent();
                Emit(body);
                _w.Dedent();
            }
        }

        private static string ToCppType(TypeRef type) => type.Kind switch
        {
            DataType.Int => "int",
            DataType.Bool => "bool",
            DataType.Long => "long",
            DataType.Void => "void",
            DataType.Char => "char",
            DataType.Float => "float",
            DataType.Double => "double",
            DataType.String => "std::string",
            _ => "auto"
        };

        // ---------- выражения ----------

        public string VisitLiteral(LiteralExpr node) => node.Text;

        public string VisitIdentifier(IdentifierExpr node) => node.Name;

        public string VisitParen(ParenExpr node) => $"({Render(node.Inner)})";

        public string VisitUnary(UnaryExpr node) => $"{node.Op}{Render(node.Operand)}";

        public string VisitBinary(BinaryExpr node) => $"{Render(node.Left)} {node.Op} {Render(node.Right)}";

        public string VisitRaw(RawExpr node) => node.SourceText;

        // ---------- инструкции ----------

        public void VisitExprStmt(ExprStmt node)
        {
            string text = Render(node.Expression);
            if (!string.IsNullOrEmpty(text))
            {
                _w.Line($"{text};");
            }
        }

        public void VisitVarDecl(VarDeclStmt node) => _w.Line(DeclText(node) + ";");

        public void VisitAssign(AssignStmt node) => _w.Line(AssignText(node) + ";");

        public void VisitIncDec(IncDecStmt node) => _w.Line(IncDecText(node) + ";");

        // Тексты без ';' — переиспользуются в заголовке for (...)
        private string DeclText(VarDeclStmt node)
        {
            string prefix = node.IsConst ? "const " : "";
            string declared = DeclaredType(node);
            return node.Init == null
                ? $"{prefix}{declared} {node.Name}"
                : $"{prefix}{declared} {node.Name} = {Render(node.Init)}";
        }

        // Тип не указан (Kotlin, C# var) -> auto, как и раньше
        private static string DeclaredType(VarDeclStmt node)
        {
            if (node.Type == null) return "auto";
            if (node.Type.Kind != DataType.Unknown) return ToCppType(node.Type);
            return node.Init == null ? node.Type.SourceName : "auto";
        }

        private string AssignText(AssignStmt node) => $"{node.Target} {node.Op} {Render(node.Value)}";

        private static string IncDecText(IncDecStmt node) => node.Target + (node.IsIncrement ? "++" : "--");

        private string Inline(Stmt stmt) => stmt switch
        {
            VarDeclStmt v => DeclText(v),
            AssignStmt a => AssignText(a),
            IncDecStmt i => IncDecText(i),
            ExprStmt e => Render(e.Expression),
            _ => string.Empty
        };

        public void VisitBlock(BlockStmt node)
        {
            _w.Line("{");
            _w.Indent();
            foreach (var stmt in node.Statements) Emit(stmt);
            _w.Dedent();
            _w.Line("}");
        }

        public void VisitIf(IfStmt node)
        {
            _w.Line($"if ({Render(node.Condition)})");
            EmitBody(node.Then);

            if (node.Else != null)
            {
                _w.Line("else");
                EmitBody(node.Else);
            }
        }

        public void VisitForRange(ForRangeStmt node)
        {
            string v = node.Variable;
            _w.Line($"for (int {v} = {Render(node.Start)}; {v} <= {Render(node.EndInclusive)}; {v}++)");
            EmitBody(node.Body);
        }

        public void VisitFor(ForStmt node)
        {
            _w.Line($"for ({Inline(node.Init)}; {Render(node.Condition)}; {Inline(node.Update)})");
            EmitBody(node.Body);
        }

        public void VisitForEach(ForEachStmt node)
        {
            string type = node.VariableType != null && node.VariableType.Kind != DataType.Unknown
                ? ToCppType(node.VariableType)
                : "auto";
            _w.Line($"for ({type} {node.Variable} : {Render(node.Iterable)})");
            EmitBody(node.Body);
        }

        public void VisitWhile(WhileStmt node)
        {
            _w.Line($"while ({Render(node.Condition)})");
            if (node.Body != null) EmitBody(node.Body);
            else _w.Line(";");
        }

        public void VisitDoWhile(DoWhileStmt node)
        {
            _w.Line("do");
            if (node.Body != null) EmitBody(node.Body);
            else _w.Line(";");
            _w.Line($"while ({Render(node.Condition)});");
        }

        public void VisitFunction(FunctionDecl node)
        {
            bool isMain = node.Name == "main";

            // main в C++ всегда возвращает int
            string returnType = isMain ? "int" : ToCppType(node.ReturnType);
            string parameters = string.Join(", ", node.Parameters.Select(p => $"{ToCppType(p.Type)} {p.Name}"));
            string signature = $"{returnType} {node.Name}({parameters})";

            if (!isMain) _prototypes.Add(signature + ";");
            _w.Line(signature);

            if (node.Body != null)
            {
                if (isMain && !node.IsExpressionBody)
                {
                    // в конец main гарантированно добавляем return 0
                    _w.Line("{");
                    _w.Indent();
                    foreach (var stmt in node.Body.Statements) Emit(stmt);
                    _w.Line("return 0;");
                    _w.Dedent();
                    _w.Line("}");
                }
                else
                {
                    Emit(node.Body);
                }
            }

            _w.Line("");
        }

        public void VisitReturn(ReturnStmt node)
        {
            _w.Line(node.Value == null ? "return;" : $"return {Render(node.Value)};");
        }

        public void VisitBreak(BreakStmt node) => _w.Line("break;");

        public void VisitContinue(ContinueStmt node) => _w.Line("continue;");

        public void VisitThrow(ThrowStmt node)
        {
            _hasExceptions = true;
            _w.Line($"// throw {Render(node.Value)}");
        }

        public void VisitTry(TryStmt node)
        {
            _hasExceptions = true;
            _w.Line("try");
            Emit(node.Body);

            foreach (var clause in node.Catches)
            {
                if (string.IsNullOrEmpty(clause.ExceptionType))
                {
                    _w.Line("catch (...)");
                    Emit(clause.Body);
                    continue;
                }

                string cppType = clause.ExceptionType switch
                {
                    "Exception" => "const std::exception&",
                    "IOException" => "const std::ios_base::failure&",
                    _ => "const std::exception&"
                };

                _w.Line($"catch ({cppType} {clause.ParamName})");
                Emit(clause.Body);
            }

            if (node.Finally != null)
            {
                _w.Line("// finally");
                Emit(node.Finally);
            }
        }

        public void VisitUnsupported(UnsupportedStmt node)
        {
            _w.Line($"/* {node.Description}: {node.SourceText} */");
        }
    }
}