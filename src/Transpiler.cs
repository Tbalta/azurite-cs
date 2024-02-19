using System.Collections;
using System.Collections.Generic;

namespace Azurite
{
    /// <summary>
    /// Transpiler contain the mains method to convert an S-expression.
    /// </summary>
    public class Transpiler
    {
        public static bool track_recursion = true;
        public static HashSet<string> numberNames = new HashSet<string>();

        /// <summary>
        /// Convert an S-expression in the specified language
        /// </summary>
        /// <param name="expression">The expression to convert.</param>
        /// <param name="language">The language to convert in.</param>
        /// <returns>Return the expression converted in the language.</returns>
        public static string Convert(Parser.SExpression expression, string language, string type = null)
        {

            //If no child then return the data fo the current expression if there is data.
            if (expression.first() == null)
                return (expression.data == "NULL") ? "" : expression.data;
            if (expression.first().data == Langconfig.function_name)
                FormalReborn.SetContextFunc(expression);


            // Try to find some translate in the expression and convert them into string.
            string test = Directive.Execute(language, expression, type);
            if (expression.first().data == Langconfig.function_name)
                FormalReborn.ExitContextFunc();
            if (test != null)
                return test;

            // If the first child has data

            //Get the data of the first child then convert the second.
            if (!expression.second().is_end)
            {
                // return "not found";
                throw new Azurite.Ezception(501, $"No translate found in {language}", expression.Stringify());

            }
            var result = Convert(expression.first(), language);
            return result;

        }
        public static string Convert(string input, string language, string type = null)
        {
            return Transpiler.Convert(new Parser.SExpression(input), language, type);
        }

        public static void ResetTracking()
        {
            numberNames = new HashSet<string>();
        }

    }

}