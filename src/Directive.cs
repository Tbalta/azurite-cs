using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

using Prototype = System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, System.Collections.Generic.KeyValuePair<Azurite.Directive.MATCH_LEVEL, string>>>;
namespace Azurite
{
    using Formal = FormalReborn;

    ///<summary>
    /// Directive is the class wich handle the translate.
    /// It can load a translate and convert an expression based on the translated loaded.
    ///</summary>
    public class Directive
    {
        /// <summary>
        /// For more information on MATCH_LEVEL read the documentation.
        /// </summary>
        public enum MATCH_LEVEL
        {
            /// <summary>The token must be the one specified </summary>
            EXACT,
            /// <summary> The part surrounded by || is optional.</summary>
            PARTIAL,
            /// <summary>The token must be present in form of keyword.</summary>
            STRICT,
            /// <summary>The token can be anything </summary> 
            LIGHT,
            /// <summary>The token must be a list.</summary>
            LIST,
            /// <summary>The token must be callable cf: function name.</summary>
            CALLABLE,
            /// <summary>There is no match level.</summary>
            NULL
        }

        private static string Tokenize(string token, Directive.MATCH_LEVEL level)
        {
            switch (level)
            {
                case MATCH_LEVEL.EXACT:
                    return "\"" + token + "\"";
                case MATCH_LEVEL.STRICT:
                    return "'" + token + "'";
                case MATCH_LEVEL.LIST:
                    return token + "...";
                case MATCH_LEVEL.CALLABLE:
                    return "[" + token + "]";
                default:
                    return token;
            }
        }

        /// <summary>An instruction represent the way to convert a specific set of keyword into another language </summary>
        public struct Instruction
        {
            /// <summary>the statement to add at the top of the page ex:using System; in C# </summary>
            public string import;

            /// <summary>The arguments associated with the match level ex:(x->LIST)</summary>
            public Prototype proto;

            /// <summary> the structure in the targeted language ex: Console.Write({x}) </summary>
            public string effect;

            /// <summary> The type of the instruction ex:"num" "num" "bool"</summary> 
            public Instruction(string import, Prototype proto, string effect)
            {
                this.proto = proto;
                this.effect = effect;
                this.import = import;
            }

            /// <summary> Equal check if the specified instruction is equal to the current
            /// <param name="instruction">The instruction to compare with</param>
            /// <return> true if the two intructions are equals else false </return>
            ///</summary>
            public bool Equal(Instruction instruction) =>
                this.import == instruction.import &&
                this.proto == instruction.proto &&
                this.effect == instruction.effect;
            public override string ToString()
            {
                string to_return = "(";
                foreach (KeyValuePair<string, KeyValuePair<MATCH_LEVEL, string>> entry in proto)
                {
                    to_return += Directive.Tokenize(entry.Key, entry.Value.Key) + " ";
                }
                return to_return.TrimEnd() + ")";
            }

        }

        public static List<Tuple<Prototype, string>> protoList = new List<Tuple<Prototype, string>>();
        public static List<List<Instruction>> instructions_list = new List<List<Instruction>>();
        public static List<string> imports_list = new List<string>();

        private static bool SexpressionMatch(Parser.SExpression expression, Prototype proto)
        {
            var childs = expression.LoadAllChild();
            if (childs.Count() != proto.Count())
                return false;

            var index = 0;
            foreach (var child in childs)
            {
                if (!child.matchProto(proto.ElementAt(index)))
                    return false;
                index++;
            }
            return true;
        }

        public static int protoScore(Prototype proto)
        {
            var proto_match_level = proto.Select(x => x.Value.Key);
            var precedence = new Dictionary<MATCH_LEVEL, int>() {
                {MATCH_LEVEL.EXACT, 5},
                {MATCH_LEVEL.PARTIAL, 4},
                {MATCH_LEVEL.STRICT, 3},
                {MATCH_LEVEL.LIGHT, 2},
                {MATCH_LEVEL.LIST, 1},
                {MATCH_LEVEL.CALLABLE, 0}
            };

            return proto_match_level.Sum(x => precedence[x]);
        }

        public static int comparePrototype(Prototype proto1, Prototype proto2)
        {
            return protoScore(proto1) - protoScore(proto2);
        }

