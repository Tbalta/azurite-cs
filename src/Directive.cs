using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
            public Dictionary<string, KeyValuePair<MATCH_LEVEL, string>> proto;

            /// <summary> the structure in the targeted language ex: Console.Write({x}) </summary>
            public string effect;

            /// <summary> The type of the instruction ex:"num" "num" "bool"</summary> 
            public Instruction(string import, Dictionary<string, KeyValuePair<MATCH_LEVEL, string>> proto, string effect)
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

        public static List<Dictionary<string, KeyValuePair<MATCH_LEVEL, string>>> protoList = new List<Dictionary<string, KeyValuePair<MATCH_LEVEL, string>>>();
        public static List<List<Instruction>> instructions_list = new List<List<Instruction>>();
        public static List<string> imports_list = new List<string>();

        private static bool SexpressionMatch(Parser.SExpression expression, Dictionary<string, KeyValuePair<MATCH_LEVEL, string>> proto)
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

        public static List<Dictionary<string, KeyValuePair<MATCH_LEVEL, string>>> find_matching_proto(Parser.SExpression expression)
        {
            return protoList.Where(x => SexpressionMatch(expression, x)).ToList();
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

        private static bool CheckList(List<Parser.SExpression> expr, int index, string instruc_type, string forced = null, bool args_later = false)
        {


            return
            (forced != null && (instruc_type == forced)) ||
            (!args_later && expr.Count > 1 && (instruc_type == "any" || (FormalReborn.IsEquivalent(FormalReborn.GetType(expr[index]).Last(), instruc_type)))) ||
            ((args_later || expr.Count == 1) && (instruc_type == "any" || (expr.GetRange(index, expr.Count - index - 1).TrueForAll(elem =>
                           FormalReborn.IsEquivalent(FormalReborn.GetType(elem) + "...", instruc_type)))));


        }

        /// <summary> loop throught all translate and return the translate witch correspond to the argument list
        /// <param name="lang"> The language in wich the Instruction must be </param>
        /// <param name="arguments"> The list of arguments </param>
        /// <return> Return the corresponding instruction of an arguments if no found return Instruction with only null </return>
        /// </summary>
        private static Instruction Match(string lang, Parser.SExpression expression, string forced = null)
        {
            var arguments = expression.LoadAllChild();
            foreach (Instruction instruction in instructions_list[Azurite.LanguageHandler.getLanguageIndex(lang)])
            {
                Func<int, KeyValuePair<string, KeyValuePair<MATCH_LEVEL, string>>> getProto = index => instruction.proto.ElementAt(index);
                if (arguments.Count == instruction.proto.Count || getProto(instruction.proto.Count - 1).Value.Key == MATCH_LEVEL.LIST)
                {

                    int i = 0;
                    bool isLast = i == instruction.proto.Count - 1 && i != arguments.Count - 1;

                    while (i < instruction.proto.Count && arguments[i].matchProto(getProto(i)) && FormalReborn.compare_type(getProto(i).Value.Value, FormalReborn.GetType(arguments[i]).Last()))
                    {

                        i++;
                        isLast = i == instruction.proto.Count - 1 && i != arguments.Count - 1;

                    }

                    if (i == instruction.proto.Count)
                        return instruction;

                }
            }
            return new Instruction(null, null, null);
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
            List<Parser.SExpression> childs = expression.LoadAllChild();
            if (childs.Count < 2)
                return false;
            if (!childs[0].LoadAllData().TrueForAll(x => !(x is null)))
                return false;
            if (!childs[1].LoadAllData().TrueForAll(x => !(x is null)))
                return false;
            int i = 2;
            for (; i < childs.Count && childs[i].LoadAllData().TrueForAll(x => !(x is null)); i++) ;
            if (i != childs.Count)
                return false;
            while (i < childs.Count)
            {

            }
            return true;

        }
        /// <summary> Load Intruction Convert an SExpression into a Instruction and add it to the list of instruction
        /// <param name="expression"> the SExpression to load in.</param>
        /// <param name="filename"> if specified the tag to add to every Strong match. </param>
        /// </summary>
        public static void LoadInstruction(Parser.SExpression expression, string filename = "")
        {
            if (!EnsureTranslateIntegrity(expression))
                throw new Azurite.Ezception(504, "Translate integrity check failed");
            // Expression is the right brother of translate
            List<KeyValuePair<string, MATCH_LEVEL>> arguments = Evaluate(expression.first(), filename);

            //Looping through all the language defined

            List<string> type = expression.second().first().LoadAllData();
            for (int i = 0; i < type.Count; i++)
                type[i] = type[i].Replace("\"", "");

            // Converting the arguments into the prototype
            Dictionary<string, KeyValuePair<MATCH_LEVEL, string>> proto = new Dictionary<string, KeyValuePair<MATCH_LEVEL, string>>();
            int offset = 0;

            for (int i = 0; i < arguments.Count; i++)
            {
                string name = arguments[i].Key;
                MATCH_LEVEL level = arguments[i].Value;
                string arg_type = "";
                if (level == MATCH_LEVEL.EXACT || level == MATCH_LEVEL.PARTIAL)
                    // offset++;
                    type.Insert(i, "");

                else
                    arg_type = type[i - offset];
                if (proto.ContainsKey(name))
                    throw new Azurite.Ezception(505, "Two parameters have the same name");
                proto.Add(name, new KeyValuePair<MATCH_LEVEL, string>(level, arg_type));
            }
            protoList.Add(proto);
            Lexer.add_to_globals(new Lexer.Symbol(arguments[0].Key, type));

            // Loading all the language definition.
            List<Parser.SExpression> langs = expression.second().second().LoadAllChild();

            foreach (Parser.SExpression lang in langs)
            {
                // Load the targeted language / the import / the effect
                List<string> elemt = lang.LoadAllData();

                if (elemt.Count != 3)
                    throw new Azurite.Ezception(502, $"Can't import translate, 3 arguments expected, {elemt.Count} founds", lang.Stringify());

                string cible = elemt[0];
                string import = "";
                if (elemt[1] != "")
                    import = elemt[1].Substring(1, elemt[1].Length - 2).Replace($"{{{Langconfig.libpath}}}", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace("\\", "/") + "/.azurite"); ;
                string effect = elemt[2].Substring(1, elemt[2].Length - 2);

                AddInstruction(cible, new Instruction(import, proto, effect));
            }
        }
        #endregion
        #region instruction_convert

        /// <summary> Search expression to evaluate in a string and convert the expression</summary>
        /// <return> Return True while they are expression to evaluate.</return>
        /// <param name="effect"> The string to search in.</param>
        /// <param name="language"> The language to convert in.</param>
        private static bool Eval(ref string effect, List<string> argumentName, List<Parser.SExpression> arguments, string language)
        {
            // Regex reg = new Regex("<eval (.*?)>");
            // MatchCollection match = reg.Matches(effect);
            string text = GetEvalText(effect);

            if (text == "")
                return false;

            Parser.SExpression expression = new Parser.SExpression(text);
            // Parser.SExpression expression = new Parser.SExpression(match[0].Groups[1].Value.Trim());

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
                throw new Azurite.Ezception(501, $"Unable to evaluate the expression inside the eval: <eval {text}>", expression.Stringify(), -1, e);
            }
            catch (Exception e)
            {
                throw new Azurite.Ezception(501, $"Unexpected error while parsing {expression.Stringify()} inside <eval {text}>, associated error message: {e.Message}");
            }
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

        /*/// <summary> Search expression to concatenate in a string and convert the expression</summary>
        /// <return> Return the effect after application.</return>
        /// <param name="effect"> The string to search in.</param>
        /// <param name="language"> The language to convert in.</param>
        private static string Concat(ref string effect, List<string> argumentName, List<Parser.SExpression> arguments, string language)
        {
            Regex reg = new Regex("(.*?)@@(.*?)@@");
            MatchCollection match = reg.Matches(effect);

            for(MatchCollection match = reg.Matches(effect); match.Count > 0; match = reg.Matches(effect)){
                string new_lhs = () => { var i = argumentName.FindIndex( x => x == match[0].Groups[0].Value); return i == -1 ? match[0].Groups[0].Value : arguments[i];};
                string new_rhs = () => { var i = argumentName.FindIndex( x => x == match[0].Groups[1].Value); return i == -1 ? match[0].Groups[1].Value : arguments[i];};
                effect.Replace(match[0].Groups[0].Value + "@@" + match[0].Groups[1].Value, new_lhs + new_rhs);
            }

            while(match.Count > 0){

                match = reg.Matches(effect);
            }

            if (match.Count == 0)
                return false;

            Parser.SExpression expression = new Parser.SExpression(match[0].Groups[1].Value.Trim() + match[1].Groups[1].Value.Trim());
            for (int i = 0; i < argumentName.Count; i++)
            {
                if (effect.Contains($"[{argumentName[i]}]"))
                    expression.Map((Parser.SExpression expr) => (expr.data == $"[{argumentName[i]}]") ? arguments[i] : expr);
            }

            effect = reg.Replace(effect, Transpiler.Convert(Azurite.MacroApply(expression), language));
            return true;
        }*/

        /// <summary> Search for strict match level with polymorphe type, try to type them and add it to the lexer
        /// <param name="expression"> The list of arguments of the instruction.</param>
        /// <param name="instruction"> The instruction containing the match level of the parameters.</param>
        /// </summary>
        private static void addCustomToLexer(List<Parser.SExpression> expression, Instruction instruction)
        {

            // if the instrsuction contains a strict match
            int index = 0;
            while (index < instruction.proto.Count &&
                instruction.proto.ElementAt(index).Value.Key != MATCH_LEVEL.STRICT &&
                !Formal.is_polymorphic(instruction.proto.ElementAt(index).Value.Value))
                index++;

            // if we index is greater then there is no MATCH level strict with type polymorph
            if (index == instruction.proto.Count)
                return;

            var item = instruction.proto.ElementAt(index);

            for (int i = 0; i < instruction.proto.Count; i++)
            {
                if (i != index && instruction.proto.ElementAt(i).Value.Value == item.Value.Value)
                {
                    List<string> type = FormalReborn.GetType(expression[i]);
                    Lexer.add_to_globals(new Lexer.Symbol(expression[index].data, type));
                }
            }

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
                    throw new Azurite.Ezception(506, "stack overflow " + expression.Stringify() + " match with " + instruction.ToString());
                Transpiler.numberNames.Add(expression.Stringify());
            }

            // addCustomToLexer(arguments, instruction);



            string effect = instruction.effect;

            int size = instruction.proto.Count;
            // Replace global variable

            effect.Replace($"{{{Langconfig.libpath}}}", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Replace("\\", "/") + "/.azurite");

            int offset = 0;
            for (int i = 0; i < size; i++)
            {

                if (i < forced_type.Count)
                    type = forced_type[i - 1];
                else
                    type = null;
                //List<Parser.SExpression> child = expression.LoadAllChild();


                List<string> expresionType = new List<string>();
                if (effect.Contains("$" + instruction.proto.ElementAt(i).Key + "$"))
                {
                    expresionType = FormalReborn.GetType(arguments[i]);
                    effect = effect.Replace("$" + instruction.proto.ElementAt(i).Key + "$",
                    Transpiler.Convert($"({expresionType[expresionType.Count - 1]})", language));
                }
                if (effect.Contains("^" + instruction.proto.ElementAt(i).Key + "^"))
                {
                    if (expresionType.Count == 0)
                        expresionType = FormalReborn.GetType(arguments[i]);
                    effect = effect.Replace("^" + instruction.proto.ElementAt(i).Key + "^",
                    String.Join(" ", expresionType.Select(type => Transpiler.Convert($"({type})", language))));
                }


                switch (instruction.proto.ElementAt(i).Value.Key)
                {
                    case MATCH_LEVEL.EXACT:
                        offset++;
                        break;
                    case MATCH_LEVEL.CALLABLE:
                        Lexer.Symbol symbo = Lexer.GetSymbol(arguments[i].data);
                        if (symbo == null)
                            throw new Azurite.Ezception(503, "call to function before definition");
                        forced_type = symbo.type;
                        FormalReborn.GetType(expression);
                        effect = effect.Replace($"@{instruction.proto.ElementAt(i).Key}@", (Lexer.GetSymbol(arguments[i].data).type.Count - 1).ToString());
                        goto case MATCH_LEVEL.STRICT;
                    case MATCH_LEVEL.STRICT:
                        effect = effect.Replace("{" + instruction.proto.ElementAt(i).Key + "}", arguments[i].data);
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

                        if (type != null && FormalReborn.GetType(arguments[i])[0] == type)
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
                            evaluate_arg.Add(Transpiler.Convert(expr, language));
                        }

                        // Replace with the evaluate list.
                        effect = replacement.Replace(effect, start + string.Join(separator, evaluate_arg) + end);
                        break;
                    case MATCH_LEVEL.LIGHT:
                        if (effect.Contains($"{{{instruction.proto.ElementAt(i).Key}}}"))
                            effect = effect.Replace($"{{{instruction.proto.ElementAt(i).Key}}}",
                            arguments[i].data != null ?
                            arguments[i].data :
                            Transpiler.Convert(arguments[i], language, type)
                            );
                        break;
                    case MATCH_LEVEL.PARTIAL:
                        Match extract = new Regex(@"\|.+\|").Match(instruction.proto.ElementAt(i).Key);
                        string variable_name = extract.Value.Trim('|');
                        string argument_name = arguments[i].data;
                        string temp_argu = "";
                        arguments[i].data = variable_name;

                        string instruction_name = instruction.proto.ElementAt(i).Key;
                        int nombre_char_end = instruction_name.Length - (extract.Index + extract.Length);
                        if (argument_name.Length - nombre_char_end - extract.Index != 0)
                            temp_argu = argument_name.Substring(extract.Index, (argument_name.Length - nombre_char_end - extract.Index));


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
            while (effect != null && TOUR < MainClass.MAX_RECURSION_ALLOWED && Eval(ref effect, instruction.proto.Keys.ToList(), arguments, language))
                TOUR++;
            Transpiler.numberNames.Remove(expression.Stringify());
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