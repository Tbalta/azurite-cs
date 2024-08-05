using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Azurite
{
    public class Parser
    {
        public class QuickException : Exception
        {
            public QuickException(string info) : base(info) { }
        }

        public class SExpression
        {
            public List<SExpression> childs = new List<SExpression>();
            public string data { get; set; }
            public bool has_data { get; set; }
            public bool is_end { get; set; }
            public int line { get; set; }
            public int column { get; set; }

            public static List<string> remove_parenthesis(List<string> data)
            {
                return (data[0] == "(" && data[data.Count - 1] == ")") ? data.GetRange(1, data.Count - 2) : data;
            }
            public static List<string> remove_first_and_last(List<string> data)
            {
                return data.GetRange(1, data.Count - 2);
            }

            public static List<string> trim(List<string> data)
            {
                List<string> to_return = new List<string>();
                foreach (var item in data)
                {
                    if (item != "")
                        to_return.Add(item.Trim());
                }
                return to_return;
            }

            private static List<string> parseString(string param)
            {
                param = param.Trim();
                List<string> array = tokenize(param);

                int parenthesis = 0;
                for (int i = 0; i < array.Count; i++)
                {
                    if (array[i] == "(")
                        parenthesis++;
                    else if (array[i] == ")")
                        parenthesis--;
                }
                if (parenthesis < 0)
                    throw new Azurite.Ezception(101, " missing '(' at " + param);
                if (parenthesis > 0)
                    throw new Azurite.Ezception(101, " missing ')' at " + param);
                array = trim(array);
                return array;
            }

            public static List<string> tokenize(string data)
            {
                List<string> to_return = new List<string>();
                to_return.Add("");
                bool current_between_parenthesis = false;
                bool isEscaped = false;
                // bool isstring = false;
                foreach (char c in data)
                {

                    if (isEscaped || (current_between_parenthesis && c != '"') || (c != '\n' && c != '\r' && c != ' ' && c != '(' && c != ')' && c != '"'))
                    {
                        to_return[to_return.Count - 1] += c;
                    }
                    else if (c == ' ' || c == '(' || c == ')')
                    {
                        if (c != ' ')
                            to_return.Add(char.ToString(c));
                        else
                            to_return.Add("");
                        to_return.Add("");
                    }
                    else if ((c == '"') && !isEscaped)
                    {
                        to_return[to_return.Count - 1] += c;
                        current_between_parenthesis = !current_between_parenthesis;
                    }
                    if (c == '\\')
                    {
                        isEscaped = true;
                    }
                    else if (isEscaped)
                    {
                        isEscaped = false;
                    }
                    else if (isEscaped)
                    {
                        // isstring = c != '"';
                    }
                }
                return to_return;
            }

            public static int find_matching_parenthesis(List<string> data, int start)
            {
                int pos = 0;
                uint count = 1;
                foreach (var element in data)
                {
                    if (pos > start)
                    {
                        if (element == "(")
                        {
                            count++;
                        }
                        else if (element == ")")
                        {
                            count--;
                        }
                    }
                    if (count == 0)
                    {
                        return pos;
                    }
                    pos++;
                }
                return -1;
            }
            public static string add_spaces(string data)
            {
                string to_return = "";
                bool p = false;
                bool b = false;
                foreach (var c in data)
                {
                    if (!p && (c == '(' || c == ')'))
                    {
                        if (b)
                            to_return += c;
                        else
                            to_return += $" {c} ";
                    }
                    else if (c == '"')
                    {
                        p = !p;
                        to_return += c;
                        b = false;
                    }
                    else
                    {
                        to_return += c;
                    }
                }
                return to_return;
            }

            public SExpression()
            {
                data = "NULL";
                has_data = true;
                is_end = true;
            }

            public SExpression(string data, List<SExpression> childs)
            {
                this.childs = childs;
                this.data = data;
                this.has_data = data != null;
            }

            public SExpression(List<string> array)
            {

                if (array.Count == 1)
                {
                    this.data = array[0];
                    has_data = true;
                    return;
                }
                
                // Skip the first parenthesis
                array = remove_parenthesis(array);
                int start = 0;

                while (start < array.Count)
                {
                    if (array[start] == "")
                    {
                        start++;
                        continue;
                    }
                    if (array[start] == "(")
                    {
                        int pos = find_matching_parenthesis(array, start);
                        childs.Add(new SExpression(array.GetRange(start, pos - start + 1)));
                        start = pos + 1;
                    }
                    else
                    {
                        childs.Add(new SExpression(array[start]));
                        start++;
                    }
                }
            }

            public SExpression(string param) : this(parseString(param))
            {
            }

            public SExpression(SExpression sExpression)
            {
                //Take an SExpression as entries copy the data
                data = sExpression.data;
                has_data = sExpression.has_data;
                is_end = sExpression.is_end;
                childs = sExpression.childs.ConvertAll(x => x.Clone());
            }

            public SExpression(double value)
            {
                this.data = value.ToString();
                this.has_data = true;
                this.is_end = true;
            }
            public SExpression Clone()
            {
                return new SExpression(this);
            }

            public SExpression(List<SExpression> liste)
            {
                this.childs = liste;
                this.has_data = false;
                this.is_end = false;
            }

            public void PrettyPrint(string indent = "")
            {

                bool last = childs.Count == 0;
                Console.WriteLine(indent + "+- " + data);
                indent += last ? "   " : "|  ";
                childs.ForEach(c => c.PrettyPrint(indent));
            }

            public bool Equal(Parser.SExpression test_expression)
            {
                if (test_expression == null)
                    return false;
                bool is_equal = this.has_data == test_expression.has_data &&
                                this.is_end == test_expression.is_end &&
                                this.data == test_expression.data &&
                                this.childs.Count == test_expression.childs.Count;
                return is_equal && this.childs.Zip(test_expression.childs, (a, b) => a.Equal(b)).All(x => x);
            }

            public List<string> LoadAllData()
            {

                return LoadAllChild().ConvertAll(x => x.data);
            }


            public string Stringify()
            {
                if (this.has_data)
                    return data;
                return "(" + this.ExpressionToString() + ")";
            }
            private string ExpressionToString()
            {
                List<SExpression> childs = this.LoadAllChild();
                if (this.has_data)
                    return this.data;
                string to_return = "";
                foreach (SExpression child in childs)
                {
                    if (child.has_data)
                    {
                        to_return += child.data.Trim() + " ";
                    }
                    else
                    {
                        to_return += "(" + child.ExpressionToString() + ") ";
                    }
                }
                return to_return.TrimEnd();
            }

            /// <summary>
            /// Load all sub S-expression contains in the current S-expression.
            /// </summary>
            /// <returns>Return a list of S-expression</returns>
            ///<example>
            /// Admitting this S-expression: ((+ 2 3) (foo)) this method will return (+ 2 3),(foo) inside a list.
            /// </example>
            public List<SExpression> LoadAllChild()
            {
                return this.childs;
            }

            private void ImportExpression(SExpression expression)
            {
                this.childs = expression.childs;
                this.data = expression.data;
                this.has_data = expression.has_data;
                this.is_end = expression.is_end;
            }

            /// <summary>
            /// Apply a function to every node of the S-expression
            /// </summary>
            /// <param name="function">The fonction to apply</param>
            public void Map(Func<SExpression, SExpression> function)
            {
                SExpression newExpression = function(this);
                this.childs.ForEach(x => x.Map(function));
                ImportExpression(newExpression);
            }


            public void MapData(Func<SExpression, string> function)
            {
                this.childs.ForEach(x => x.MapData(function));
                this.data = function(this);
            }

            public static SExpression fromList(List<Parser.SExpression> liste)
            {
                return new SExpression(liste);
            }

            /// <summary>
            /// Return true if the token contained in the S-expression is a callable.
            /// </summary>
            /// <returns>Return true if the token contained in the S-expression is a callable.</returns>
            public bool isCallable()
            {
                return this.has_data && Directive.known_token.Contains(this.data);
            }

            /// <summary>
            /// Return true if the S-expression respect the match level of a proto.
            /// </summary>
            /// <param name="proto">The proto to match</param>
            public bool matchProto(KeyValuePair<string, KeyValuePair<Directive.MATCH_LEVEL, string>> proto)
            {
                if (proto.Value.Key == Directive.MATCH_LEVEL.LIGHT || proto.Value.Key == Directive.MATCH_LEVEL.LIST)
                    return true;
                if (proto.Value.Key == Directive.MATCH_LEVEL.STRICT)
                    return this.has_data;
                if (proto.Value.Key == Directive.MATCH_LEVEL.EXACT)
                    return this.data == proto.Key;
                if (proto.Value.Key == Directive.MATCH_LEVEL.PARTIAL)
                    return Directive.CheckPartialMatch(proto.Key, this.data);
                if (proto.Value.Key == Directive.MATCH_LEVEL.CALLABLE)
                    return this.isCallable();
                return false;
            }
        }

    }
    // Move the ShowHelp() method outside of the namespace
    class MainClass
    {
        static void ShowHelp()
        {
            Console.WriteLine("Usage: azurite [file] [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("  -t, --target <language>  Specify the target language");
            Console.WriteLine("  -c, --config <file>      Load a configuration file");
            Console.WriteLine("  -l, --list               List all available languages");
            Console.WriteLine("  -s, --save <file>        Save the output to a file");
            Console.WriteLine("  -d, --DEBUG              Enable debug mode");
            Console.WriteLine("  -h, --help               Show this help message");
            Console.WriteLine("  --stdlib                 Specify the standard library path");
            Console.WriteLine("  -g, --debugger           Enable the debugger");
        }
        public const int MAX_RECURSION_ALLOWED = 100;
        public static void Main(string[] args)
        {

            if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
            {
                ShowHelp();
                return;
            }

            // first switch index
            int i = 0;
            for (; i < args.Length; i++)
            {
                if (args[i].StartsWith("-"))
                    break;
            }

            List<string> inputFiles = args.ToList().GetRange(0, i);
            List<string> options = args.ToList().GetRange(i, args.Length - i);

            Lexer.init_builtins();

            // string filePath = "";
            // ParameterManagers.OnInput = input => { if (!Langconfig.is_loaded) Langconfig.load(); Azurite.Load(input); Azurite.Compile(); };
            List<string> languages = new List<string>();
            string outputFile = "";

            ParameterManagers.registerCommand(
                new ParameterManagers.Command("-t", langs => languages = langs,
                    new List<string>() { "--target" },
                    true
                    ));




            ParameterManagers.registerCommand(
                new ParameterManagers.Command("--stdlib",
                    output => Azurite.stdlib = output[0],
                    new List<string>() { "--config" },
                    false,
                    () => { Azurite.stdlib = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.azurite"; }
                ));

            ParameterManagers.registerCommand(
                new ParameterManagers.Command("-l", output =>
                {
                    Console.Write(Azurite.LanguageHandler.LogLanguage());
                },
                    new List<string>() { "--list" },
                    true
                    ));

            ParameterManagers.registerCommand(
                new ParameterManagers.Command("-o", output =>
                {
                    outputFile = output[0];
                },
                new List<string>() { "--save" },
                true
                ));

            ParameterManagers.registerCommand(
                new ParameterManagers.Command("-d",
                                              output => Azurite.DEBUG = true,
                                              new List<string>() { "--DEBUG" },
                                              false));

            ParameterManagers.registerCommand(
                new ParameterManagers.Command("-g", output => Azurite.debugger = true,
                                              new List<string>() { "--debugger" },
                                              false));

            ParameterManagers.Execute(options.ToArray());
            if (!Langconfig.is_loaded)
                Langconfig.load();


            if (Azurite.debugger)
            {
                var debugger = Debugger.create("main");
                debugger.Breakpoint();
            }

            foreach (string file in inputFiles)
            {
                Azurite.Load(file);
                Azurite.Compile();
            }

            foreach (string lang in languages)
            {
                Azurite.Export(outputFile + "." + lang, lang);
                Console.WriteLine($"file saved as {outputFile + "." + lang}");
            }


            Azurite.DisplayError();
        }
    }
}