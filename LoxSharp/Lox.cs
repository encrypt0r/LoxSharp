﻿using System;
using System.IO;

namespace LoxSharp
{
    class Lox
    {
        static bool _hadError = false;
        static bool _hadRuntimeError = false;

        static readonly Interpreter _interpreter = new Interpreter();

        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                Console.WriteLine("Usage: loxsharp [script]");
            }
            else if (args.Length == 1)
            {
                RunFile(args[0]);
            }
            else
            {
                RunPrompt();
            }
        }

        public static void RuntimeError(RuntimeException ex)
        {
            Console.WriteLine($"{ex.Message}\n[line {ex.Token.Line:N0}]");
            _hadRuntimeError = true;
        }

        static void RunFile(string path)
        {
            var code = File.ReadAllText(path);
            Run(code);

            if (_hadError)
                Environment.Exit(65);
            if (_hadRuntimeError)
                Environment.Exit(70);
        }

        static void RunPrompt()
        {
            while(true)
            {
                Console.Write("> ");
                Run(Console.ReadLine());

                _hadError = false;
            }
        }

        static void Run(string source)
        {
            Scanner scanner = new Scanner(source);
            var tokens = scanner.ScanTokens();

            Parser parser = new Parser(tokens);
            Expr expression = parser.Parse();

            // Stop if there was a syntax error.
            if (_hadError) return;

            _interpreter.Interpret(expression);
        }

        public static void Error(int line, string message)
        {
            Report(line, "", message);
        }

        static void Report(int line, string where, string message)
        {
            Console.WriteLine($"[Line {line:N0}] Error{where}: {message}");
            _hadError = true;
        }

        public static void Error(Token token, string message)
        {
            if (token.Type == TokenType.EOF)
                Report(token.Line, " at end", message);
            else
                Report(token.Line, $" at '{token.Lexeme}'", message);
        }
    }
}