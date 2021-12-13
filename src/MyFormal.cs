using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


namespace Azurite
{
    public class MyFormal
    {


        private static Dictionary<string, string> parametersType = new Dictionary<string, string>();


        private static bool reType = false;
        /// <summary>
        /// Import the function parameters in the locals symbol table.
        /// </summary>
        /// <param name="expression">The function to import</param>
        public static void SetContextFunc(Parser.SExpression expression)
        {
            Lexer.locals = new List<Lexer.Symbol>();
            List<Parser.SExpression> childs = expression.LoadAllChild();
            if (childs.Count != 4)
                throw new Azurite.Ezception(201, "expression is not a function");

            List<string> type = Lexer.GetSymbol(Lexer.globals, childs[1].data).type;
            List<string> parameters = childs[2].LoadAllData();

            for (int i = 0; i < parameters.Count; i++)
            {
                Lexer.add_to_locals(new Lexer.Symbol(parameters[i], new List<string>() { type[i] }));
            }


        }

        /// <summary>
        /// Clear the current context
        /// </summary>
        public static void ClearContext()
        {
            Lexer.locals = new List<Lexer.Symbol>();
        }
        public static Lexer.Symbol GetFunctionType(Parser.SExpression expression)
        {
            parametersType = new Dictionary<string, string>();
            List<Parser.SExpression> childs = expression.LoadAllChild();

            foreach (string param in childs[2].LoadAllData())
            {
                parametersType.Add(param, "");
            }

            string output = "";
            do
            {
                reType = false;
                output = GetType(childs[3], "", childs[1].data);
                reType = reType && new List<string>(parametersType.Values).TrueForAll(elmt => elmt == "" || Formal.is_polymorphic(elmt));
            } while (reType);

            List<string> type = new List<string>(parametersType.Values);

            type.Add(output);
            Lexer.Symbol symbol = new Lexer.Symbol(childs[1].data, type);
            Lexer.add_to_globals(symbol);

            parametersType = new Dictionary<string, string>();
            return symbol;

        }

        public static bool IsAny(string type)
        {
            return type.Contains("any") || type == "";
        }

        public static bool IsAnyEquivalent(string anyMatcher, string to_match)
        {
            if (!IsAny(anyMatcher))
                return false;

            return ListLevelType(to_match) >= ListLevelType(anyMatcher);
        }

        public static int ListLevelType(string type)
        {
            Regex reg = new Regex(@"(\.\.\.)+");
            return reg.Match(type).Length / 3;
        }

        public static bool IsEquivalent(string type, string polymorphe)
        {
            // empty type mean any
            if (type == "")
                return true;
            if (polymorphe.Contains("any"))
                return ListLevelType(type) >= ListLevelType(polymorphe);

            if (!Formal.is_polymorphic(polymorphe))
            {
                if (type == polymorphe)
                    return true;

                return false;
            }

            if (ListLevelType(polymorphe) == 0)
                return true;
            if (ListLevelType(polymorphe) == ListLevelType(type))
                return true;

            return false;
        }
        private static List<string> ReplacePolymorphe(List<string> input, string type, string match)
        {
            while (ListLevelType(match) > 0)
            {
                type = type.Substring(0, type.Length - 3);
                match = match.Substring(0, match.Length - 3);
            }
            return input.Select(x => x.Replace(match, type)).ToList();
        }


        private static string GetTypeWithData(string data)
        {
            if (data.StartsWith("\"") && data.EndsWith("\""))
                return "str";
            if (double.TryParse(data, out _))
                return "num";
            if (Lexer.globals.Exists(x => x.symbol == data))
                return Lexer.globals.Find(x => x.symbol == data).type[0];
            if (Lexer.ContainSymbol(Lexer.locals, data))
                return Lexer.GetSymbol(Lexer.locals, data).type[0];

            throw new Azurite.Ezception(201, $"Unexpected token {data}");
        }

