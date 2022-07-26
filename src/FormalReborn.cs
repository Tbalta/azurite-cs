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
            if (polymorphic_values.ContainsKey(type))
                return polymorphic_values[type] + rest;
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

        public static bool compare_type(string type1, string type2)
        {
            if (is_polymorphic(type1) || is_polymorphic(type2))
                return true;
            if (type1 == "any" || type2 == "any" || type1 == "")
                return true;
            return type1 == type2;
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

        private static string GetTypeWithData(string data)
        {
            if (data.StartsWith("\"") && data.EndsWith("\""))
                return "str";
            if (double.TryParse(data, out _))
                return "num";
            if (Lexer.ContainSymbol(Lexer.localsLayer, data) || Lexer.ContainSymbol(Lexer.globalsLayer, data))
                return Lexer.GetSymbol(data).type.Last();

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


        public static List<string> GetType(Parser.SExpression expression, string return_type = "")
        {
            if (expression.has_data)
                return new List<string>() { GetTypeWithData(expression.data) };
            var protoList = Directive.find_matching_proto(expression);
            var argumentType = expression.LoadAllChild().ConvertAll(l => GetType(l, "")).ConvertAll(l => l.Last());
            if (protoList.Count == 0)
                return ToListType(argumentType);
            var protoTypes = protoList.ConvertAll(x => Lexer.GetSymbol(x.ElementAt(0).Key).type);
            Lexer.addTemporaryLayer(Lexer.localsLayer);
            List<string> errors = new List<string>();

            for (int index = 0; index < protoTypes.Count; index++)
            {
                var protoType = protoTypes[index];
                var polymorphValue = new Dictionary<string, string>();
                if (is_polymorphic(protoType.Last()) && !is_polymorphic(return_type) && return_type != "")
                    protoType = ReplacePolymorphe(protoType, protoType.Last(), return_type);
                argumentType = expression.LoadAllChild().Zip(protoType, (x, y) => GetType(x, GetPolymorpheValue(y, polymorphValue)).Last()).ToList();
                var isMatch = true;
                for (int i = 0; i < argumentType.Count; i++)
                {
                    if (is_polymorphic(protoType[i]))
                    {
                        if (polymorphValue.ContainsKey(protoType[i]))
                        {
                            if (polymorphValue[protoType[i]] == "any")
                                polymorphValue[protoType[i]] = argumentType[i];
                        }
                        else
                            polymorphValue.Add(protoType[i], argumentType[i]);
                    }
                    if (protoType[i] == "" || protoType[i] == "any" || GetPolymorpheValue(protoType[i], polymorphValue) == argumentType[i])
                        continue;
                    if (LastChanceCheck(expression.LoadAllChild()[i], protoType[i]))
                    {
                        index--;
                        isMatch = false;
                        break;
                    }
                    errors.Add($"{expression.LoadAllChild()[i].Stringify()} is {argumentType[i]} but should be {protoType[i]}");
                    isMatch = false;
                    break;

                }
                if (isMatch)
                {
                    Lexer.MergeTemporaryLayer(Lexer.localsLayer);
                    return protoType.ConvertAll(x => GetPolymorpheValue(x, polymorphValue)).ToList();
                }
            }
            Lexer.removeTemporaryLayer(Lexer.localsLayer);
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