using System;
using System.Collections.Generic;
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
            Parser.SExpression temp = Execute(Azurite.DEBUG ? expression.Clone() : expression, ref modif, index);
            // if(Azurite.DEBUG)
            //     Azurite.debug_list.Insert(0,expression.Stringify() + "-->" + temp.Stringify());
            return temp;
        }
        public static Parser.SExpression Execute(Parser.SExpression expression, ref bool modification, int index = 0)
        {

            if (expression.first() != null)
                expression.first(Execute(expression.first(), ref modification, 0));
            if (expression.second() != null)
                expression.second(Execute(expression.second(), ref modification, 0));
            // index = 0;
            Dictionary<string, Parser.SExpression> proto = FindMacro(expression, ref index);



            if (proto == null)
            {
                return expression;
            }
            modification = true;

            Parser.SExpression effect = macro_list[index].body.Clone();
            if (Azurite.DEBUG)
            {

                Azurite.debug_list.Add(expression.Stringify() + "-->");
                Console.WriteLine(expression.Stringify() + "is matched with " + macro_list[index].parameters.Stringify());
            }



            foreach (KeyValuePair<string, Parser.SExpression> argument in proto)
            {
                effect.Map(expr => (expr.data == argument.Key) ?
                    (!argument.Value.has_data && (argument.Value.first() == null || argument.Value.second() == null)
                     ? argument.Value.first() : argument.Value) : expr);
                if (argument.Value.data != null)
                    effect.MapData(expr => expr.data.Replace("@" + argument.Key + "@", argument.Value.data.Trim('\"')).Replace("\\\"", "\""));
                else
                    effect.MapData(expr => expr.data.Replace("@" + argument.Key + "@", argument.Value.Stringify()));

            }

            //effect = Tools.reinsert_nulls(effect);
            //effect.PrettyPrint();
            if (Azurite.DEBUG)
                Azurite.debug_list[Azurite.debug_list.Count - 1] += effect.Stringify();

            return Execute(effect, 0);
        }

        private static Parser.SExpression Replace(Parser.SExpression expression, string target, Parser.SExpression replacement)
        {
            /*
                Replace build a new SExpression based on an existing SExpression
                if the data of the current expression is equal to the target
                 then this expression is replaced by another expression
            */
            if (expression == null)
                return null;

            if (expression.data == target)
                return replacement;

            if (expression.first() != null)
                expression.first(Replace(expression.first(), target, replacement));

            if (expression.second() != null)
                expression.second(Replace(expression.second(), target, replacement));

            return expression;
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