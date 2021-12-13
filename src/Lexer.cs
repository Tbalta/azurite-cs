using System.Collections.Generic;
using System.Xml.Serialization;
using System;

namespace Azurite
{

    using UniqueSymbolTable = List<Lexer.Symbol>;

    public class Lexer
    {

        public class Symbol
        {
            //private string _symbol;
            public string symbol
            {
                get;
                private set;
            }
            public List<string> type;
            //private bool is_keyword;
            public bool is_keyword
            {
                get;
                private set;
            }
            public Symbol(string _symbol)
            {
                symbol = _symbol;
                is_keyword = true;
            }
            public Symbol(string _symbol, List<string> _type)
            {
                symbol = _symbol;
                type = _type;
                is_keyword = false;
            }
            public Symbol(string _symbol, uint number_of_parameters = 0)
            {
                type = new List<string>();
                symbol = _symbol;
                for (/*uint y = polymorphic_level*/; number_of_parameters + 1 > 0; number_of_parameters--)
                {
                    type.Add("#" + number_of_parameters.ToString());
                }
                polymorphic_level += (number_of_parameters + 1);
                is_keyword = false;
            }
            public static string get_new_polymorphic()
            {
                polymorphic_level++;
                return $"#{polymorphic_level - 1}";
            }
            public static uint polymorphic_level = 0;
        }

        /// <summary>
        /// Search the Symbol with the data as keyword.
        /// </summary>
        /// <param name="table">The table to search in</param>
        /// <param name="data">The keyword to search</param>
        /// <returns>The corresponding symbol</returns>
        public static Symbol GetSymbol(UniqueSymbolTable table, string data)
        {
            return table.Find(x => x.symbol == data);
        }

        /// <summary>
        /// Return true if the table contains the specified symbol
        /// </summary>
        /// <param name="table">The table to search in</param>
        /// <param name="data">The keyword of the symbol</param>
        public static bool ContainSymbol(UniqueSymbolTable table, string data)
        {
            return table.Exists(x => x.symbol == data);
        }

        public static bool IsToken(string data)
        {
            return ContainSymbol(builtins, data) || ContainSymbol(locals, data) || ContainSymbol(globals, data);
        }
        public static UniqueSymbolTable builtins;
        public static UniqueSymbolTable globals;
        public static UniqueSymbolTable locals;

        public static void init_builtins()
        {
            builtins = new UniqueSymbolTable();
            globals = new UniqueSymbolTable();
            locals = new UniqueSymbolTable();
            builtins.Add(new Symbol(Langconfig.function_name));
            //builtins.Add(new Symbol(Langconfig.macro_name));
            //builtins.Add(new Symbol("match"));
            //builtins.Add(new Symbol(Langconfig.function_name));

            // builtins.Add(new Symbol("if", new List<string>(){"bool", "#1", "#1", "#1"}));

            // builtins.Add(new Symbol("+", new List<string>(){"num...", "num"}));
            // builtins.Add(new Symbol("-", new List<string>(){"num...", "num"}));
            // builtins.Add(new Symbol("*", new List<string>(){"num...", "num"}));
            // builtins.Add(new Symbol("/", new List<string>(){"num...", "num"}));
            // builtins.Add(new Symbol("mod", new List<string>(){"num", "num", "num"}));

            // builtins.Add(new Symbol("=", new List<string>(){"#1", "#1", "bool"}));
            // builtins.Add(new Symbol(">", new List<string>(){"#1...", "bool"}));
            // builtins.Add(new Symbol("<=", new List<string>(){"#1...", "bool"}));
            // builtins.Add(new Symbol("or", new List<string>(){"bool...", "bool"}));
            // builtins.Add(new Symbol("not", new List<string>(){"bool", "bool"}));

            // builtins.Add(new Symbol("NtoB", new List<string>(){"num", "bool"}));

            // builtins.Add(new Symbol("index", new List<string>(){"num", "#1...", "#1"}));

            // builtins.Add(new Symbol("cat", new List<string>(){"str...", "str"}));
            // builtins.Add(new Symbol("NtoS", new List<string>(){"num", "str"}));
            // builtins.Add(new Symbol("cons", new List<string>(){"#1", "#1...", "#1..."}));
            // builtins.Add(new Symbol("empty", new List<string>(){"#1..."}));
        }
        public static void clear_locals()
        {
            locals.Clear();
        }
        public static void add_to_globals(Symbol to_add)
        {
            globals.Add(to_add);
        }
        public static void add_to_locals(Symbol to_add)
        {
            locals.Add(to_add);
        }
        public static bool check_name(ref string _rtoken)
        {
            string token = _rtoken;
            float a = 0;
            if (float.TryParse(token, out a))
            {
                int b = 0;
                if (int.TryParse(token, out b))
                {
                    _rtoken += ".0";
                }
            }
            return builtins.Exists(x => x.symbol == token) || globals.Exists(x => x.symbol == token) ||
            locals.Exists(x => x.symbol == token) || float.TryParse(token, out a) || (token[0] == '"' && token[token.Length - 1] == '"' || token == "true" || token == "false");
        }
        public static bool check_SExpression(Parser.SExpression to_check)
        {
            if (to_check.has_data)
            {
                string data = to_check.data;
                bool b = check_name(ref data);
                to_check.data = data;
                return b;
            }
            else
            {
                return check_SExpression(to_check.first()) && check_SExpression(to_check.second());
            }
        }

    }

}