        public static List<Tuple<Prototype, string>> find_matching_proto(Parser.SExpression expression)
        {
            var result = protoList.Where(x => SexpressionMatch(expression, x.Item1)).ToList();
            result.Sort((a, b) => comparePrototype(a.Item1, b.Item1));
            return result;
        }
        /// <summary>
        /// It's the list of the token wich are known callable.
        /// </summary>
        public static List<string> known_token = new List<string>();

        /// <summary>
        /// The list of import.
        /// </summary>
        public static List<string> Imports_list { get => imports_list; set => imports_list = value; }

        public static void Reset()
        {
            instructions_list = new List<List<Instruction>>();
            imports_list = new List<string>();
            known_token = new List<string>();
        }
        #region instruction_load

        /// <summary>
        /// Add an instruction in the list of instruction.
        /// </summary>
        /// <param name="language">The language of the instruction.</param>
        /// <param name="instruction">The instruction to add in.</param>
        private static void AddInstruction(string language, Instruction instruction)
        {
            if (Azurite.LanguageHandler.AddLanguage(language))
                instructions_list.Add(new List<Instruction>());
            instructions_list[Azurite.LanguageHandler.getLanguageIndex(language)].Add(instruction);
        }

        public static bool CheckPartialMatch(string instruction_name, string argument_name)
        {
            if (argument_name == null)
                return false;

            Regex match = new Regex(@"\|.+\|");
            Match extract = match.Match(instruction_name);

            string temp_instruc = instruction_name.Remove(extract.Index, extract.Length);

            if (temp_instruc.Length > argument_name.Length) return false;



            int nombre_char_end = instruction_name.Length - (extract.Index + extract.Length);
            string temp_argu = argument_name.Remove(extract.Index, (argument_name.Length - nombre_char_end - extract.Index));


            return (argument_name.Length - nombre_char_end - extract.Index != 0) && temp_argu == temp_instruc;

        }

        /// <summary> loop throught all translate and return the translate witch correspond to the argument list
        /// <param name="lang"> The language in wich the Instruction must be </param>
        /// <param name="arguments"> The list of arguments </param>
        /// <return> Return the corresponding instruction of an arguments if no found return Instruction with only null </return>
        /// </summary>
        private static Instruction Match(string lang, Parser.SExpression expression, string forced = null)
        {
            var arguments = expression.LoadAllChild();
            var lang_instruction = instructions_list[Azurite.LanguageHandler.getLanguageIndex(lang)];
            var result = lang_instruction.Where(x => x.proto.Count == arguments.Count && SexpressionMatch(expression, x.proto));
            if (result.Count() == 0)
                return new Instruction(null, null, null);
            return result.MaxBy(x => protoScore(x.proto));
        }

        /// <summary> Determine the Match level of an expression. </summary>
        /// <param name="expression"> The expression to evaluate. </param>
        /// <param name="filename"> if specified the tag to add to every Strong Match </param>
        /// <return> The list of parameters associated with their match Level (cf: documentation) </return>
        private static List<KeyValuePair<string, MATCH_LEVEL>> Evaluate(Parser.SExpression expression, string filename = "")
        {
            List<KeyValuePair<string, MATCH_LEVEL>> arguments = new List<KeyValuePair<string, MATCH_LEVEL>>();

            foreach (string arg in expression.LoadAllData())
            {
                if (arg is null)
                    throw new Azurite.Ezception(504, "translate argument is not an atome");
                MATCH_LEVEL type = MATCH_LEVEL.LIGHT;
                string argument = arg;
                if (new Regex(@"\|.+\|").IsMatch(arg))
                {
                    type = MATCH_LEVEL.PARTIAL;
                    argument = arg.Replace("\"", "");
                }
                else if (arg.Contains("\'"))
                {
                    type = MATCH_LEVEL.STRICT;
                    argument = arg.Replace("\'", "");
                }
                else if (arg.Contains("\""))
                {
                    type = MATCH_LEVEL.EXACT;
                    argument = ((filename == "") ? "" : filename + ".") + arg.Replace("\"", "");
                }
                else if (arg.Contains("..."))
                {
                    type = MATCH_LEVEL.LIST;
                    argument = arg.Replace("...", "");
                }
                else if (arg.Contains("[") && arg.Contains("]"))
                {
                    type = MATCH_LEVEL.CALLABLE;
                    argument = argument.Replace("[", "");
                    argument = argument.Replace("]", "");
                }

                arguments.Add(new KeyValuePair<string, MATCH_LEVEL>(argument, type));
            }

            return arguments;
        }

