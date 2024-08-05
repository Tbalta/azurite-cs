using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Azurite
{
    /// <summary>
    /// Transpiler contain the mains method to convert an S-expression.
    /// </summary>
    public class Transpiler
    {
        public static bool track_recursion = true;
        public static Stack<string> numberNames = new Stack<string>();

        /// <summary>
        /// Convert an S-expression in the specified language
        /// </summary>
        /// <param name="expression">The expression to convert.</param>
        /// <param name="language">The language to convert in.</param>
        /// <returns>Return the expression converted in the language.</returns>
        public static string Convert(Parser.SExpression expression, string language, string type = null)
        {

            List<Parser.SExpression> children = expression.LoadAllChild();
            
            if (children.Count == 0)
            {
                return expression.data?? "";
            }
            if (children[0].data == Langconfig.function_name)
                FormalReborn.SetContextFunc(expression);


            // Try to find some translate in the expression and convert them into string.
            string result = Directive.Execute(language, expression, type);
            if (children[0].data == Langconfig.function_name)
                FormalReborn.ExitContextFunc();
            if (result != null)
                return result;
            
            // This is for compatibility purpose, to not translate atoms
            if (children[0].data != null)
                return children[0].data;
            throw new Azurite.Ezception(501, $"No translate found in {language}", expression.Stringify());
        }
        public static string Convert(string input, string language, string type = null)
        {
            return Convert(new Parser.SExpression(input), language, type);
        }

        public static void ResetTracking()
        {
            numberNames = new Stack<string>();
        }

    }

}