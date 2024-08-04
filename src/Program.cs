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
        public class pair<FirstType, SecondType>
        {
            protected FirstType _first;
            protected SecondType _second;
            public FirstType first()
            {
                return _first;
            }
            public void first(FirstType value)
            {
                _first = value;
            }
            public SecondType second()
            {
                return _second;
            }
            public void second(SecondType value)
            {
                _second = value;
            }
        }

        public class SExpression : pair<SExpression, SExpression>
        {

            public string data { get; set; }
            public bool has_data { get; set; }
            public bool is_end { get; set; }
            /*public static string modify_parenthesis(string to_modify){
				
			}*/

            public static string insert_nulls(string data)
            {

                Regex reg = new Regex(@"(?<!\\)\)(?=(?:[^""]*""[^""]*"")*[^""]*$)");
                return reg.Replace(data, " NULL )");
                // return data.Replace(")", " NULL )");
            }
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

            public static void print_list(List<string> data)
            {
                foreach (var item in data)
                {
                    Console.Write("'{0}' ", item);
                }
                Console.WriteLine("");
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

            public static List<string> sanitize_parenthesis(List<string> data)
            {
                var to_return = new List<string>(data);
                for (int i = 1; i < data.Count - 2; i++)
                {
                    if (data[i] == "(" && data[i + 1] == "(")
                    {
                        var y = find_matching_parenthesis(data, i + 1);
                        if (y == find_matching_parenthesis(data, i) - 1)
                        {
                            to_return.RemoveAt(i + 1);
                            to_return.RemoveAt(y);
                        }
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
                    } /*else if (c == '\\'){
                        if(b){
                            b = false;
                            to_return += c;
                        } else{
                            b = true;
                        }
                    }*/
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
            public SExpression(List<string> array, uint depth = 0)
            {
                if (array.Count == 1)
                {
                    this.data = array[0];
                    has_data = true;
                }
                else
                {
                    has_data = false;
                    array = trim(array);
                    var head = array[0].Trim();
                    if (head != "(" && head != ")")
                    {
                        _first = new SExpression(head, depth + 1);
                        if (array[1] == "NULL")
                        {
                            _second = new SExpression();
                        }
                        else
                            _second = new SExpression(array.GetRange(1, array.Count - 1), depth + 1);
                    }
                    else if (head == "(")
                    {
                        var pos = find_matching_parenthesis(array, 0);
                        _first = new SExpression(array.GetRange(1, pos - 1), depth + 1);
                        _second = new SExpression(array.GetRange(pos + 1, array.Count - pos - 1), depth + 1);
                    }
                    else
                    {
                        throw new Azurite.Ezception(101, " mismatched ')' found");
                    }
                }
            }
            public SExpression(string param, uint depth = 0)
            {
                try
                {
                    param = add_spaces(param);
                    List<string> array = new List<string>(tokenize(param)); //(param.Split(' '));
                    if (array[0] == "" || array[0] == " ")
                        array = remove_first_and_last(array);

                    array = remove_parenthesis(array);
                    array = trim(array);
                    array = sanitize_parenthesis(array);
                    int parenthesis = 0;
                    for (int i = 0; i < array.Count; i++)
                    {
                        if (array[i] == "(")
                            parenthesis++;
                        else if (array[i] == ")")
                            parenthesis--;
                        if (parenthesis < 0)
                            throw new Azurite.Ezception(101, " mismatched ')' found");
                    }
                    //array = trim(array);
                    if (array.Count == 1)
                    {
                        this.data = array[0];
                        has_data = true;
                    }
                    else
                    {
                        int count = 0;
                        has_data = false;
                        var head = array[0].Trim();
                        if (head != "(" && head != ")")
                        {
                            _first = new SExpression(head, depth + 1);
                            if (array[1] == "NULL")
                            {
                                _second = new SExpression();
                            }
                            else
                                _second = new SExpression(array.GetRange(1, array.Count - 1), depth + 1);
                        }
                        else if (head == "(")
                        {
                            var pos = find_matching_parenthesis(array, count);
                            _first = new SExpression(array.GetRange(1, pos - 1), depth + 1);
                            _second = new SExpression(array.GetRange(pos + 1, array.Count - pos - 1), depth + 1);
                        }
                        else
                        { //if(head == ")")
                            throw new Azurite.Ezception(101, " mismatched ')' found");
                        }
                        count++;
                    }
                }
                catch (Exception)
                {
                    throw new Azurite.Ezception(103, "Error while parsing " + param);
                }
            }


            public SExpression(string data, Parser.SExpression first, Parser.SExpression second)
            {
                this._first = first;
                this._second = second;
                this.data = data;
                this.has_data = data != null;
                this.is_end = (first == null && second == null);
            }

            public SExpression(List<string> array)
            {
                //Console.Write("CALL WITH LIST : ");
                //print_list(array);
                //Console.WriteLine("\n");
                if (array.Count == 1)
                {
                    this.data = array[0];
                    has_data = true;
                }
                else
                {
                    int count = 0;
                    has_data = false;
                    array = trim(array);
                    var head = array[0].Trim();
                    if (head != "(" && head != ")")
                    {
                        //Console.WriteLine("checkpoint {0}", array.Count-1);
                        _first = new SExpression(head);
                        if (array[1] == "NULL")
                        {
                            _second = new SExpression();
                        }
                        else
                            _second = new SExpression(array.GetRange(1, array.Count - 1));
                        //_second = new SExpression(array.GetRange(1, array.Count-1));
                    }
                    else if (head == "(")
                    {
                        var pos = find_matching_parenthesis(array, count);
                        _first = new SExpression(array.GetRange(1, pos - 1));
                        _second = new SExpression(array.GetRange(pos + 1, array.Count - pos - 1));
                    }
                    else
                    { //if(head == ")")
                        throw new Azurite.Ezception(101, " mismatched ')' found");
                    }
                    count++;
                }
            }
            public SExpression(string param)
            {



                try
                {
                    // param = add_spaces(param);
                    param = param.Trim();
                    param = insert_nulls(param);
                    List<string> array = new List<string>(tokenize(param)); //(param.Split(' '));


                    if (array[0] == "" || array[0] == " ")
                        array = remove_first_and_last(array);

                    array = remove_parenthesis(array);
                    array = sanitize_parenthesis(array);
                    array = trim(array);
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


                    if (array.Count == 1)
                    {
                        this.data = array[0];
                        has_data = true;
                    }
                    else
                    {
                        int count = 0;
                        has_data = false;
                        var head = array[0].Trim();
                        if (head != "(" && head != ")")
                        {
                            _first = new SExpression(head);
                            if (array[1] == "NULL")
                            {
                                _second = new SExpression();
                            }
                            else
                                _second = new SExpression(array.GetRange(1, array.Count - 1));
                        }
                        else if (head == "(")
                        {
                            var pos = find_matching_parenthesis(array, count);
                            if (pos == array.Count - 1)
                            {

                            }
                            else
                            {
                                _first = new SExpression(array.GetRange(1, pos - 1));
                                _second = new SExpression(array.GetRange(pos + 1, array.Count - pos - 1));
                            }
                        }
                        else
                        { //if(head == ")")
                            throw new Azurite.Ezception(101, " mismatched ')' found");
                        }
                        count++;
                    }
                }
                catch (Azurite.Ezception e)
                {
                    throw e;
                }
                catch (System.Exception)
                {
                    throw new Azurite.Ezception(100, "Error while parsing " + param);
                }
            }

            public SExpression(SExpression sExpression)
            {
                //Take an SExpression as entries copy the data
                this.data = sExpression.data;
                this.has_data = sExpression.has_data;
                this.is_end = sExpression.is_end;

                //Clone the first and right child
                if (sExpression._first != null)
                    this._first = sExpression._first.Clone();
                if (sExpression._second != null)
                    this._second = sExpression._second.Clone();
            }

            public SExpression(double value) : this(value.ToString(CultureInfo.InvariantCulture), null, null) { }
            public SExpression Clone()
            {
                return new SExpression(this);
            }

            public SExpression(List<SExpression> liste)
            {
                SExpression current = this;
                for (int i = 0; i < liste.Count; i++)
                {
                    current.data = null;
                    current._first = liste[i];
                    current._second = new Parser.SExpression();
                    current = current.second();
                }
            }

            public void print(uint level = 0)
            {
                if (this.has_data)
                {
                    for (uint i = 0; i < level; i++)
                    {
                        Console.Write("-");
                    }
                    Console.Write("{0}\n", data);
                    //Console.WriteLine();
                }
                else
                {
                    _first.print(level + 1);
                    _second.print(level + 1);
                }
            }

            //source: https://stackoverflow.com/questions/1649027/how-do-i-print-out-a-tree-structure
            public void PrettyPrint(string indent = "")
            {

                bool last = _first == null && _second == null;
                Console.WriteLine(indent + "+- " + data);
                indent += last ? "   " : "|  ";

                if (_first != null)
                    _first.PrettyPrint(indent);
                if (_second != null)
                    _second.PrettyPrint(indent);

            }
            public int getDeph()
            {
                if (this.data == "NULL")
                    return 0;

                //* Source: https://www.educative.io/edpresso/finding-the-maximum-depth-of-a-binary-tree
                int leftDepth = _first.getDeph();
                int rightDepth = _second.getDeph();

                // Get the larger depth and add 1 to it to
                // account for the root.
                if (leftDepth > rightDepth)
                    return (leftDepth + 1);
                else
                    return (rightDepth + 1);
            }

            public bool Equal(Parser.SExpression test_expression)
            {
                if (test_expression == null)
                    return false;
                bool is_equal = this.has_data == test_expression.has_data &&
                                this.is_end == test_expression.is_end &&
                                this.data == test_expression.data;
                if (is_equal == false)
                    return is_equal;
                if (this.first() != null && !this.first().Equal(test_expression.first()))
                    return false;
                if (this.second() != null && !this.second().Equal(test_expression.second()))
                    return false;
                return true;
            }
            // public static bool operator != (Parser.SExpression test, Parser.SExpression test_expression) => !(test == test_expression);

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

            public string OLD_Stringify(string start = "(", string end = ")", bool showNull = false)
            {
                // if (this.has_data && (showNull || this.data != "NULL"))
                //     return this.data;

                // return ((this.first() != null) ? (this.first().has_data ? "" : start) + this.first().Stringify(start, end, showNull) : "") +
                //         (this.second() != null ? this.second().Stringify(start, end, showNull) + (this.second().has_data ? end : ""): "");


                // if (this.first().has_data)
                return (this.has_data) ? (showNull || this.data != "NULL") ? this.data : "" : start +
                    ((this.first() != null) ? this.first().OLD_Stringify(start, end, showNull) : "") +
                    ((this.second() != null) ? this.second().OLD_Stringify(start, end, showNull) : "") +
                    end;
            }
            /// <summary>
            /// Load all sub S-expression contains in the current S-expression.
            /// </summary>
            /// <returns>Return a list of S-expression</returns>
            ///<example>
            /// Admitting this S-expression: ((+ 2 3) (foo)) this method will return (+ 2 3),(foo) inside a list.
            /// </example>
            public List<Parser.SExpression> LoadAllChild()
            {
                List<Parser.SExpression> parameters = new List<SExpression>();

                Parser.SExpression current = this;
                while (current != null && current.data != "NULL")
                {

                    parameters.Add(current.first() != null ? current.first() : current);
                    current = current.second();
                }

                return parameters;
            }

            public bool Match(Parser.SExpression pattern) =>
            this.data == pattern.data &&
            ((pattern.first() != null) && this.first() != null) ? this.first().Match(pattern.first()) : true &&
                ((pattern.second() != null) && this.second() != null) ? this.second().Match(pattern.second()) : true;

            private void ImportExpression(SExpression expression)
            {
                this._first = expression.first();
                this._second = expression.second();
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
                if (this._first != null)
                    this._first.Map(function);
                if (this._second != null)
                    this._second.Map(function);
                ImportExpression(newExpression);
            }


            public void MapData(Func<SExpression, string> function)
            {
                if (this._first != null)
                    this._first.MapData(function);
                if (this._second != null)
                    this._second.MapData(function);
                if (this.data != null)
                    this.data = function(this);
            }

            public static Parser.SExpression fromList(List<Parser.SExpression> liste)
            {
                SExpression expression = new Parser.SExpression();
                SExpression current = expression;

                for (int i = 0; i < liste.Count; i++)
                {
                    current.ImportExpression(new Parser.SExpression(null, liste[i], new Parser.SExpression()));
                    // current.data = null;
                    // current._first = liste[i];
                    // current._second = new Parser.SExpression();
                    current = current.second();
                }
                return expression;
            }

            public void Append(Parser.SExpression expression)
            {
                // Parser.SExpression temp = this;
                if (this._second != null)
                    this._second.Append(expression);
                else
                {
                    this._second = expression;
                    this.is_end = false;
                }

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

            public SExpression this[int i]
            {

                get { return i == 0 ? this : this.second()[i - 1]; }
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
            Console.WriteLine("  -r, --repl               Start the REPL");
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
                new ParameterManagers.Command("-r", output =>
                {
                    if (output.Count == 0)
                        new REPL();
                    else
                        new REPL(output[0]);
                },
                new List<string>() { "--repl", "--REPL", "-R" })
            );



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
                new ParameterManagers.Command("-r",
                                                output => new REPL(),
                                                new List<string>() { "--REPL" },
                                                true));
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