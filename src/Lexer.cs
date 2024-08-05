using System.Collections.Generic;
using System.Xml.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Azurite
{

    using UniqueSymbolTable = List<Lexer.Symbol>;
    using LexerLayer = Stack<List<Lexer.Symbol>>;
    public class Lexer
    {
        public static LexerLayer localsLayer = new LexerLayer();
        public static LexerLayer globalsLayer = new LexerLayer();
        public class Symbol : IComparable<Symbol>
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

            public int CompareTo(object obj)
            {
                if (obj is Symbol)
                {
                    return symbol.CompareTo(((Symbol)obj).symbol);
                }
                else
                {
                    return symbol.CompareTo(obj.ToString());
                }
            }

            public int CompareTo([AllowNull] Symbol other)
            {
                if (other == null)
                {
                    return 1;
                }
                return symbol.CompareTo(other.symbol);
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
            return table.Find(x => x.symbol == data || Directive.CheckPartialMatch(x.symbol, data));
        }

        internal static void removeTemporaryLayer(LexerLayer layer)
        {
            layer.Pop();
        }

        public static Symbol GetSymbol(LexerLayer layer, string data)
        {
            foreach (var table in layer)
            {
                var symbol = GetSymbol(table, data);
                if (symbol != null)
                    return symbol;
            }
            return null;
        }

        internal static void remove_from_locals(string data)
        {
            var symbol = GetSymbol(localsLayer, data);
            while (symbol != null)
            {
                for (int i = 0; i < localsLayer.Count; i++)
                {
                    if (localsLayer.Peek().Remove(symbol))
                        break;
                }
                symbol = GetSymbol(localsLayer, data);
            }
        }

        public static Symbol GetSymbol(string data)
        {
            var symbol = GetSymbol(localsLayer, data);
            if (symbol == null)
                symbol = GetSymbol(globalsLayer, data);
            return symbol;
        }


        /// <summary>
        /// Return true if the table contains the specified symbol
        /// </summary>
        /// <param name="layer">The layer search in</param>
        /// <param name="data">The keyword of the symbol</param>
        public static bool ContainSymbol(LexerLayer layers, string data)
        {
            foreach (var table in layers)
            {
                if (GetSymbol(table, data) != null)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Return true if the table contains the specified symbol
        /// </summary>
        /// <param name="table">The table search in</param>
        /// <param name="data">The keyword of the symbol</param>
        public static bool ContainSymbol(UniqueSymbolTable table, string data)
        {
            return table.Find(x => x.symbol == data) != null;
        }

        public static bool IsToken(string data)
        {
            return ContainSymbol(builtins, data) || ContainSymbol(localsLayer, data) || ContainSymbol(globalsLayer, data);
        }
        public static UniqueSymbolTable builtins;

        /// <summary>
        /// Add a new symbol table in which the new symbol will be stored.
        /// </summary>
        internal static void addTemporaryLayer(LexerLayer linked_table)
        {
            linked_table.Push(new UniqueSymbolTable());
        }



        // public static UniqueSymbolTable globals;
        // public static UniqueSymbolTable locals;

        public static void init_builtins()
        {
            globalsLayer.Push(new UniqueSymbolTable());
            localsLayer.Push(new UniqueSymbolTable());
            builtins = new UniqueSymbolTable();
            builtins.Add(new Symbol(Langconfig.function_name));
        }

        public static void add_to_layer(LexerLayer layer, Symbol symbol)
        {
            if (layer.Count == 0)
                throw new Exception("Cannot add to an empty layer");
            layer.Peek().RemoveAll(x => x.symbol == symbol.symbol);
            layer.Peek().Add(symbol);
        }

        public static void add_to_globals(Symbol to_add)
        {
            add_to_layer(globalsLayer, to_add);
        }
        public static void add_to_locals(Symbol to_add)
        {
            if (localsLayer.Peek().Contains(to_add))
                localsLayer.Peek().Remove(to_add);
            localsLayer.Peek().Add(to_add);
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
            return builtins.Exists(x => x.symbol == token) || IsToken(token) || float.TryParse(token, out a) || (token[0] == '"' && token[token.Length - 1] == '"' || token == "true" || token == "false");
        }

        internal static void MergeTemporaryLayer(LexerLayer layer)
        {
            if (localsLayer.Count <= 1)
                return;
            var last = localsLayer.Pop();
            last.ForEach(x => add_to_layer(layer, x));
        }

    }

}