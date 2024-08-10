using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Azurite
{
    /// <summary>
    /// Main azurite file containing the method for loading file and converting.
    /// </summary>
    public static class Azurite
    {
        public static string main_file = "";
        public static string stdlib = "";

        public static bool debugger = false;

        public static List<string> target_languages = new List<string>();
        public static Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();

        public static string getRelativePath(string path)
        {
            var mainPath = Path.GetDirectoryName(main_file);
            return Path.GetRelativePath(mainPath, path);
        }

        /// <summary>
        /// Language currently imported inside Azurite.
        /// </summary>
        public static class LanguageHandler
        {

            private static Dictionary<string, int> lang = new Dictionary<string, int>();

            /// <summary>
            /// Add a language to Azurite.
            /// </summary>
            /// <param name="language">The language to add.</param>
            /// <returns>True if the language wasn't yet present.</returns>
            public static bool AddLanguage(string language)
            {
                if (!lang.ContainsKey(language))
                {
                    lang.Add(language, lang.Count);
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Get the unique id of the language depending of the position at which the language has been added.
            /// </summary>
            /// <param name="language">The language to add.</param>
            /// <returns>Return the id of the language if found otherwise -1.</returns>
            public static int getLanguageIndex(string language)
            {
                if (lang.ContainsKey(language))
                    return lang[language];
                throw new Azurite.Ezception(500, $"can't find language {language}. Language available:{String.Join(',', new List<string>(lang.Keys))}");
            }

            /// <summary>
            /// Get the list of languages separated with ','.
            /// </summary>
            /// <returns>Return the list of languages separated with ','.</returns>
            public static string LogLanguage()
            {
                return String.Join(',', new List<string>(lang.Keys));
            }
        }

        /// <summary> 
        /// An expression is just the parsed line associated with the value of the line
        /// </summary>
        public struct Expression
        {
            public Parser.SExpression arbre;
            public string line;
            public Expression(Parser.SExpression arbre, string line)
            {
                this.arbre = arbre;
                this.line = line;
            }
        }

        public class Ezception : Exception
        {
            public int index;
            public string text;
            public int code;
            new private Ezception InnerException;

            public Ezception get_InnerException()
            {
                return InnerException;
            }


            public Ezception(int code, string message, string text = "", int index = -1, Ezception inner = null) : base(message)
            {
                this.InnerException = inner;
                this.index = index;
                this.text = text;
                this.code = code;
            }

        }

        public static List<Expression> expressions_list = new List<Expression>();
        private static List<Ezception> errors_list = new List<Ezception>();

        public static List<string> debug_list = new List<string>();
        public static bool DEBUG = false;

        /// <summary>
        /// Reset Azurite.
        /// </summary>
        public static void Reset()
        {
            Directive.Reset();
            expressions_list.Clear();
        }

        static HashSet<string> loaded_files = new HashSet<string>();
        public static void Load(string path, string filename = "")
        {
            if (!File.Exists(path))
                throw new Ezception(010, $"Cannot find {path}");
            if (loaded_files.Contains(path))
                return;
            loaded_files.Add(path);
            string[] fileContent = File.ReadAllLines(path);
            Load(fileContent, path);
            Console.WriteLine("successfully loaded: " + path + " in the current Azurite runtime");

        }

        /// <summary> Load a file in the current azurite runtime
        /// <param name="path"> the path of the file to load </param>
        /// <param name="filename"> if specified the tag to add to every method and function of the file </param>
        /// </summary>
        public static void Load(string[] fileContent, string filename = "")
        {
            List<string> lines = new List<string>();

            for (int index = 0; index < fileContent.Length;)
            {
                string newLine = "";
                do
                {
                    string line = fileContent[index];
                    newLine += line;

                    index++;
                    lines.Add("");
                } while (Parser.SExpression.find_matching_parenthesis(
                        Parser.SExpression.trim(
                            Parser.SExpression.tokenize(newLine)), 0) == -1 && index < fileContent.Length);

                lines[lines.Count - 1] = newLine;
            }

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (line == "")
                    continue;


                try
                {
                    Load(new Parser.SExpression(line, i + 1, 0, filename), line);
                }
                catch (Ezception e)
                {
                    errors_list.Add(new Ezception(e.code, e.Message, (e.text == "") ? line : e.text, i, e.get_InnerException()));
                }
#if !DEBUG
                catch (Exception e)
                {
                    errors_list.Add(new Ezception(0, "unknown error please report the following lines:" + e.Message, line, i));
                    throw;
                }
#endif


            }
        }

        public static void AddExpressionToResult(Parser.SExpression expression)
        {

            foreach (var language in target_languages)
            {
                if (!result.ContainsKey(language))
                    result[language] = new List<string>();
                string expressionString = ExpressionToString(expression, language);
                if (expressionString != "")
                    result[language].Add(expressionString);
            }
        }

        /// <summary>
        /// Process an S-expression and add it if necessary in the current azurite context
        /// </summary>
        /// <param name="arbre">The S-expression to process</param>
        /// <param name="line">The S-expression as a str</param>
        public static void Load(Parser.SExpression arbre, string line = "")
        {
            Debugger debugger = Debugger.create(arbre);

            #if DEBUG
                debugger.variables.Add("func", "Load");
            #endif

            if (Azurite.debugger && debugger.ShouldBreak())
                debugger.Breakpoint();

            expressions_list = new List<Expression>();
            Expression expression = new Expression(MacroApply(arbre), line);
            if (!Process(expression))
            {
                AddExpressionToResult(expression.arbre);
            } else 
            {
                expressions_list.ForEach(expr => AddExpressionToResult(expr.arbre));
            }
            Debugger.remove();

        }
        /// <summary> Check if <paramref name="expression"/> start with an explicit token and do the appropriate action
        /// <param name="expression">The expression to check.</param>
        /// <param name="filename">If specified the tag to the expression if needed</param>
        /// <return> Return true if the expression the expression start with an explicit token otherwise false</return>
        /// </summary>
        public static bool Process(Expression expression, string filename = "")
        {
            // expression.arbre = MacroApply(expression.arbre);
            Parser.SExpression arbre = expression.arbre;
            List<Parser.SExpression> children = arbre.LoadAllChild();

            if (arbre.has_data && arbre.data == "NULL")
                return true;
            if (children.Count == 0)
                return false;

            if (children[0].data == Langconfig.macro_name)
            {
                MacroManager.LoadMacro(arbre, filename);
                return true;
            }

            if (children[0].data == Langconfig.variables)
            {
                if (Langconfig.compilation == "1")
                    EnvironmentManager.LoadVar(children[1].data, children[2]);
                return false;
            }

            if (children[0].data == Langconfig.translate_name)
            {
                Directive.LoadInstruction(arbre, filename);
                if (Azurite.DEBUG)
                    debug_list.Add(arbre.Stringify());
                return true;
            }

            if (children[0].data == Langconfig.function_name && children[0] != null)
            {
                Directive.known_token.Add(children[1].data);
                expressions_list.Add(expression);
                if (Langconfig.compilation == "1")
                    EnvironmentManager.LoadFunc(arbre);

                return true;
            }


            if (children[0].data == Langconfig.import_name)
            {
                List<string> data = arbre.LoadAllData();
                string path = data[1];
                path = path.Replace("\"", "").Trim();
                if (path[0] == '.')
                    Load(path + ".azur", (data.Count > 2) ? data[2] : "");
                else if (path[0] == '~' && path[1] == '/')
                    Load(stdlib + "/" + path.Substring(2) + ".azur", (data.Count > 2) ? data[2] : "");
                else
                    Load(stdlib + "/" + path + ".azur", (data.Count > 2) ? data[2] : "");
                return true;
            }

            if (!arbre.has_data)
            {
                bool quit = false;
                var nonAdded = new List<Parser.SExpression>();
                foreach (var expr in arbre.LoadAllChild())
                {
                    if (Process(new Expression(expr, expr.Stringify()), filename))
                        quit = true;
                    else
                        nonAdded.Add(expr);
                }
                if (quit)
                    nonAdded.ForEach(elt => expressions_list.Add(new Expression(elt, elt.Stringify())));
                return quit;
            }

            return false;
        }

        /// <summary> Apply macro in the specified expression
        /// <param name="expression">The expression to check in.</param>
        /// <return>The expression without macro inside.</return>
        /// </summary>
        public static Parser.SExpression MacroApply(Parser.SExpression expression)
        {
            expression = MacroManager.Execute(expression);
            return expression;
        }

        /// <summary> Translate the expression list in a specified language.
        /// <param name="language">The targeted language.</param>
        /// <return>A list containing the translated list.</return>
        /// </summary>
        public static string ExpressionToString(Parser.SExpression expression, string language)
        {
            Transpiler.ResetTracking();
            return Transpiler.Convert(expression, language);
        }


        public static void Export(string path)
        {
            foreach (var language in target_languages)
            {
                Export(path + "." + language, language);
                Console.WriteLine("successfully exported: " + path + "." + language);
            }
        }
        /// <summary>
        /// Save and translate the S-expression loaded in Azurite.cs
        /// </summary>
        /// <param name="path">The adress of the output file.</param>
        /// <param name="language">The language in wich the S-expression must be converted.</param>
        public static void Export(string path, string language)
        {
            List<string> imports = Directive.Imports_list;
            List<string> fileContent = result[language];
            fileContent.InsertRange(0, imports);


            if (Azurite.DEBUG)
                fileContent.InsertRange(0, debug_list);

            for (int i = 0; i < fileContent.Count; i++)
            {
                fileContent[i] = fileContent[i]
                    .Replace("\\n", "\n")
                    .Replace("\\t", "\t")
                    .Replace("\\\"", "\"")
                    .Replace("\\(", "(")
                    .Replace("\\)", ")")
                    .Replace("\\ ", " ")
                    .Replace("\\0", "")
                    .Replace("\\x20", " ");
            }
            Directive.Imports_list = new List<string>();

            File.WriteAllLines(path, fileContent);

        }


        /// <summary>
        /// Get the file extension for a language.
        /// </summary>
        /// <param name="language">The language to get the extension from.</param>
        /// <returns>The extension of the language.</returns>
        public static string GetFileExtension(string language)
        {
            return Transpiler.Convert(new Parser.SExpression($"({language})"), language);
        }

        internal static void Compile()
        {
            if (Langconfig.compilation == "0")
                return;


            for (int i = 0; i < expressions_list.Count; i++)
            {
                expressions_list[i] = new Expression(Compiler.Compile(expressions_list[i].arbre), expressions_list[i].line);
            }


        }
        private static void DisplayError(Ezception error)
        {
            if (error == null)
                return;
            TextWriter errorWriter = Console.Error;
            errorWriter.WriteLine($"{error.code} {error.Message} at line {error.index}: {error.text}");
            DisplayError(error.get_InnerException());

        }
        public static void DisplayError()
        {
            TextWriter errorWriter = Console.Error;
            foreach (Ezception error in errors_list)
            {
                DisplayError(error);
            }

        }

    }
}