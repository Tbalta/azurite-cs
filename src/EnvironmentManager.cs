using System;
using System.Collections.Generic;

namespace Azurite
{

    public static class EnvironmentManager
    {
        private struct Function
        {
            public List<string> parameters;
            public Parser.SExpression body;

            public Function(List<string> parameters, Parser.SExpression body)
            {
                this.parameters = parameters;
                this.body = body;
            }
        }

        private static Dictionary<string, Parser.SExpression> variables = new Dictionary<string, Parser.SExpression>();
        private static Dictionary<string, Function> functionsLists = new Dictionary<string, Function>();

        private static Dictionary<string, Parser.SExpression> localVar = new Dictionary<string, Parser.SExpression>();
        public static void LoadFunc(Parser.SExpression expression)
        {
            List<Parser.SExpression> childs = expression.LoadAllChild();
            string name = childs[0].data;
            functionsLists.Add(name, new Function(childs[1].LoadAllData(), childs[2]));
        }

        public static Parser.SExpression ExecuteFunc(string funcName, Parser.SExpression expression)
        {
            if (!functionsLists.ContainsKey(funcName))
                return expression;
            Function function = functionsLists[funcName];
            List<Parser.SExpression> arguments = expression.LoadAllChild();

            if (arguments.Count != function.parameters.Count)
                throw new ArgumentException($"{funcName} expected {function.parameters.Count} arguments but found {arguments.Count}");

            Parser.SExpression body = function.body.Clone();
            for (int i = 0; i < arguments.Count; i++)
            {
                arguments[i] = Compiler.Compile(arguments[i]);
                body.Map(expr => (expr.data == function.parameters[i]) ? arguments[i] : expr);
            }

            return body;
        }

        public static void LoadVar(string name, Parser.SExpression var)
        {
            // Lexer.add_to_globals(new Lexer.Symbol(name, Formal(var)));
            variables.Add(name, Compiler.Compile(var));
        }

        public static bool IsVariable(string name)
        {
            return variables.ContainsKey(name);
        }
        public static Parser.SExpression GetVariable(string name)
        {
            return variables[name].Clone();
        }
    }
}