﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace LoxSharp
{
    public class Interpreter : IExprVisitor<object>, IStmtVisitor<object>
    {
        public enum ExecuteResult
        {
            Continue,
            Break,
        }

        public Interpreter()
        {
            _environment = Globals = new Environment();

            Globals.Define("clock",
                new NativeFunction("clock", (_, __) => new TimeSpan(DateTime.Now.Ticks).TotalSeconds));
            Globals.Define("print",
                new NativeFunction("print", (_, a) =>
                {
                    Console.WriteLine(a.OfType<string>().FirstOrDefault());
                    return null;
                }, 1));
        }

        private static object _undefined = new object();
        public Environment Globals { get; private set; }
        private Environment _environment;
        private readonly object _breakInterrupt = new object();

        public void Interpret(List<Stmt> statements)
        {
            try
            {
                foreach (var stmt in statements)
                {
                    Execute(stmt);
                }
            }
            catch (RuntimeException ex)
            {
                Lox.RuntimeError(ex);
            }
        }

        private ExecuteResult Execute(Stmt stmt)
        {
            return stmt.Accept(this) == _breakInterrupt ? ExecuteResult.Break : ExecuteResult.Continue;
        }

        private string Stringify(object value)
        {
            if (value is null) return "nil";

            if (value is double)
            {
                var text = value.ToString();

                // if the number was an integer, print it without a decimal point
                if (text.EndsWith(".0"))
                {
                    text = text.Substring(0, text.Length - 2);
                }

                return text;
            }

            return value.ToString();
        }

        public object VisitBinaryExpr(Expr.Binary expr)
        {
            var left = Evaluate(expr.Left);
            var right = Evaluate(expr.Right);

            switch (expr.Operator.Type)
            {
                case TokenType.Greater:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left > (double)right;

                case TokenType.GreaterEqual:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left >= (double)right;

                case TokenType.Less:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left < (double)right;

                case TokenType.LessEqual:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left <= (double)right;

                case TokenType.Minus:
                    CheckNumberOperands(expr.Operator, left, right);
                    return (double)left - (double)right;

                case TokenType.Slash:
                    CheckNumberOperands(expr.Operator, left, right);

                    var denominator = (double)right;
                    CheckDevideByZero(expr.Operator, denominator);

                    return (double)left / denominator;

                case TokenType.Star:
                    CheckNumberOperands(expr.Operator, left, right);

                    return (double)left * (double)right;

                case TokenType.Percent:
                    CheckNumberOperands(expr.Operator, left, right);

                    denominator = (double)right;
                    CheckDevideByZero(expr.Operator, denominator);

                    return (double)left % denominator;

                case TokenType.EqualEqual:
                    return IsEqual(left, right);

                case TokenType.BangEqual:
                    return !IsEqual(left, right);

                case TokenType.Plus:
                    if (left is string t1 && right is string t2)
                    {
                        return t1 + t2;
                    }
                    else if (left is string || right is string)
                    {
                        return Stringify(left) + Stringify(right);
                    }
                    else if (left is double n1 && right is double n2)
                    {
                        return n1 + n2;
                    }

                    throw new RuntimeException(expr.Operator, "Operands must be two numbers or two strings.");
            }

            // Unreachable
            return null;
        }

        private void CheckDevideByZero(Token @operator, double denominator)
        {
            if (denominator == 0)
            {
                throw new RuntimeException(@operator, "Can't devide by zero.");
            }
        }

        private void CheckNumberOperands(Token @operator, object left, object right)
        {
            if (left is double && right is double) return;

            throw new RuntimeException(@operator, "Operands must be numbers.");
        }

        private bool IsEqual(object left, object right)
        {
            // nil is only equal to nil
            if (left == null && right == null) return true;
            if (left == null) return false;

            return left.Equals(right);
        }

        public object VisitGroupingExpr(Expr.Grouping expr)
        {
            return Evaluate(expr.Expression);
        }

        public object VisitLiteralExpr(Expr.Literal expr)
        {
            return expr.Value;
        }

        public object VisitUnaryExpr(Expr.Unary expr)
        {
            var right = Evaluate(expr.Right);

            switch (expr.Operator.Type)
            {
                case TokenType.Minus:
                    CheckNumberOperand(expr.Operator, right);
                    return -(double)right;
                case TokenType.Bang:
                    return !IsTruthy(right);
            }

            return null;
        }

        private void CheckNumberOperand(Token @operator, object operand)
        {
            if (operand is double) return;

            throw new RuntimeException(@operator, "Operand must be a number.");
        }

        private bool IsTruthy(object value)
        {
            // TODO: 0 is is also falsy?
            if (value == null) return false;
            if (value is bool b) return b;

            return true;
        }

        private object Evaluate(Expr expression)
        {
            return expression.Accept(this);
        }

        public object VisitExpressionStmt(Stmt.Expression stmt)
        {
            Evaluate(stmt.Expr);
            return null;
        }

        public object VisitPrintStmt(Stmt.Print stmt)
        {
            var value = Evaluate(stmt.Expr);
            Console.WriteLine(Stringify(value));
            return null;
        }

        public object VisitVariableExpr(Expr.Variable expr)
        {
            var value = _environment.Get(expr.Name);
            if (value == _undefined)
                throw new RuntimeException(expr.Name, $"Variable '{expr.Name.Lexeme}' has not been properly initialized.");

            return value;
        }

        public object VisitVarStmt(Stmt.Var stmt)
        {
            object value = _undefined;

            if (stmt.Initializer != null)
                value = Evaluate(stmt.Initializer);

            _environment.Define(stmt.Name.Lexeme, value);

            return null;
        }

        public object VisitAssignExpr(Expr.Assign expr)
        {
            var value = Evaluate(expr.Value);

            _environment.Assign(expr.Name, value);
            return value;
        }

        public object VisitBlockStmt(Stmt.Block stmt)
        {
            var result = ExecuteBlock(stmt.Statements, new Environment(_environment));

            return result == ExecuteResult.Break ? _breakInterrupt : null;
        }

        public ExecuteResult ExecuteBlock(IEnumerable<Stmt> statements, Environment environment)
        {
            var previous = _environment;

            try
            {
                _environment = environment;

                foreach (var statement in statements)
                {
                    if (Execute(statement) == ExecuteResult.Break)
                        return ExecuteResult.Break;
                }
            }
            finally
            {
                _environment = previous;
            }

            return ExecuteResult.Continue;
        }

        public object VisitIfStmt(Stmt.If stmt)
        {
            var condition = Evaluate(stmt.Condition);

            if (IsTruthy(condition))
            {
                if (Execute(stmt.ThenBranch) == ExecuteResult.Break)
                    return _breakInterrupt;
            }
            else if (stmt.ElseBranch != null)
            {
                if (Execute(stmt.ElseBranch) == ExecuteResult.Break)
                    return _breakInterrupt;
            }

            return null;
        }

        // var thing = this && that;
        // var thing = this || that;

        public object VisitLogicalExpr(Expr.Logical expr)
        {
            var left = Evaluate(expr.Left);

            if (expr.Operator.Type == TokenType.Or && IsTruthy(left))
            {
                return left;
            }
            else if (expr.Operator.Type == TokenType.And && !IsTruthy(left))
            {
                return left;
            }

            return Evaluate(expr.Right);
        }

        public object VisitWhileStmt(Stmt.While stmt)
        {
            while (IsTruthy(Evaluate(stmt.Condition)))
            {
                if (Execute(stmt.Body) == ExecuteResult.Break)
                    break;
            }

            return null;
        }

        public object VisitBreakStmt(Stmt.Break stmt)
        {
            return _breakInterrupt;
        }

        public object VisitCallExpr(Expr.Call expr)
        {
            var callee = Evaluate(expr.Callee);

            if (callee is ICallable function)
            {
                var arguments = expr.Arguments.Select(Evaluate).ToArray();

                if (arguments.Length != function.Arity)
                {
                    throw new RuntimeException(expr.Parenthesis,
                       $"Expected {function.Arity} arguments but got {arguments.Length}.");
                }

                return function.Call(this, arguments);
            }

            throw new RuntimeException(expr.Parenthesis, "Can only call functions and classes.");
        }

        public object VisitFunctionStmt(Stmt.Function stmt)
        {
            var function = new Function(stmt);
            _environment.Define(stmt.Name.Lexeme, function);

            return null;
        }

        public object VisitReturnStmt(Stmt.Return stmt)
        {
            object value = null;

            if (stmt.Value != null)
                value = Evaluate(stmt.Value);

            throw new Return(value);
        }
    }
}
