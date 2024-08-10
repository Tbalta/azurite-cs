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
            public int length { get; set; }

            public string file { get; set; }

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

                return array.Where(x => x != "").ToList();
            }

            public static List<string> tokenize(string data)
            {
                List<string> to_return = new List<string>
                {
                    ""
                };
                bool current_between_parenthesis = false;
                bool isEscaped = false;
                foreach (char c in data)
                {

                    if (isEscaped || (current_between_parenthesis && c != '"') || (c != '\n' && c != '\r' && c != ' ' && c != '(' && c != ')' && c != '"'))
                    {
                        to_return[to_return.Count - 1] += c;
                    }
                    else if (c == '"')
                    {
                        to_return[to_return.Count - 1] += c;
                        current_between_parenthesis = !current_between_parenthesis;
                    }
                    else
                    {
                        to_return.Add(char.ToString(c));
                        to_return.Add("");
                    }

                    if (c == '\\')
                    {
                        isEscaped = true;
                    }
                    else if (isEscaped)
                    {
                        isEscaped = false;
                    }
                }
                return to_return;
            }

            public static int find_matching_parenthesis(List<string> data, int start)
            {
                int pos = 0;
                uint count = 1;
                foreach (string element in data)
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

            public SExpression(List<string> array, int line = 0, int col = 0, string file = "")
            {
                Debug.Assert(array.Count > 0);
                this.file = file;
                if (array.Count == 1)
                {
                    this.data = array[0];
                    this.column = col;
                    this.line = line;
                    this.length = array[0].Length;
                    has_data = true;
                    return;
                }


                // Find the first parenthesis
                int start = 0;
                while (start < array.Count && array[start] != "(")
                {
                    if (array[start] == "\n")
                    {
                        line++;
                        col = 0;
                    }
                    else
                    {
                        col += array[start].Length;
                    }
                    start++;
                }
                this.line = line;
                this.column = col;

                int end = find_matching_parenthesis(array, start);
                if (end == -1)
                {
                    throw new QuickException("missing ')'");
                }
                array = array.GetRange(start + 1, end - (start + 1));
                start = 0; // restore to 0 after GetRange

                this.length = array.Sum(x => x.Length) + 2;
                col += 1; // take into account the (

                while (start < array.Count)
                {
                    Debug.Assert(array[start] != "");

                    // Skip whitespaces
                    if (array[start].All(x => char.IsWhiteSpace(x)))
                    {
                        if (array[start].Contains("\n"))
                        {
                            col = 0;
                            line += array[start].Count(x => x == '\n');
                        } else 
                        {
                            col += array[start].Length;
                        }
                        start++;
                        continue;
                    }

                    if (array[start] == "(")
                    {
                        int pos = find_matching_parenthesis(array, start);
                        childs.Add(new SExpression(array.GetRange(start, pos - start + 1), line, col, file));
                        start = pos + 1;
                    }
                    else
                    {
                        childs.Add(new SExpression(array[start], line, col, file));
                        start++;
                    }
                    col += childs.Last().length;
                }
            }

            public SExpression(string param, int line = 0, int col = 0, string filename = "") : this(parseString(param), line, col, filename)
            {
            }

            public SExpression(SExpression sExpression)
            {
                //Take an SExpression as entries copy the data
                data = sExpression.data;
                has_data = sExpression.has_data;
                is_end = sExpression.is_end;
                line = sExpression.line;
                column = sExpression.column;
                length = sExpression.length;
                file = sExpression.file;
                childs = sExpression.childs.ConvertAll(x => x.Clone());                
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
                this.line = childs[0].line;
                this.column = childs[0].column;
                this.length = childs.Sum(x => x.length);

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
                childs = expression.childs;
                data = expression.data;
                has_data = expression.has_data;
                is_end = expression.is_end;
                line = expression.line;
                column = expression.column;
                length = expression.length;
                file = expression.file;
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
            public bool matchProto(KeyValuePair<SExpression, KeyValuePair<Directive.MATCH_LEVEL, string>> proto)
            {
                if (proto.Value.Key == Directive.MATCH_LEVEL.LIGHT || proto.Value.Key == Directive.MATCH_LEVEL.LIST)
                    return true;
                if (proto.Value.Key == Directive.MATCH_LEVEL.STRICT)
                    return this.has_data;
                if (proto.Value.Key == Directive.MATCH_LEVEL.EXACT)
                    return this.data == proto.Key.data;
                if (proto.Value.Key == Directive.MATCH_LEVEL.PARTIAL)
                    return Directive.CheckPartialMatch(proto.Key.data, this.data);
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
            
            string outputFile = "";
            ParameterManagers.registerCommand(
                new ParameterManagers.Command("-t", langs => Azurite.target_languages = langs,
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
            ParameterManagers.registerCommand(
                new ParameterManagers.Command("-s", output => Debugger.no_menu = true,
                                              new List<string>() { "--server" },
                                                false));

            ParameterManagers.Execute(options.ToArray());
            if (!Langconfig.is_loaded)
                Langconfig.load();


            if (Azurite.debugger)
            {
                var debugger = Debugger.create(new Parser.SExpression("main", 0, 0, inputFiles.FirstOrDefault("main")));
                debugger.Breakpoint();
                debugger.stepIn |= debugger.step;
            }

            foreach (string file in inputFiles)
            {
                Azurite.main_file = file;
                Azurite.Load(file);
                Azurite.Compile();
            }

            Azurite.Export(outputFile);


            Azurite.DisplayError();
        }
    }
}