        /// <summary> return true if the translate is correct.
        /// <param name="expression"> The translate to check</param>
        /// <param name="filename"> if specified the tag to add to every Strong match. </param>
        /// </summary>
        private static bool EnsureTranslateIntegrity(Parser.SExpression expression)
        {
            List<Parser.SExpression> children = expression.LoadAllChild();
            if (children.Count < 3)
                return false;

            // skip "translate"
            return children.Skip(1).All(x => x.LoadAllData().TrueForAll(y => y != null));
        }
        /// <summary> Load Intruction Convert an SExpression into a Instruction and add it to the list of instruction
        /// <param name="expression"> the SExpression to load in.</param>
        /// <param name="filename"> if specified the tag to add to every Strong match. </param>
        /// </summary>
        public static void LoadInstruction(Parser.SExpression expression, string filename = "")
        {
            if (!EnsureTranslateIntegrity(expression))
                throw new Azurite.Ezception(504, "Translate integrity check failed");
            
            List<Parser.SExpression> children = expression.LoadAllChild();
            Debug.Assert(children.Count >= 3);

            // (translate[0] (args[1]) (type[2]))
            List<KeyValuePair<string, MATCH_LEVEL>> arguments = Evaluate(children[1], filename);

            List<string> type = children[1].LoadAllData();

            // Converting the arguments into the prototype
            Prototype proto = new Prototype();


            for (int i = 0; i < arguments.Count; i++)
            {
                string name = arguments[i].Key;
                MATCH_LEVEL level = arguments[i].Value;
                string arg_type = "";
                // Check if parameters is already present except for exact match, also check for partial match
                if (proto.Where(val => val.Value.Key != MATCH_LEVEL.EXACT).Any(val => val.Key == name || val.Key.Contains("|" + name+ "|") || name.Contains("|" + val.Key + "|")))
                    throw new Azurite.Ezception(505, "Two parameters have the same name");
                
                proto.Add(new KeyValuePair<string, KeyValuePair<MATCH_LEVEL, string>>(name, new KeyValuePair<MATCH_LEVEL, string>(level, arg_type)));
            }
            protoList.Add(new Tuple<Prototype, string>(proto, "any"));

            // Loading all the language definition.
            IEnumerable<Parser.SExpression> langs = children.Skip(3);

            foreach (Parser.SExpression lang in langs)
            {
                // Load the targeted language / the import / the effect
                List<string> elemt = lang.LoadAllData();

                if (elemt.Count != 3)
                    throw new Azurite.Ezception(502, $"Can't import translate, 3 arguments expected, {elemt.Count} founds", lang.Stringify());

                string cible = elemt[0];
                string import = "";
                if (elemt[1] != "")
                    import = elemt[1].Substring(1, elemt[1].Length - 2).Replace($"{{{Langconfig.libpath}}}", Azurite.stdlib); ;
                string effect = elemt[2].Substring(1, elemt[2].Length - 2);

                AddInstruction(cible, new Instruction(import, proto, effect));
            }
        }
        #endregion
        #region instruction_convert

        private static bool PrintPrototype(Prototype proto)
        {
            foreach (var item in proto)
            {
                Console.WriteLine(item.Key + " : " + item.Value.Key + " : " + item.Value.Value);
            }
            return true;
        }