        private static string GetTypeFromAst(Parser.SExpression expression, string mustReturn, string fname)
        {
            List<Parser.SExpression> childs = expression.LoadAllChild();
            List<string> expressionType = new List<string>();
            Dictionary<string, KeyValuePair<int, Func<string, string>>> typedParam = new Dictionary<string, KeyValuePair<int, Func<string, string>>>();

            expressionType = Lexer.globals.Find(x => x.symbol == childs[0].data).type;


            if (mustReturn != "" && mustReturn != "any" && !Formal.is_polymorphic(mustReturn) && mustReturn != expressionType[expressionType.Count - 1])
            {
                string toreturn = expressionType[expressionType.Count - 1];
                if (Formal.is_polymorphic(toreturn))
                    expressionType = expressionType.Select(x => x.Replace(toreturn, mustReturn)).ToList();
                else if (toreturn != "any")
                    throw new Azurite.Ezception(202, $"Type error {expression.Stringify()} is {toreturn} but should be {mustReturn}");
            }


            if (childs.Count > expressionType.Count)
            {
                Parser.SExpression sum = Parser.SExpression.fromList(childs.Skip(expressionType.Count - 1).Take(childs.Count - (expressionType.Count - 1)).ToList());
                childs = childs.Take(expressionType.Count - 1).ToList();
                childs.Add(sum);
            }

            for (int i = 1; i < childs.Count; i++)
            {
                if(expressionType[i] == "")
                    continue;

                string type = "";

                if (childs[i].has_data && parametersType.ContainsKey(childs[i].data))
                {
                    if (parametersType[childs[i].data] == "" || Formal.is_polymorphic(parametersType[childs[i].data]))
                    {
                        typedParam.Add(childs[i].data, new KeyValuePair<int, Func<string, string>>(i - 1, (expr) => expr));
                        type = expressionType[i];
                    }
                    else
                    {
                        type = parametersType[childs[i].data];
                    }
                }
                else
                {

                    type = GetType(childs[i], expressionType[i], fname);
                    if (Formal.is_polymorphic(expressionType[i]) && type != expressionType[i])
                    {

                        expressionType = MyFormal.ReplacePolymorphe(expressionType, type, expressionType[i]);
                    } else if (type != expressionType[i] && !Formal.is_polymorphic(type) && expressionType[i] != "any" ){
                        if (i < childs.Count - 1)
                            throw new Azurite.Ezception(202, $"Type error {childs[i].Stringify()} is {type} but should be {expressionType[i]}");
                        type += "...";
                    }

                }

                if (type != expressionType[i])
                {

                    if (!Formal.is_polymorphic(expressionType[i]) && !IsEquivalent(type, expressionType[i]))
                        throw new Azurite.Ezception(201, "goubliélexception");


                    for (int j = 1; j < i; j++)
                    {
                        if (expressionType[j - 1] == expressionType[i])
                            GetType(childs[j], type, fname);
                    }
                    expressionType = ReplacePolymorphe(expressionType, type, expressionType[i]);
                }
            }



            foreach (var element in typedParam)
            {
                string value = element.Value.Value(expressionType[element.Value.Key]);
                if (parametersType[element.Key] == "" || Formal.is_polymorphic(parametersType[element.Key]))
                    parametersType[element.Key] = value;
                else if (Formal.is_polymorphic(value))
                    expressionType = ReplacePolymorphe(expressionType, parametersType[element.Key], value);
                else if (parametersType[element.Key] != value)
                    throw new Azurite.Ezception(204, "Parameter type error");
            }

            return expressionType[expressionType.Count - 1];
        }

