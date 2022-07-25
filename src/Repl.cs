using System;
using System.Collections.Generic;

namespace Azurite
{
    class REPL
    {

        public static string get_extension(string lang)
        {
            lang = lang.ToLower().Trim();
            if (lang == "c++" || lang == "cpp" || lang == "cplusplus" || lang == "cxx") return "cpp";
            if (lang == "python" || lang == "python3" || lang == "py" || lang == "py3") return "py";
            if (lang == "caml" || lang == "ocaml" || lang == "ml") return "ml";
            if (lang == "js" || lang == "javascript") return "js";
            if (lang == "cs" || lang == "csharp" || lang == "c#") return "cs";
            if (lang == "azurite" || lang == "azur") return "azur";
            return "error";
        }



        public string get_lang_name(string lang_extension)
        {
            lang_extension = get_extension(lang_extension);
            switch (lang_extension)
            {
                case "cpp":
                    return "C++";
                case "py":
                    return "Python 3";
                case "cs":
                    return "C#";
                case "ml":
                    return "OCaml";
                case "js":
                    return "Javascript";
                case "azur":
                    return "Azurite";
                default:
                    throw new NotImplementedException($"get_lang_name {lang_extension}");
            }
        }

        public void print_head(string lang_str)
        {
            Console.WriteLine($"Azurite REPL started, with language {get_lang_name(get_extension(lang_str))}.");
        }

        bool debug_mode()
        {
            return false;
        }

        void writeRed(string to_write, string end = "\n")
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(to_write + end);
            Console.ResetColor();
        }