        /// <summary> Search expression to evaluate in a string and convert the expression</summary>
        /// <return> Return True while they are expression to evaluate.</return>
        /// <param name="effect"> The string to search in.</param>
        /// <param name="language"> The language to convert in.</param>
        /// <param name="language"> The language to convert in.</param>
        private static bool Eval(ref string effect, Instruction instruction, Parser.SExpression p_expression, string language)
        {
            string text = GetEvalText(effect);

            List<Parser.SExpression> arguments = p_expression.LoadAllChild();
            if (text == "")
                return false;

            Debugger debugger = Debugger.stack.Peek();
            debugger.variables["effect"] = effect.Replace(text, $"\x1b[31m{text}\x1b[0m");
            
            List<string> argumentName = instruction.proto.ConvertAll(x => x.Key);

            for (int i = 0; i < argumentName.Count; i++)
            {
                debugger.variables.TryAdd("$"+argumentName[i], arguments[i].Stringify());
            }

            if (Azurite.debugger && debugger.ShouldBreak())
            {
                debugger.Breakpoint();
            }
            
            Parser.SExpression expression;
            try
            {
                expression = new Parser.SExpression(text);
            }
            catch (Azurite.Ezception e)
            {
                debugger.Breakpoint();            
                throw new Azurite.Ezception(501, $"Unable to parse the expression inside <eval {text}>" , text, -1, e);
            }
            #if !DEBUG
            catch (Exception)
            {
                throw;
            }
            #endif

            for (int i = 0; i < argumentName.Count; i++)
            {
                if (effect.Contains($"[{argumentName[i]}]"))
                    expression.Map((Parser.SExpression expr) => (expr.data == $"[{argumentName[i]}]") ? arguments[i] : expr);
            }
            try
            {
                effect = effect.Replace("<eval " + text + ">", Transpiler.Convert(Azurite.MacroApply(expression), language));
            }
            catch (Azurite.Ezception e)
            {
                Debugger.stack.Pop();
                throw new Azurite.Ezception(501, $"{instruction}: Unable to evaluate the expression inside the eval", expression.Stringify(), -1, e);
            }
            #if !DEBUG
            catch (Exception e)
            {
                Debugger.stack.Pop();
                throw new Azurite.Ezception(501, $"Unexpected error while parsing {expression.Stringify()} inside <eval {text}>, associated error message: {e.Message}");
            }
            #endif
            // Debugger.stack.Pop(); Pop is done in higher level
            return true;
        }
        private static string GetEvalText(string effect)
        {
            string eval = "<eval ";
            int index = effect.IndexOf(eval);
            if (index == -1)
                return "";

            int dephlevel = 1;
            index += eval.Length;
            int i = index;
            for (; i < effect.Length && dephlevel != 0; i++)
            {
                if (effect[i] == '>')
                    dephlevel--;
                if (i < effect.Length - eval.Length && effect.IndexOf(eval, i, eval.Length) == i)
                {
                    index = (i += eval.Length);
                    // i += eval.Length;
                }
                // dephlevel++;
            }

            return effect.Substring(index, i - index - 1);

        }

