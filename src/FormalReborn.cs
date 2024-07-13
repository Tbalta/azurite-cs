using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Azurite
{
    static class FormalReborn
    {
        public static int ListLevelType(string type)
        {
            Regex reg = new Regex(@"(\.\.\.)+");
            return reg.Match(type).Length / 3;
        }
        private static List<string> ReplacePolymorphe(List<string> input, string polymorphe, string match)
        {
            if (ListLevelType(polymorphe) > ListLevelType(match))
                throw new ArgumentException($"{polymorphe} is not a subtype of {match}");

            while (ListLevelType(polymorphe) > 0)
            {
                polymorphe = polymorphe.Substring(0, polymorphe.Length - 3);
                match = match.Substring(0, match.Length - 3);
            }
            return input.Select(x => x.Replace(polymorphe, match)).ToList();
        }

        private static string GetPolymorpheValue(string type, Dictionary<string, string> polymorphic_values)
        {
            var type_name = type.Substring(0, type.Length - (ListLevelType(type) * 3));
            var rest = type.Substring(type_name.Length);
            if (polymorphic_values.ContainsKey(type_name))
                return polymorphic_values[type_name] + rest;
            return type;
        }

        public static bool is_polymorphic(string type)
        { // Un polymorphe commence avec '#'
            return type != "" && type[0] == '#';
        }

        public static bool is_variadic(string type)
        {  //Un nom de liste fini avec '...'
            return type.EndsWith("...");
        }

        public static bool compare_type(string reference, string to_test)
        {
            while (ListLevelType(to_test) > 0 && ListLevelType(reference) > 0)
            {
                to_test = unlist(to_test, 1);
                reference = unlist(reference, 1);
            }

            if (is_polymorphic(reference))
                return ListLevelType(reference) <= ListLevelType(to_test);
            if (reference == "any" || reference == "")
                return true;
            return reference == to_test;
        }

        internal static void SetContextFunc(Parser.SExpression expression)
        {
            var childs = expression.LoadAllChild();
            var parameters = childs[2].LoadAllData();
            var symbol = Lexer.GetSymbol(childs[1].data);
            Lexer.addTemporaryLayer(Lexer.localsLayer);
            if (symbol != null)
            {
                for (int i = 0; i < parameters.Count; i++)
                {
                    Lexer.add_to_locals(new Lexer.Symbol(parameters[i], new List<string>() { symbol.type[i] }));
                }
            }
            else
            {
                parameters.ForEach(x => Lexer.add_to_locals(new Lexer.Symbol(x, new List<string>() { "any" })));
                var dummy_type = parameters.ConvertAll(x => "any");
                dummy_type.Add("any");
                Lexer.add_to_locals(new Lexer.Symbol(childs[1].data, dummy_type));
                var return_type = GetType(childs[3]).Last();
                Lexer.remove_from_locals(childs[1].data);
                var type = parameters.ConvertAll(x => Lexer.GetSymbol(x).type.Last());
                type.Add(return_type);
                Lexer.add_to_globals(new Lexer.Symbol(childs[1].data, type));
            }
        }

        private static string GetTypeWithData(Parser.SExpression expr)
        {
            var ProtoList = Directive.find_matching_proto(expr);
            string data = expr.data;
   

            if (data.StartsWith("\"") && data.EndsWith("\""))
                return "str";
            if (double.TryParse(data, out _))
                return "num";
            if (Lexer.ContainSymbol(Lexer.localsLayer, data) || Lexer.ContainSymbol(Lexer.globalsLayer, data))
                return Lexer.GetSymbol(data).type.Last();
            Console.WriteLine($"[Warning]Unknown token: {data}");
            return "any";
            throw new Azurite.Ezception(201, $"Unknown token {data}");
        }

        internal static void ExitContextFunc()
        {
            Lexer.removeTemporaryLayer(Lexer.localsLayer);
        }

        public static bool IsEquivalent(string type, string polymorphe)
        {
            // empty type mean any
            if (type == "")
                return true;
            if (polymorphe.Contains("any"))
                return ListLevelType(type) >= ListLevelType(polymorphe);

            if (!is_polymorphic(polymorphe))
            {
                return type == polymorphe;
            }

            if (ListLevelType(polymorphe) == 0)
                return true;
            if (ListLevelType(polymorphe) == ListLevelType(type))
                return true;

            return false;
        }

        private static bool LastChanceCheck(string name, string type)
        {
            if (is_polymorphic(type))
                return false;
            var symbol = Lexer.GetSymbol(Lexer.localsLayer, name);
            if (symbol == null)
                return false;

            var symbol_type = symbol.type.Last();

            if (is_polymorphic(symbol_type) || symbol_type == "any" || symbol_type == "")
            {
                var new_type = symbol.type.SkipLast(1).Append(type).ToList();
                Lexer.add_to_layer(Lexer.localsLayer, new Lexer.Symbol(name, new_type));
                return true;
            }
            return false;
        }

        private static bool LastChanceCheck(Parser.SExpression expression, string type)
        {
            if (expression.has_data)
                return LastChanceCheck(expression.data, type);
            return LastChanceCheck(expression.LoadAllChild()[0].data, type);
        }
        public static string unlist(string type) => type.Substring(0, type.Length - 3 * (ListLevelType(type)));
        public static string unlist(string type, int level) => type.Substring(0, type.Length - 3 * level);


        private class Typeable
        {
            public string expected;
            public Parser.SExpression expression;
            public bool second_try;
            public string effective_type;

            public Typeable(Parser.SExpression expression, string expected)
            {
                this.expected = expected;
                this.expression = expression;
                this.second_try = false;
                this.effective_type = "";
            }

            public void Type()
            {
                this.effective_type = FormalReborn.GetType(this.expression, expected).Last();
            }

            internal void Type(string expected)
            {
                this.effective_type = FormalReborn.GetType(this.expression, expected).Last();
            }
        }

        public static List<string> GetTypeV2(Parser.SExpression expression, List<string> protoType)
        {

            var polymorphValue = new Dictionary<string, string>();
            var to_type = new Queue<Typeable>(expression.LoadAllChild().Zip(protoType, (ast, type) => new Typeable(ast, type)).ToList());

            while (to_type.Count() > 0)
            {
                var current = to_type.Dequeue();
                current.Type(GetPolymorpheValue(current.expected, polymorphValue));
                if (is_polymorphic(current.expected) && ListLevelType(current.expected) <= ListLevelType(current.effective_type))
                {
                    var polymorph_name = unlist(current.expected);
                    var polymorph_type = unlist(current.effective_type, ListLevelType(current.expected));
                    if (polymorphValue.ContainsKey(polymorph_name))
                    {
                        if (polymorphValue[polymorph_name] == "any")
                            polymorphValue[polymorph_name] = polymorph_type;
                    }
                    else
                        polymorphValue.Add(polymorph_name, polymorph_type);
                }
                if (current.expected == "" || current.expected == "any" || compare_type(GetPolymorpheValue(current.expected, polymorphValue), current.effective_type))
                    continue;
                if (LastChanceCheck(current.expression, GetPolymorpheValue(current.expected, polymorphValue)))
                {
                    to_type = new Queue<Typeable>(expression.LoadAllChild().Zip(protoType, (ast, type) => new Typeable(ast, type)).ToList());
                    continue;
                }
                if (current.second_try)
                    throw new Azurite.Ezception(201, $"Type mismatch in {current.expression.Stringify()} is {current.effective_type} but expected {current.expected}");
                current.second_try = true;
                to_type.Enqueue(current);
            }
            return protoType.ConvertAll(x => GetPolymorpheValue(x, polymorphValue)).ToList();
        }


        public static List<string> GetType(Parser.SExpression expression, string return_type = "")
        {
            if (expression.has_data)
                return new List<string>() { GetTypeWithData(expression) };
            var protoList = Directive.find_matching_proto(expression);
            if (protoList.Count == 0)
                return ToListType(expression.LoadAllChild().ConvertAll(l => GetType(l, "")).ConvertAll(l => l.Last()));
            var protoTypes = protoList.ConvertAll(x => x.Item1.Values.ToList().ConvertAll(x => x.Value).Append(x.Item2).ToList());
            List<string> errors = new List<string>();

            for (int index = 0; index < protoTypes.Count; index++)
            {
                if (is_polymorphic(protoTypes[index].Last()) && return_type != "" && return_type != "any")
                    protoTypes[index] = ReplacePolymorphe(protoTypes[index], protoTypes[index].Last(), return_type);
                Lexer.addTemporaryLayer(Lexer.localsLayer);
                List<string> type = null;
                try
                {
                    type = GetTypeV2(expression, protoTypes[index]);
                }
                catch (Azurite.Ezception e)
                {
                    errors.Add(e.Message);

                }
                if (type != null)
                {
                    Lexer.MergeTemporaryLayer(Lexer.localsLayer);
                    return type;
                }
                // errors.Add($"{expression.Stringify()}({protoList[index].Item2})");
                Lexer.removeTemporaryLayer(Lexer.localsLayer);
            }
            throw new Azurite.Ezception(201, string.Join("\n", errors));
        }

        private static List<string> ToListType(List<string> argumentType)
        {
            if (argumentType.Count == 0)
                return new List<string>() { "any..." };
            var first_item = argumentType[0];
            if (argumentType.TrueForAll(x => x == first_item))
                return new List<string>() { first_item + "..." };
            return new List<string>() { "any..." };
        }
    }
}