        private static string GetTypeFromList(Parser.SExpression expression, string mustReturn, string fname)
        {
            List<string> type = new List<string>();
            List<Parser.SExpression> childs = expression.LoadAllChild();

            string listType = (mustReturn != "") ? mustReturn.Substring(0, mustReturn.Length - 3) : "";

            for (int i = 0; i < childs.Count; i++)
            {
                string newType = "";
                if (childs[i].has_data && parametersType.ContainsKey(childs[i].data))
                {
                    if (parametersType[childs[i].data] == "" || Formal.is_polymorphic(parametersType[childs[i].data]))
                    {
                        parametersType[childs[i].data] = listType;
                        reType = true;

                        type.Add(listType);
                    }
                    newType = parametersType[childs[i].data];
                    // else
                    // {
                    //     if (listType == "")
                    //         listType = parametersType[childs[i].data];
                    //     type.Add(parametersType[childs[i].data]);
                    // }
                }
                else
                {
                    if (IsAny(mustReturn))
                    {
                        try
                        {
                            newType = GetType(childs[i], listType, fname);
                        }
                        catch (Azurite.Ezception e)
                        {
                          //  if (e.code == 204)
                            //    return "any";
                            throw e;
                        }
                    }
                    else
                    {
                        newType = GetType(childs[i], listType, fname);

                    }
                }

                if (IsAny(listType))
                    listType = newType;

                if (newType != listType)
                {
                    if (Formal.is_polymorphic(listType))
                    {
                        return GetTypeFromList(expression, newType + "...", fname);
                    }

                    throw new Azurite.Ezception(203, "Arity error the array is not of the same type");

                }

            }

            return listType + "...";
        }


        /// <summary>
        /// Return the type of an S-expression
        /// </summary>
        /// <param name="expression">The expression to type</param>
        /// <param name="mustReturn">The type that the S-expression need to be (leave blank)</param>
        /// <param name="fname">The name of the function to type (leave blank)</param>
        /// <returns>The type of the S-expression</returns>
        public static string GetType(Parser.SExpression expression, string mustReturn = "", string fname = "")
        {
            List<Parser.SExpression> childs = expression.LoadAllChild();
            string type = "";

            if (expression.has_data)
            {
                if (Lexer.ContainSymbol(Lexer.locals, expression.data))
                    type = Lexer.GetSymbol(Lexer.locals, expression.data).type[0];

                else if (parametersType.ContainsKey(expression.data))
                {
                    if (parametersType[expression.data] == "" || Formal.is_polymorphic(parametersType[expression.data]))
                    {
                        parametersType[expression.data] = mustReturn;
                    }
                    type = parametersType[expression.data];
                }
                else
                    type = GetTypeWithData(expression.data);
            }

            else if (childs[0].data == fname)
            {
                for (int i = 1; i < childs.Count; i++)
                    GetType(childs[i], parametersType.ElementAt(i - 1).Value, fname);

                type = mustReturn;
            }

            else if (Lexer.globals.Exists(x => x.symbol == childs[0].data))
            {
                type = GetTypeFromAst(expression, mustReturn, fname);
            } else if(Lexer.locals.Exists(x => x.symbol == childs[0].data)){
                type = Lexer.GetSymbol(Lexer.locals, childs[0].data).type[0];
            }
            else
            {
                type = GetTypeFromList(expression, mustReturn, fname);
            }
            if (type == "" || type == "any")
            {
                return mustReturn;
            }
            if (!IsAnyEquivalent(mustReturn, type) && type != mustReturn)
            {
                if (Formal.is_polymorphic(mustReturn) && IsEquivalent(type, mustReturn))
                    return Formal.is_polymorphic(type) ? mustReturn : type;
                // if (type + "..." == mustReturn)
                // return type;
                throw new Azurite.Ezception(204, $"Type error {expression.Stringify()} is {type} but should be {mustReturn}");
            }
            return type;
        }

        /// <summary>
        /// return the type of any S-expression, you don't have to filter wich method to use.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="mustReturn"></param>
        /// <returns></returns>
        public static List<string> GetStupidType(Parser.SExpression expression, string mustReturn = "any")
        {
            if (expression.has_data)
                return new List<string>() { GetTypeWithData(expression.data) };
            switch (expression.first().data)
            {
                case "defun":
                    return GetFunctionType(expression).type;
                case "import":
                case "translate":
                    return new List<string>() { "toplevel" };
                default:
                    return new List<string>() { GetType(expression, mustReturn) };
            }
        }

    }

}