        // public static HashSet<string> numberNames = new HashSet<string>();
        /// <summary> Transpile the expression into the target language.
        /// <param name="language">The targeted language.</param>
        /// <param name="expression">THe expression to translate.</param>
        /// <return>The translated expression.</return>
        /// </summary>
        public static string Execute(string language, Parser.SExpression expression, string type = null)
        {
            //Formal.descendent_verification(expression);
            List<Parser.SExpression> arguments = expression.LoadAllChild();
            List<string> forced_type = new List<string>();

            Instruction instruction = Match(language, expression, type);
            if (instruction.proto == null)
                return null;
            if (Transpiler.track_recursion)
            {
                if (Transpiler.numberNames.Contains(expression.Stringify()))
                {
                    string callStack = string.Join(" -> ", Transpiler.numberNames.Reverse());
                    throw new Azurite.Ezception(506, "stack overflow " + callStack + " -> " + expression.Stringify());
                }
                Transpiler.numberNames.Push(expression.Stringify());
            }
            var ArgumentName = instruction.proto.ConvertAll(x => x.Key);

            string effect = instruction.effect;

            int size = instruction.proto.Count;
            Debugger debugger = Debugger.create(expression.Stringify());
            // Replace global variable

            effect.Replace($"{{{Langconfig.libpath}}}", Azurite.stdlib);

            var localVariables = new Dictionary<string, string>();
            for (int i = 0; i < ArgumentName.Count; i++)
            {
                debugger.variables.TryAdd("$"+ArgumentName[i], arguments[i].Stringify());
            }
            debugger.variables.Add("effect", effect);
            debugger.variables.Add("callstack", string.Join(" -> ", Transpiler.numberNames.Reverse()));
            debugger.variables.Add("instruction", instruction.ToString());
            debugger.variables.Add("originalEffect", language);

            bool debuggerWillBreak = ArgumentName.Any(x => effect.Contains($"{{{x}}}")) || effect.Contains("<eval ");
            
            if (Azurite.debugger && debugger.ShouldBreak())
            {
                if (!debuggerWillBreak)
                {
                    debugger.Breakpoint();
                }
            }

            for (int i = 0; i < size; i++)
            {

                if (i < forced_type.Count)
                    type = forced_type[i - 1];
                else
                    type = null;


                List<string> expresionType = new List<string>();
                if (effect.Contains("$" + instruction.proto.ElementAt(i).Key + "$"))
                {
                    // expresionType = FormalReborn.GetType(arguments[i]);
                    effect = effect.Replace("$" + instruction.proto.ElementAt(i).Key + "$",
                    Transpiler.Convert($"({expresionType[expresionType.Count - 1]})", language));
                    // Convert name so user can use custom name for the type
                }
                if (effect.Contains("^" + instruction.proto.ElementAt(i).Key + "^"))
                {
                    /* if (expresionType.Count == 0)
                        expresionType = FormalReborn.GetType(arguments[i]);*/
                    effect = effect.Replace("^" + instruction.proto.ElementAt(i).Key + "^",
                    String.Join(" ", expresionType.Select(type => Transpiler.Convert($"({type})", language))));
                }

                if (Azurite.debugger && effect.Contains($"{{{instruction.proto.ElementAt(i).Key}}}"))
                {
                    if (debugger.ShouldBreak())
                    {
                        debugger.variables["effect"] = effect.Replace($"{{{instruction.proto.ElementAt(i).Key}}}", $"\x1b[31m{{{instruction.proto.ElementAt(i).Key}}}\x1b[0m");
                        debugger.Breakpoint();
                    }
                }

                switch (instruction.proto.ElementAt(i).Value.Key)
                {
                    case MATCH_LEVEL.EXACT:
                        break;
                    case MATCH_LEVEL.CALLABLE:
                        Lexer.Symbol symbo = Lexer.GetSymbol(arguments[i].data);
                        if (symbo == null)
                            throw new Azurite.Ezception(503, "call to function before definition");
                        forced_type = symbo.type;
                        effect = effect.Replace($"@{instruction.proto.ElementAt(i).Key}@", (Lexer.GetSymbol(arguments[i].data).type.Count - 1).ToString());
                        goto case MATCH_LEVEL.STRICT;
                    case MATCH_LEVEL.STRICT:
                        effect = effect.Replace("{" + instruction.proto.ElementAt(i).Key + "}", Transpiler.Convert("(" + arguments[i].data + ")", language, type));
                        break;
                    case MATCH_LEVEL.LIST:
                        string name = instruction.proto.ElementAt(i).Key;
                        Regex replacement = new Regex("{" + name + @" (\\}|[^}])*}");
                        // Get the separator, end and start of the list.

                        string text_to_replace = replacement.Match(effect).Value.Replace("\\}", "}");
                        try
                        {
                            text_to_replace = text_to_replace.Substring(name.Length + 1, text_to_replace.Length - (name.Length + 2));
                        }
                        catch (System.ArgumentOutOfRangeException)
                        {
                            throw new Azurite.Ezception(505, $"no body for parameter: {name} in \"{effect}\"");
                        }

                        Parser.SExpression body = new Parser.SExpression(text_to_replace);
                        List<Parser.SExpression> parameters = body.LoadAllChild();
                        if (parameters.Count < 2 && parameters.Count > 4)
                            throw new Exception("bad parameters in list");


                        string separator = Transpiler.Convert(parameters[1], language).Replace("\\x20", " ");
                        string start = "";
                        string end = "";
                        if (parameters.Count > 2)
                        {
                            start = Transpiler.Convert(parameters[2], language);
                            end = Transpiler.Convert(parameters[3], language);
                        }

                        List<Parser.SExpression> args = new List<Parser.SExpression>();

                        if (type != null)
                        {
                            args.Add(arguments[i]);
                            if (arguments.Count > forced_type.Count)
                            {
                                args.Add(Parser.SExpression.fromList(
                                    arguments.GetRange(i + 1, arguments.Count - i - 1)
                                ));

                            }
                        }

                        else if (arguments.Count > instruction.proto.Count)
                        {
                            args = arguments.GetRange(instruction.proto.Count - 1, arguments.Count - instruction.proto.Count + 1);
                        }
                        else if (arguments[i].has_data && arguments[i].data != "NULL")
                        {
                            args = new List<Parser.SExpression>() { arguments[i] };
                        }
                        else
                        {
                            args = arguments[i].LoadAllChild();
                        }

                        List<string> evaluate_arg = new List<string>();
                        foreach (Parser.SExpression arg in args)
                        {
                            Parser.SExpression expr = parameters[0].Clone();
                            expr.Map(_child => (_child.data == $"{instruction.proto.ElementAt(i).Key}") ? arg : _child);
                            evaluate_arg.Add(Transpiler.Convert(MacroManager.Execute(expr), language));
                        }

                        // Replace with the evaluate list.
                        effect = replacement.Replace(effect, start + string.Join(separator, evaluate_arg) + end);
                        break;
                    case MATCH_LEVEL.LIGHT:
                        if (effect.Contains($"{{{instruction.proto.ElementAt(i).Key}}}"))
                            effect = effect.Replace($"{{{instruction.proto.ElementAt(i).Key}}}",
                            arguments[i].data != null ?
                            Transpiler.Convert("(" + arguments[i].data + ")", language, type) :
                            Transpiler.Convert(arguments[i], language, type)
                            );
                        break;
                    case MATCH_LEVEL.PARTIAL:
                        Match extract = new Regex(@"\|.+\|").Match(instruction.proto.ElementAt(i).Key);
                        string variable_name = extract.Value.Trim('|');
                        string argument_name = arguments[i].data;
                        string temp_argu = "";
                        ArgumentName[i] = variable_name;
                        arguments[i] = arguments[i].Clone();
                        string instruction_name = instruction.proto.ElementAt(i).Key;
                        int nombre_char_end = instruction_name.Length - (extract.Index + extract.Length);
                        if (argument_name.Length - nombre_char_end - extract.Index != 0)
                            temp_argu = argument_name.Substring(extract.Index, (argument_name.Length - nombre_char_end - extract.Index));
                        arguments[i].data = temp_argu;

                        if (effect.Contains($"{{{variable_name}}}"))
                            effect = effect.Replace($"{{{variable_name}}}", Transpiler.Convert("(" + temp_argu + ")", language, type));
                        break;
                }
                
            }

            // If the translate contain an import then add it to the import list.
            if (instruction.import != "" && !imports_list.Contains(instruction.import))
            {
                imports_list.Add(instruction.import);
            }

            int TOUR = 0;
            while (effect != null && TOUR < MainClass.MAX_RECURSION_ALLOWED && Eval(ref effect, instruction, expression, language))
                TOUR++;
            Transpiler.numberNames.Pop();
            if (debugger.ShouldBreak())
            {
                debugger.variables["effect"] = effect;
                debugger.Breakpoint();
            }
            Debugger.remove();
            return effect;

        }

        /// <summary>Search an instruction according to the specified Match Level
        /// <param name="lang">The lang of the instruction.</param>
        /// <param name="level">The match level of the instruction.</param>
        /// <return>The correspondinsg instruction if no found instruction with only null will be return</return>
        /// </summary>
        public static Instruction SearchWithTag(string lang, List<MATCH_LEVEL> level)
        {
            foreach (Instruction instruction in instructions_list[Azurite.LanguageHandler.getLanguageIndex(lang)])
            {
                if (instruction.proto.Count == level.Count)
                {
                    int i = 0;
                    while (i < level.Count && instruction.proto.ElementAt(i).Value.Key == level[i])
                    {
                        i++;
                    }
                    if (i == level.Count) return instruction;
                }
            }

            return new Instruction(null, null, null);
        }
        #endregion
    }
}