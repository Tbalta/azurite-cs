using System;
using System.Collections.Generic;
using System.Linq;
namespace Azurite
{
    //\brief static tools functions
    class Tools
    {
        public static bool list_equal<T>(List<T> a, List<T> b) where T : class
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }
            return true;
        }
        public static string get_pretty_type(List<string> type)
        {
            string to_return = "";
            for (int i = 0; i < type.Count - 1; i++)
            {
                to_return += type[i] + " -> ";
            }
            to_return += type[type.Count - 1];
            return to_return;
        }

        public static string get_from_ast(Parser.SExpression expr)
        {
            // string to_return = "";
            if (expr.has_data) return expr.data;
            return "(" + get_from_ast(expr.first()) + " " + get_from_ast(expr.second()) + ")";
        }

        public static string repeat_string(string to_repeat, string bind, uint occurences)
        {
            if (occurences == 0) return "";
            string r = "";
            while (occurences > 1)
            {
                r = r + to_repeat + bind;
                occurences--;
            }
            return r + to_repeat;
        }

        public static List<T> tail<T>(List<T> to_get)
        {
            if (to_get.Count < 2) return new List<T>();
            return to_get.GetRange(1, to_get.Count - 1);
        }

        private static bool is_of_match_level(Parser.SExpression expression, Directive.MATCH_LEVEL lvl, string data_token = "")
        {
            switch (lvl)
            {
                case Directive.MATCH_LEVEL.EXACT:
                    return expression.has_data && expression.data == data_token.Trim('"');
                case Directive.MATCH_LEVEL.STRICT:
                    return expression.has_data;
                case Directive.MATCH_LEVEL.LIGHT:
                    return expression != null;
                case Directive.MATCH_LEVEL.LIST:
                    return !expression.has_data;
                default:
                    throw new NotImplementedException("Match value added, not used here.");
            }
            //throw new NotImplementedException("Match value added, not used here.");
        }

        private static Directive.MATCH_LEVEL get_match_level_of(string data)
        {
            if (data == null)
                return Directive.MATCH_LEVEL.LIGHT;
            if (data.StartsWith("\"") && data.EndsWith("\""))
                return Directive.MATCH_LEVEL.EXACT;
            if (data.EndsWith("..."))
                return Directive.MATCH_LEVEL.LIST;
            if (data.StartsWith("'") && data.EndsWith("'"))
                return Directive.MATCH_LEVEL.STRICT;
            //if(data.StartsWith("[") && data.EndsWith("]"))
            //    return Directive.MATCH_LEVEL.CALLABLE;
            return Directive.MATCH_LEVEL.LIGHT;
        }

        private static string get_token(string data, Directive.MATCH_LEVEL curlvl)
        {
            switch (curlvl)
            {
                case Directive.MATCH_LEVEL.EXACT:
                    return data.Trim('\"');
                case Directive.MATCH_LEVEL.STRICT:
                    return data.Trim('\'');
                case Directive.MATCH_LEVEL.LIST:
                    return data.Remove(data.Length - 3);
                case Directive.MATCH_LEVEL.LIGHT:
                    return data;
                default:
                    throw new NotImplementedException("Match value added, not used here.");
            }
            //throw new NotImplementedException("Match value added, not used here.");
        }

        private static Dictionary<string, Parser.SExpression> Merge(Dictionary<string, Parser.SExpression> a, Dictionary<string, Parser.SExpression> b)
        {
            var to_return = new Dictionary<string, Parser.SExpression>();
            if (b.Count == 0)
                return a;
            if (a.Count == 0)
                return b;

            foreach (var scrut in b)
            {
                if (a.ContainsKey(scrut.Key) && get_from_ast(a[scrut.Key]) != get_from_ast(scrut.Value))
                {
                    throw new Exception($"Macro incompatibility in subcall.");
                }
                to_return.Add(scrut.Key, scrut.Value);
            }
            foreach (var scrut in a)
            {
                if (!to_return.ContainsKey(scrut.Key))
                    to_return.Add(scrut.Key, scrut.Value);
            }
            return to_return;
        }

        /*private static Parser.SExpression fromChildren(List<Parser.SExpression> to_merge){
            if(to_merge.Count == 0)
                return new Parser.SExpression("()");
            if(to_merge.Count == 1)
                return to_merge[0];
            var to_return = new Parser.SExpression();
            to_return.first(to_merge[0].Clone());
            to_return.second(fromChildren(to_merge.GetRange(1, to_merge.Count-1)));
            return to_return;
        }*/

        public static Parser.SExpression build_expr(Parser.SExpression left, Parser.SExpression right, string data)
        {
            var r = new Parser.SExpression("()");
            r.first(left);
            r.second(right);
            r.data = data;
            r.has_data = data != "" && data != null; //to_modify => should involve forst and second
            return r;
        }

        static Parser.SExpression remove_nulls(Parser.SExpression expr)
        {
            var to_return = expr.Clone();
            to_return.Map(x => !x.has_data && x.second().has_data && x.second().data == "NULL" ? build_expr(null, null, x.data) : x);
            return to_return;
        }

        public static Parser.SExpression reinsert_nulls(Parser.SExpression expr)
        {
            var to_return = expr.Clone();
            to_return.Map(x => !x.has_data && x.second().has_data && x.second().data != "NULL" ?
            build_expr(x.first(), build_expr(x.second(), build_expr(null, null, "NULL"), null), null) : x);
            to_return.PrettyPrint();
            return to_return;
        }

        ///<summary> Matches two SExpression in order to detect macro application</summary>
        ///<param name="reference"> The body of the macro prototype</param>
        ///<param name="to_match"> The SExpression to try to match</param>
        ///<returns> A Dictionary matching atoms in prototype with SExpression. Throws if unmached</returns> 
        public static Dictionary<string, Parser.SExpression> Match(Parser.SExpression reference, Parser.SExpression to_match)
        {

            var to_return = new Dictionary<string, Parser.SExpression>();
            int i = 0;
            if (reference.has_data)
            {
                if (to_match.has_data && reference.data.Trim('\"') == to_match.data.Trim('\"'))
                {
                    to_return.Add(reference.data, to_match);
                    return to_return;
                }
                return null;
            }
            if (to_match.first() == null)
                return null;
            var to_inspect = to_match.LoadAllChild();

            var proto = reference.LoadAllChild();
            foreach (var scrut in proto)
            {
                if (scrut.has_data)
                {
                    if (to_inspect.Count <= i)
                        return null;

                    var level = get_match_level_of(scrut.data);
                    if (is_of_match_level(to_inspect[i], level, scrut.data))
                    {
                        to_return = Merge(to_return, new Dictionary<string, Parser.SExpression>() { { get_token(scrut.data, level), to_inspect[i] } });
                    }
                    else
                    {
                        //check of last_index
                        if (i == proto.Count - 1 && level == Directive.MATCH_LEVEL.LIST)
                        { //add checks for types
                            to_return = Merge(to_return, new Dictionary<string, Parser.SExpression>()
                            {{get_token(scrut.data, level), new Parser.SExpression(to_inspect.GetRange(i, to_inspect.Count-i))}});
                        }
                        else
                        {
                            return null;
                            //throw new Exception($"Macro incompatibility : {get_from_ast(reference)} / {get_from_ast(to_match)}");
                        }
                    }
                }
                else
                { //launches recursively
                    var tempdico = Match(scrut, to_inspect[i]);
                    if (tempdico == null)
                        return null;
                    to_return = Merge(to_return, Match(scrut, to_inspect[i]));
                }
                i++;
            }
            return to_return;
        }


        public static Dictionary<string, Parser.SExpression> MatchV2(Parser.SExpression reference, Parser.SExpression to_match)
        {


            Directive.MATCH_LEVEL level;
            Dictionary<string, Parser.SExpression> to_return = new Dictionary<string, Parser.SExpression>();
            if (reference.has_data)
            {
                level = get_match_level_of(reference.data);
                if (!is_of_match_level(to_match, level, reference.data) || !to_match.has_data)
                {
                    return null;
                }

                to_return.Add(get_token(reference.data, level), to_match);
                return to_return;
            }
            List<Parser.SExpression> ref_childs = reference.LoadAllChild();
            List<Parser.SExpression> match_childs = to_match.LoadAllChild();

            if (match_childs.Count < ref_childs.Count)
                return null;
            if (ref_childs.Count < match_childs.Count && get_match_level_of(ref_childs[ref_childs.Count - 1].data) != Directive.MATCH_LEVEL.LIST)
                return null;
            for (int i = 0; i < ref_childs.Count; i++)
            {
                if (match_childs[i] == null)
                    return null;
                if (!ref_childs[i].has_data)
                {
                    Dictionary<string, Parser.SExpression> temp_dict = MatchV2(ref_childs[i], match_childs[i]);
                    if (temp_dict == null)
                        return null;
                    to_return = Merge(to_return, temp_dict);
                }
                else
                {
                    level = get_match_level_of(ref_childs[i].data);
                    if (is_of_match_level(match_childs[i], level, ref_childs[i].data))
                    {
                        string token = get_token(ref_childs[i].data, level);
                        if (to_return.ContainsKey(token))
                        {
                            if (!to_return[token].Equal(match_childs[i]))
                                return null;
                        }
                        else
                            to_return.Add(get_token(ref_childs[i].data, level), match_childs[i]);

                    }
                    else if (i == ref_childs.Count - 1 && level == Directive.MATCH_LEVEL.LIST)
                        to_return.Add(get_token(ref_childs[i].data, level), Parser.SExpression.fromList(match_childs.GetRange(i, match_childs.Count - 1)));
                    else
                        return null;
                }

            }
            return to_return;
        }



        /// <summary>
        /// Check si un arbre est un peigne droit
        /// </summary>
        /// <param name="arbre">the arbre to check</param>
        /// <returns>return true si l'arbre est un peigne droit</returns>
        public static bool estPeigneDroit(Parser.SExpression arbre)
        {
            return arbre.LoadAllChild().TrueForAll(expr => (expr != null && expr.has_data));
        }

        // public static bool isAtPos(string source, string comp, int pos){
        //     int index = 0;
        //     while (index < source.Length)
        //     {

        //     }

        // }

        // public static string GetMatchingSymbol(string source, string start, string end){

        //     int starting_pos = source.IndexOf(start);
        //     if(starting_pos == source.Length)
        //         return "";


        //     int elementDiff = 1;
        //     starting_pos += start.Length;




        // }


    }


}