        void writeGreen(string to_write, string end = "\n")
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(to_write + end);
            Console.ResetColor();
        }

        void writeYellow(string to_write, string end = "\n")
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(to_write + end);
            Console.ResetColor();
        }

        public static bool show_type = true;
        public static bool show_raw = true;
        public static bool show_beginning_ast = true;
        public static bool show_final = true;
        public static bool show_final_ast = true;


        void WriteOutput(Parser.SExpression ast, string language_name = "azurite")
        {
            foreach (Azurite.Expression expr in Azurite.expressions_list)
                WriteAST(expr, language_name);

            Azurite.expressions_list.Clear();
        }
        void WriteAST(Azurite.Expression expr, string language_name = "Azurite")
        {

            Parser.SExpression ast = expr.arbre;
            if (show_beginning_ast)
            {
                writeYellow("Tree of expression :");
                ast.PrettyPrint();
            }

            if (show_raw)
            {
                writeYellow($"Expression after macros :\n\t{Azurite.MacroApply(ast).Stringify()}");
            }

            if (show_type)
            {
                List<string> temp = new List<string>();
                if (!ast.has_data && ast.first().has_data && ast.first().data == Langconfig.function_name)
                {
                    temp = FormalReborn.GetType(ast);

                }
                else
                {
                    temp = FormalReborn.GetType(ast);
                }
                if (temp.Count == 0 || (temp.Count == 1 && FormalReborn.is_polymorphic(temp[0]) && FormalReborn.is_variadic(temp[0])))
                {
                    writeRed($"Type : {Tools.get_pretty_type(temp)}");
                }
                else
                {
                    writeGreen($"Type : {Tools.get_pretty_type(temp)}");
                }
            }

            if (show_final)
            {
                if (language_name == "Azurite")
                {
                    var tmp = Compiler.Compile(ast);
                    writeGreen($"Final expression : {tmp.Stringify()}");
                    if (show_final_ast)
                    {
                        writeYellow($"Tree of Final Expression :");
                        tmp.PrettyPrint();
                    }
                }
                else
                {
                    writeGreen($"Final expression : {Azurite.TranslateExpression(ast, language_name)}");
                }
            }
        }

        //Laisse l'output transpilé dans stdout
        public REPL(string language_name)
        {
            print_head(language_name);
            language_name = language_name.ToLower().Trim();
            if (language_name == "azurite" || language_name == "azur")
            {
                do
                {
                    string s;
                    var ast = loop_iter(out s).Clone();
                    ast = MacroManager.Execute(ast);
                    if (ast.has_data && ast.data.ToLower().Trim() == "quit")
                    {
                        return;
                    }
                    if (!debug_mode())
                    {
                        try
                        {
                            if (!ast.has_data && ast.first().has_data && ast.first().data == Langconfig.function_name)
                            {
                                //Console.WriteLine($"Tree : ");
                                //ast.PrettyPrint();
                                //Console.WriteLine(Tools.get_pretty_type(Formal.type_of_func(ast)));
                                WriteOutput(ast);
                            }
                            else
                            {
                                Azurite.Load(new Parser.SExpression(ast), ast.Stringify());
                                // if (!ast.has_data)
                                //     FormalReborn.GetType(ast);
                                WriteOutput(ast);
                                /*Console.WriteLine($"Tree : ");
                                ast.PrettyPrint();
                                Console.WriteLine(Tools.get_pretty_type(Formal.type_of(ast)));*/
                            }
                        }
                        catch (Exception e)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(e.Message);
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }
                    else
                    {
                        if (!ast.has_data && ast.first().has_data && ast.first().data == Langconfig.function_name)
                        {
                            /*Console.WriteLine($"Tree : ");
                            ast.PrettyPrint();
                            Console.WriteLine(Tools.get_pretty_type(Formal.type_of_func(ast)));*/
                            WriteOutput(ast);
                        }

                        else
                        {

                            Azurite.Load(new Parser.SExpression(ast), ast.Stringify());
                            WriteOutput(ast);
                            /*Console.WriteLine($"Tree : ");
                            ast.PrettyPrint();
                            Console.WriteLine(Tools.get_pretty_type(Formal.type_of(ast)));*/
                        }
                    }
                } while (true);
            }
            else
            {
                do
                {
                    string s;
                    var ast = loop_iter(out s).Clone();
                    ast = MacroManager.Execute(ast);
                    if (ast.has_data && ast.data.ToLower().Trim() == "quit")
                    {
                        return;
                    }
                    if (!debug_mode())
                    {
                        try
                        {
                            if (!ast.has_data && ast.first().has_data && ast.first().data == Langconfig.function_name)
                            {
                                /*Console.WriteLine($"Tree : ");
                                ast.PrettyPrint();
                                Console.WriteLine(Tools.get_pretty_type(Formal.type_of_func(ast)));*/
                                WriteOutput(ast);
                            }
                            else if (!ast.has_data && ast.first().has_data && (ast.first().data == Langconfig.macro_name
                                                                          || ast.first().data == Langconfig.translate_name
                                                                          || ast.first().data == Langconfig.import_name))
                            {
                                Azurite.Process(new Azurite.Expression(new Parser.SExpression(ast), ast.Stringify()));
                            }
                            else
                            {
                                /*Console.WriteLine($"Tree : ");
                                ast.PrettyPrint();
                                Console.WriteLine(Tools.get_pretty_type(Formal.type_of(ast)));*/
                                WriteOutput(ast, "ocaml");
                            }
                        }
                        catch (Exception e)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(e.Message);
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }
                    else
                    {
                        if (!ast.has_data && ast.first().has_data && ast.first().data == Langconfig.function_name)
                        {
                            //Console.WriteLine(Tools.get_pretty_type(Formal.type_of_func(ast)));
                            WriteOutput(ast);
                        }
                        else if (!ast.has_data && ast.first().has_data && (ast.first().data == Langconfig.macro_name
                                                                      || ast.first().data == Langconfig.translate_name
                                                                      || ast.first().data == Langconfig.import_name))
                        {
                            Azurite.Process(new Azurite.Expression(new Parser.SExpression(ast), ast.Stringify()));
                        }
                        else
                        {
                            if (!ast.has_data)
                                FormalReborn.GetType(ast);
                            // Formal.descendent_verification(ast, "#1");
                            /*Console.WriteLine($"Tree : ");
                            ast.PrettyPrint();
                            Console.WriteLine($"Type : {Tools.get_pretty_type(Formal.type_of(ast))}");*/
                            WriteOutput(ast);

                        }
                    }
                    //Console.WriteLine(Azurite.TranslateExpression(ast, language_name) + "\n");         ///apply read_and_export
                } while (true);
            }
        }

        //Écrit l'output transpilé dans un fichier
        public REPL(string language_name, string output)
        {
            print_head(language_name);
            language_name = language_name.ToLower().Trim();
            if (language_name == "azurite" || language_name == "azur")
            {
                do
                {
                    string s;
                    var ast = loop_iter(out s).Clone();
                    ast = MacroManager.Execute(ast);
                    if (ast.has_data && ast.data.ToLower().Trim() == "quit")
                    {
                        return;
                    }
                    if (!debug_mode())
                    {
                        try
                        {
                            if (!ast.has_data && ast.first().has_data && ast.first().data == Langconfig.function_name)
                            {
                                //Console.WriteLine(Tools.get_pretty_type(Formal.type_of_func(ast)));
                                WriteOutput(ast, language_name);
                                System.IO.File.WriteAllText(output + "." + get_extension(language_name), s + "\n");
                            }
                            else
                            {
                                WriteOutput(ast, language_name);
                                /*Console.WriteLine($"Tree : ");
                                ast.PrettyPrint();
                                Console.WriteLine($"Type : {Tools.get_pretty_type(Formal.type_of(ast))}");*/
                            }
                        }
                        catch (Exception e)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(e.Message);
                            Console.ForegroundColor = ConsoleColor.White;
                        }
                    }
                    else
                    {
                        if (!ast.has_data && ast.first().has_data && ast.first().data == Langconfig.function_name)
                        {
                            /*Console.WriteLine($"Tree : ");
                            ast.PrettyPrint();
                            Console.WriteLine(Tools.get_pretty_type(Formal.type_of_func(ast)));*/
                            WriteOutput(ast, language_name);
                            System.IO.File.WriteAllText(output + "." + get_extension(language_name), s + "\n");
                        }
                        else
                        {
                            WriteOutput(ast, language_name);
                            /*Console.WriteLine($"Tree : ");
                            ast.PrettyPrint();
                            Console.WriteLine(Tools.get_pretty_type(Formal.type_of(ast)));*/
                        }
                    }

                } while (true);
            }
            else
            {
                do
                {
                    string s;
                    var ast = loop_iter(out s).Clone();
                    if (ast.has_data && ast.data.ToLower().Trim() == "quit")
                    {
                        return;
                    }
                    System.IO.File.WriteAllText(output + "." + get_extension(language_name), Azurite.TranslateExpression(ast, language_name) + "\n");         ///apply read_and_export
                } while (true);
            }
        }
        public Parser.SExpression loop_iter(out string cumul)
        {
            cumul = "";
            bool has_passed = false;
            do
            {
                Console.Write(has_passed ? "... " : ">>> ");
                string text = Console.ReadLine();
                if (text == "q")
                {
                    cumul = "";
                    has_passed = false;
                    continue;
                }
                cumul += text;
                try
                {
                    cumul = cumul.Trim();
                    var r = new Parser.SExpression(cumul);
                    return r;
                }
                catch (System.Exception)
                {
                    has_passed = true;
                    continue;
                }
                // has_passed = true;
            } while (true);
            //throw new Exception("WTF!!!!!!!!!!!!!!!!!!!!!!!!");
        }

        public string loop_iter_to_s()
        {
            bool has_passed = false;
            do
            {
                Console.Write(has_passed ? "... " : ">>> ");
                string cumul = "";
                cumul += Console.ReadLine();
                try
                {
                    //var r = new Parser.SExpression(cumul);
                    return cumul;
                }
                catch (System.Exception)
                {
                    has_passed = true;
                    continue;
                }
                // has_passed = true;
            } while (true);
            //throw new Exception("WTF!!!!!!!!!!!!!!!!!!!!!!!!");
        }

        public REPL()
        {
            new REPL("Azurite");
        }
    }
}