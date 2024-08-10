using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Azurite
{
    /// <summary>
    /// Macro manager handle macro
    /// </summary>
    public class MacroManager
    {
        /*
        *Macro manager description
        A macro is composed by a name, the name of the macro and an effect
        example:
        ? how it's working
        it's just remplacing the node in the AST containing an x by a corresponding SExpression.

        */
        /// <summary>
        /// Contain all data relative to the macro name, parameters and body.
        /// </summary>
        protected struct Macro
        {


            /// <summary>The S-expression representing the parameters</summary>
            public Parser.SExpression parameters;

            /// <summary>The body is the S-expression in wich you need to replace the parameters.</summary>
            public Parser.SExpression body;

            /// <summary>
            /// Instantiate a new Macro.
            /// </summary>
            /// <param name="parameters">The parameters of the macro.</param>
            /// <param name="body">S-expression representing the body of the macro.</param>
            public Macro(Parser.SExpression parameters, Parser.SExpression body)
            {
                this.parameters = parameters;
                this.body = body;
            }

            public void Exec()
            {
                this.body = MacroManager.Execute(body);
            }

        }

        // List of all macro.
        private static List<Macro> macro_list = new List<Macro>();

        private static void AddMacro(Parser.SExpression parameters, Parser.SExpression body)
        {
            macro_list.Add(new Macro(parameters, body));
            macro_list[macro_list.Count - 1].Exec();
        }




        /// <summary>
        /// Replace the parameters in the macro body by the corresponding S-expression
        /// </summary>
        /// <param name="expression">The expression to replace.</param>
        /// <param name="index">The index at witch the search must start.</param>
        /// <returns>Return an the macro's body with the arguments inside</returns>
        public static Parser.SExpression Execute(Parser.SExpression expression, int index = 0)
        {
            bool modif = false;
            Parser.SExpression temp = Execute(expression.Clone(), ref modif, index);
            // if(Azurite.DEBUG)
            //     Azurite.debug_list.Insert(0,expression.Stringify() + "-->" + temp.Stringify());
            return temp;
        }
        public static Parser.SExpression Execute(Parser.SExpression expression, ref bool modification, int index = 0)
        {
            for (int i = 0; i < expression.childs.Count; i++)
            {
                expression.childs[i] = Execute(expression.childs[i], ref modification, index);
            }

            Dictionary<string, Parser.SExpression> proto = FindMacro(expression, ref index);

            if (proto == null)
            {
                return expression;
            }
            modification = true;

            Parser.SExpression effect = macro_list[index].body.Clone();
            Debugger debugger = Debugger.create(expression);
            if (Azurite.debugger)
            {
                debugger.variables.Add("effect", effect.Stringify());
                debugger.variables["instruction"] = expression.Stringify();
            }

            if (Azurite.DEBUG)
            {

                Azurite.debug_list.Add(expression.Stringify() + "-->");
                Console.WriteLine(expression.Stringify() + "is matched with " + macro_list[index].parameters.Stringify());
            }



            foreach (KeyValuePair<string, Parser.SExpression> argument in proto)
            {
                if (Azurite.debugger)
                {
                    if (debugger.ShouldBreak() && (effect.Stringify().Contains(" " + argument.Key + " ") || effect.Stringify().Contains("@" + argument.Key + "@")))
                    {
                        debugger.variables["effect"] = effect.Stringify()
                            .Replace($" {argument.Key} ", $" \x1b[31m{argument.Key}\x1b[0m ")
                            .Replace($"@{argument.Key}@", $" \x1b[31m@{argument.Key}@\x1b[0m ");
                        debugger.Breakpoint();
                    }
                }

                effect.Map(expr =>
                {
                    if (expr.data != argument.Key)
                        return expr;
                    return argument.Value;
                });

                if (argument.Value.data != null)
                {
                    effect.MapData(expr =>
                    {
                        if (expr.data == null)
                            return expr.data;
                        return expr.data.Replace("@" + argument.Key + "@", argument.Value.data.Trim('\"')).Replace("\\\"", "\"");
                    });
                }
                else
                {
                    effect.MapData(expr =>
                    {
                        if (expr.data == null)
                            return expr.data;
                        return expr.data.Replace("@" + argument.Key + "@", argument.Value.Stringify());
                    });
                }

            }

            if (Azurite.DEBUG)
                Azurite.debug_list[Azurite.debug_list.Count - 1] += effect.Stringify();

            if (Azurite.debugger)
            {
                if (debugger.ShouldBreak())
                {
                    debugger.variables["effect"] = $"\x1b[31m{effect.Stringify()}\x1b[0m";
                    debugger.Breakpoint();
                }
            }

            var result = Execute(effect, 0);
            if (Azurite.debugger)
            {
                bool stepIn = debugger.stepIn;
                Debugger.remove();
                Debugger.stack.Peek().stepIn = debugger.stepIn;
            }
            return result;
        }

        /// <summary>
        /// Search the corresponding macro body of an expression. 
        /// </summary>
        /// <param name="expression">The S-expression to match.</param>
        /// <param name="offset">The offset used to start the search in the macro list.</param>
        /// <returns>Find the paratemers corresponding to the expression.</returns>
        public static Dictionary<string, Parser.SExpression> FindMacro(Parser.SExpression expression, ref int offset)
        {
            Dictionary<string, Parser.SExpression> proto = null;


            for (int i = 0; i < macro_list.Count && proto == null; i++)
            {

                proto = Tools.MatchV2(macro_list[(i + offset) % macro_list.Count].parameters, expression);
                if (proto != null)
                    offset = (offset + i) % macro_list.Count;

            }
            return proto;
        }



        /// <summary>
        /// Load a macro in the macro manager.
        /// </summary>
        /// <param name="expression">The expression containing the macro</param>
        /// <param name="filename">If precised the name of the macro will have filename.name.</param>
        public static void LoadMacro(Parser.SExpression expression, string filename = "")
        {
            /*
                ! First child of expression NEED to be the proto !
                ! Second child of expression NEED to be the body !
            */

            List<Parser.SExpression> macro_parsed = expression.LoadAllChild();

            if (macro_parsed.Count > 3)
                throw new ArgumentException("Macro exception: too much arguments");
            if (macro_parsed.Count < 3)
                throw new ArgumentException("Macro exception: not enough arguments");

            Parser.SExpression parameters = macro_parsed[1];
            //Parser.SExpression body = macro_parsed[2].first() != null ? macro_parsed[2].first() : macro_parsed[2];
            Parser.SExpression body = macro_parsed[2];

            AddMacro(parameters, body);
        }

    }
}