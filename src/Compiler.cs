using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Azurite
{
    /// <summary>
    /// Contains the method to compile the azurite source code in order to simplify it for the transpilation.
    /// </summary>
    public class Compiler
    {
        /// <summary>
        /// Main methode to Compile (simplify) a source code.
        /// </summary>
        /// <param name="expression">The expression to simplify</param>
        /// <returns>An S-expression simplified.</returns>
        public static Parser.SExpression Compile(Parser.SExpression expression)
        {
            if (expression.has_data)
            {
                if (EnvironmentManager.IsVariable(expression.data))
                    return EnvironmentManager.GetVariable(expression.data);
                return expression;
            }

            if (expression.first().data == Langconfig.function_name || expression.first().data == Langconfig.variables)
                return expression;

            if (Directive.known_token.Contains(expression.first().data))
                return FoldingExpression(Compile(EnvironmentManager.ExecuteFunc(expression.first().data, expression.second())));


            if (expression.first().data == "if")
                return ResolveCond(ref expression) ? Compile(expression) : expression;
            if (expression.first().data == "or" || expression.first().data == "and")
                return ResolveBool(expression);

            if (expression.first() != null)
                expression.first(Compile(expression.first()));
            if (expression.second() != null)
                expression.second(Compile(expression.second()));
            return FoldingExpression(expression);
        }

        /// <summary>
        /// Apply constant folding to an S-expresssion.
        /// </summary>
        /// <param name="expression">The S-expression to simplify</param>
        /// <returns>An S-expression simplified.</returns>
        public static Parser.SExpression FoldingExpression(Parser.SExpression expression)
        {

            if (expression.has_data || expression.second().has_data)
                return expression;


            if (Tools.estPeigneDroit(expression))
                return SearchAndExecute(expression);

            //Parser.SExpression temp = expression.second();
            List<Parser.SExpression> childs = expression.LoadAllChild();
            for (int i = 0; i < childs.Count; i++)
            {
                childs[i] = FoldingExpression(childs[i]);
            }

            return SearchAndExecute(Parser.SExpression.fromList(childs));
        }

        /// <summary>
        /// Containing the description of how to fold according to different operator.
        /// </summary>
        /// <param name="expression">The S-expression to simplify</param>
        /// <returns>An S-expression simplified.</returns>
        private static Parser.SExpression SearchAndExecute(Parser.SExpression expression)
        {
            if (expression.first().data == "+")
                expression = SimplifyExpression<double>(expression.second(),
                    (expr => double.TryParse(expr.data, NumberStyles.Any, new CultureInfo("en-US"), out _)),
                    (expr => Convert.ToDouble(expr.data, CultureInfo.InvariantCulture)),
                    (liste => new Parser.SExpression(liste.Sum(x => x))),
                    "+"
                    );

            else if (expression.first().data == "@")
                expression = SimplifyExpression<string>(expression.second(),
                    (expr => !double.TryParse(expr.data, NumberStyles.Any, new CultureInfo("en-US"), out _)),
                    (expr => expr.data.Trim('\"')),
                    (liste => new Parser.SExpression(String.Join("", liste), null, null)),
                    "@"
                );

            else if (expression.first().data == "*")
                expression = SimplifyExpression<double>(expression.second(),
                    (expr => double.TryParse(expr.data, NumberStyles.Any, new CultureInfo("en-US"), out _)),
                    (expr => Convert.ToDouble(expr.data, CultureInfo.InvariantCulture)),
                    (liste =>
                    {
                        double result = 1;
                        foreach (var item in liste)
                        {
                            result *= Convert.ToDouble(item, CultureInfo.InvariantCulture);
                        }
                        return new Parser.SExpression(result);
                    }),
                    "*"
                );


            else if (expression.first().data == "-")
                expression = SimplifyExpression<double>(expression.second(),
                    (expr => double.TryParse(expr.data, NumberStyles.Any, new CultureInfo("en-US"), out _)),
                    (expr => Convert.ToDouble(expr.data, CultureInfo.InvariantCulture)),
                    // (value => new Parser.SExpression(value.ToString(CultureInfo.InvariantCulture), null, null)),
                    (liste =>
                    {
                        double result = liste[0];
                        for (int i = 1; i < liste.Count; i++)
                        {
                            result -= liste[i];
                        }
                        return new Parser.SExpression(result);
                    }),
                    "-"
                );

            else if (expression.first().data == "/")
                expression = SimplifyExpression<double>(expression.second(),
                    (expr => double.TryParse(expr.data, NumberStyles.Any, new CultureInfo("en-US"), out _)),
                    (expr => Convert.ToDouble(expr.data, CultureInfo.InvariantCulture)),
                    (liste =>
                    {
                        double result = Convert.ToDouble(liste[0], CultureInfo.InvariantCulture);
                        for (int i = 1; i < liste.Count; i++)
                        {
                            result /= Convert.ToDouble(liste[i], CultureInfo.InvariantCulture);
                        }
                        return new Parser.SExpression(result);
                    }),
                    "/"
                );

            else if (expression.first().data == "mod")
                expression = SimplifyExpression<double>(expression.second(),
                    (expr => double.TryParse(expr.data, NumberStyles.Any, new CultureInfo("en-US"), out _)),
                    (expr => Convert.ToDouble(expr.data, CultureInfo.InvariantCulture)),
                    (liste =>
                    {
                        double result = Convert.ToDouble(liste[0], CultureInfo.InvariantCulture);
                        for (int i = 1; i < liste.Count; i++)
                        {
                            result %= Convert.ToDouble(liste[i], CultureInfo.InvariantCulture);
                        }
                        return new Parser.SExpression(result);
                    }),
                    "/"
                );

            else if (expression.first().data == "<")
                expression = SimplifyExpression<double>(expression.second(),
                    (expr => double.TryParse(expr.data, NumberStyles.Any, new CultureInfo("en-US"), out _)),
                    (expr => Convert.ToDouble(expr.data, CultureInfo.InvariantCulture)),
                    (liste =>
                    {
                        string value = (liste.Zip(liste.Skip(1), (a, b) => new { a, b })
                        .All(p => (p.a < p.b))) ? "true" : "false";
                        return new Parser.SExpression(value, null, null);
                    }),
                    "<"
                );

            else if (expression.first().data == ">")
                expression = SimplifyExpression<double>(expression.second(),
                    (expr => double.TryParse(expr.data, NumberStyles.Any, new CultureInfo("en-US"), out _)),
                    (expr => Convert.ToDouble(expr.data, CultureInfo.InvariantCulture)),
                    (liste => new Parser.SExpression((liste.Zip(liste.Skip(1), (a, b) => new { a, b })
                        .All(p => (p.a > p.b))) ? "true" : "false", null, null)),
                    ">",
                    true
                );

            else if (expression.first().data == ">=")
                expression = SimplifyExpression<double>(expression.second(),
                    (expr => double.TryParse(expr.data, NumberStyles.Any, new CultureInfo("en-US"), out _)),
                    (expr => Convert.ToDouble(expr.data, CultureInfo.InvariantCulture)),
                    (liste => new Parser.SExpression((liste.Zip(liste.Skip(1), (a, b) => new { a, b })
                        .All(p => (p.a >= p.b))) ? "true" : "false", null, null)),
                    ">",
                    true
                );

            else if (expression.first().data == "<=")
                expression = SimplifyExpression<double>(expression.second(),
                    (expr => double.TryParse(expr.data, NumberStyles.Any, new CultureInfo("en-US"), out _)),
                    (expr => Convert.ToDouble(expr.data, CultureInfo.InvariantCulture)),
                    (liste => new Parser.SExpression((liste.Zip(liste.Skip(1), (a, b) => new { a, b })
                        .All(p => (p.a <= p.b))) ? "true" : "false", null, null)),
                    "<=",
                    true
                );
            else if (expression.first().data == "=")
                expression = SimplifyExpression<double>(expression.second(),
                    (expr => double.TryParse(expr.data, NumberStyles.Any, new CultureInfo("en-US"), out _)),
                    (expr => Convert.ToDouble(expr.data, CultureInfo.InvariantCulture)),
                    (liste => new Parser.SExpression((liste.Zip(liste.Skip(1), (a, b) => new { a, b })
                        .All(p => (p.a == p.b))) ? "true" : "false", null, null)),
                    "=",
                    true
                );
            else if (expression.first().data == "!=")
                expression = SimplifyExpression<double>(expression.second(),
                    (expr => double.TryParse(expr.data, NumberStyles.Any, new CultureInfo("en-US"), out _)),
                    (expr => Convert.ToDouble(expr.data, CultureInfo.InvariantCulture)),
                    (liste => new Parser.SExpression(liste.Zip(liste.Skip(1), (a, b) => new { a, b })
                        .All(p => p.a == p.b) ? "false" : "true", null, null)),
                    "!=",
                    true
                );

            else if (expression.first().data == "merge")
                expression = SimplifyExpression<Parser.SExpression>(
                    expression.second(),
                    (expr => true),
                    (expr => expr),
                    (liste =>
                    {
                        List<Parser.SExpression> result = new List<Parser.SExpression>();
                        liste.ForEach(elmt => result.AddRange(elmt.LoadAllChild()));
                        return Parser.SExpression.fromList(result);
                    }),
                    "merge"
                );




            return expression;
        }

        /// <summary>
        /// Apply constant folding to an S-expression.
        /// </summary>
        /// <param name="expression">The S-expression to simplify.</param>
        /// <param name="validator">Return true if the S-expression can be folded.</param>
        /// <param name="convertisseur">Convert each S-expression in the desired type for folding. </param>
        /// <param name="evaluator">Evaluate a list of type and return the string corresponding to the new value of the S-expression></param>
        /// <param name="keyword">The keyword of corresponding of the operator</param>
        /// <typeparam name="T">The type desired for the conversion of the S-expression</typeparam>
        /// <returns></returns>
        private static Parser.SExpression SimplifyExpression<T>(
            Parser.SExpression expression,
            Func<Parser.SExpression, bool> validator,
            Func<Parser.SExpression, T> convertisseur,
            Func<List<T>, Parser.SExpression> evaluator, string keyword, bool needAll = false)
        {


            List<Parser.SExpression> nonEvaluated = new List<Parser.SExpression>();
            List<T> evaluated = new List<T>();

            List<Parser.SExpression> childs = expression.LoadAllChild();
            if (childs.Count == 1 && !childs[0].has_data)
                childs = childs[0].LoadAllChild();

            foreach (Parser.SExpression child in childs)
            {

                if (validator(child))
                {
                    evaluated.Add(convertisseur(child));
                }
                else
                {
                    nonEvaluated.Add(child);
                }
            }

            // Parser.SExpression temp = expression;


            if (needAll && nonEvaluated.Count > 1)
                return expression;

            if (evaluated.Count > 0)
            {
                nonEvaluated.Insert(0, evaluator(evaluated));
            }
            if (nonEvaluated.Count > 1)
            {
                nonEvaluated.Insert(0, new Parser.SExpression(keyword, null, null));
            }

            return (nonEvaluated.Count > 1) ? Parser.SExpression.fromList(nonEvaluated) : nonEvaluated[0];
        }




        /// <summary>
        /// Try to simplify a condition.
        /// </summary>
        /// <param name="expression">The S-expression containing the condition.</param>
        /// <returns>The S-expression simplified.</returns>
        private static bool ResolveCond(ref Parser.SExpression expression)
        {
            List<Parser.SExpression> elements = expression.LoadAllChild();
            elements[1] = Compile(elements[1]);

            if (!elements[1].has_data)
            {
                // Parser.SExpression result = new Parser.SExpression("if", null, Compile(expression.second()));
                expression.second(Compile(expression.second()));
                return false;
            }

            if (elements[1].data == "true")
            {
                expression = elements[2];
                // return Compile(elements[2]);
            } else {

            expression = elements[3];
            }
            // return Compile(elements[3]);
            return true;

        }

        private static Parser.SExpression ResolveBool(Parser.SExpression expression)
        {
            List<Parser.SExpression> childs = expression.LoadAllChild();
            List<bool> evaluated = new List<bool>();
            Func<List<bool>, bool> stopper;
            Func<List<bool>, string> evaluator;

            switch (expression.first().data)
            {
                case "or":
                    stopper = (x => x.Count != 0 && x[x.Count - 1]);
                    evaluator = (x => x.Any(elmt => elmt) ? "true" : "false");
                    break;
                case "and":
                    stopper = (x => x.Count != 0 && !x[x.Count - 1]);
                    evaluator = (x => x.TrueForAll(z => z) ? "true" : "false");
                    break;
                default:
                    stopper = (x => true);
                    evaluator = (x => "NaB");
                    break;
            }

            for (int i = 1; i < childs.Count && !stopper(evaluated); i++)
            {
                evaluated.Add(Compile(childs[i]).data == "true");
            }

            return new Parser.SExpression(evaluator(evaluated), null, null);

        }
    }
}