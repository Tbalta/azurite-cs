using System.Collections.Generic;
using System.Xml.Serialization;
using System;

namespace Azurite{

    using UniqueSymbolTable = List<Lexer.Symbol>;
    using EquivalenceTable = List<Parser.pair<string, List<string>>>;
    public class Formal{

        public static bool is_polymorphic(string type){ // Un polymorphe commence avec '#'
            return type != "" && type[0] == '#'; 
        }
        public static bool is_variadic(string type){  //Un nom de liste fini avec '...'
            return type.EndsWith("...");
        }
        public static string unvariadic(string type){ // Retourne le nom de la liste sans les '...'
            return type.Substring(0, type.Length - 3);
        }
        public static bool are_equivalent(List<string> a, List<string> b){ // Teste l'equivalence de 2 listes
            if(a.Count != b.Count) return false;
            for(int i = 0; i < a.Count; i++){
                if(!is_polymorphic(a[i]) && !is_polymorphic(b[i])){
                    if(a[i] != b[i]) return false;
                }
            }
            return true;
        }

        public static int list_level(string type){ // Retourne le nombre de recursion de liste ex : (num...)... = 1
            int r = 0;
            while(is_variadic(type)){
                r++;
                type = unvariadic(type);
            }
            return r;
        }
        
        public static bool are_equivalent_enhanced(string real, string from_lexer) => ((!is_polymorphic(real) && !is_polymorphic(from_lexer) && real == from_lexer) // Je lis pas ca va te faire foutre Daniel ou tanguy y'a que vous 2 pour ecrire cette merde
                                                                           || (!is_polymorphic(real) && is_polymorphic(from_lexer) && list_level(real) >= list_level(from_lexer))
                                                                           || (is_polymorphic(real) && is_polymorphic(from_lexer) && list_level(real) >= list_level(from_lexer)) 
                                                                           || (is_polymorphic(real) && !is_polymorphic(from_lexer)));
        public static bool are_equivalent(string a, string b){     //Teste l'équivalence 
            //should treat the case of list_level
            if(!is_polymorphic(a) && !is_polymorphic(b)){
                if(a != b) return false;
            }
            return true;
        }

        public static Lexer.Symbol extract_about(string name, UniqueSymbolTable table){
            return table.FindLast(x => x.symbol == name);
        }

        public static List<string> type_of(Parser.SExpression to_get){
            ///don't check for errors
            if(to_get.has_data){  
                var lst = extract_about(to_get.data, Lexer.builtins);
                if(lst != null){
                    if(lst.is_keyword)           ///MUST BE MODIFIED LATER
                        throw new NotImplementedException(to_get.data + " is a keyword");
                    return lst.type;
                }
                lst = extract_about(to_get.data, Lexer.locals);
                if(lst != null){
                    if(lst.is_keyword)           ///MUST BE MODIFIED LATER
                        throw new NotImplementedException(to_get.data + " is a keyword");
                    return lst.type;
                }
                lst = extract_about(to_get.data, Lexer.globals);
                if(lst != null){
                    if(lst.is_keyword)           ///MUST BE MODIFIED LATER
                        throw new NotImplementedException(to_get.data + " is a keyword");
                    return lst.type;
                }

                float a = 0.0f;
                if(float.TryParse(to_get.data, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out a)){
                    var tp = "num";
                    var b = new List<string>();
                    b.Add(tp);
                    return b;
                }
                if(to_get.data[0] == '"' && to_get.data[to_get.data.Length-1] == '"'){
                    var tp = "str";
                    var b = new List<string>();
                    b.Add(tp);
                    return b;
                }
                if(to_get.data == "true" || to_get.data == "false"){
                    return new List<string>(){"bool"};
                }
            } else{                                                                                         //// == if not leaf
                if(type_of(to_get.first()).Count == 1){
                    var type = type_of(to_get.first());
                    var temp = new Parser.SExpression();
                    temp = to_get.Clone();
                    while(!temp.second().has_data && temp.second().data != "NULL"){
                        //if(!are_equivalent(type_of(temp.first()), type)){
                        //    throw new Exception( " ( " + Tools.get_from_ast(temp.first()) + " "+ type[0] +" )" );
                        //}          
                        temp = temp.second().Clone();
                    }
                    var in_list = type[0] + "...";
                    var to_return = new List<string>();
                    to_return.Add(in_list);
                    return to_return;
                } else {
                    var temp = new Parser.SExpression();
                    temp = to_get.Clone();
                    var type_to_apply = type_of(temp.first());
                    for(int i = 0; i < type_to_apply.Count-1; i++){
                        if( !temp.has_data ){
                            temp = temp.second();
                        }else{
                            throw new Exception("Arity Error (" + i.ToString() +") :\n\tFunction : " + Tools.get_from_ast(to_get.first()) + "\n\tIn : " + Tools.get_from_ast(to_get.second()) );
                        }
                        if(is_variadic(type_to_apply[i])){
                            var cmp = unvariadic(type_to_apply[i]);
                            if(i == type_to_apply.Count -2){               ///if is last argument
                                for(; !temp.second().has_data && temp.second().data != "NULL"; temp = temp.second()){
                                    if(!are_equivalent(type_of(temp.first())[0], cmp)){
                                        throw new Exception( Tools.get_from_ast(temp.first()) + " should be of type " + type_to_apply[i] + " but is of type " + type_of(temp.first())[0] );
                                    }
                                }
                                i = type_to_apply.Count -2;
                            } else{
                                if(!are_equivalent(type_of(temp.first())[0], type_to_apply[i])){
                                    throw new Exception( Tools.get_from_ast(temp.first()) + " should be of type " + type_to_apply[i] + " but is of type " + type_of(temp.first())[0] );
                                }
                            }
                            
                        } else if(!are_equivalent(type_of(temp.first())[0], type_to_apply[i])){
                            throw new Exception( Tools.get_from_ast(temp.first()) + " should be of type " + type_to_apply[i] + " but is of type " + type_of(temp.first())[0] );
                        }
                    }
                    return type_to_apply.GetRange(type_to_apply.Count-1, 1);
                } 
            }
            return new List<string>{"#1..."};
        }

        public static List<string> get_type_of(Parser.SExpression to_get, UniqueSymbolTable table){
            if(to_get.has_data){
                var lst = extract_about(to_get.data, table);
                if(lst != null){
                    return lst.type;
                }
            }
            return type_of(to_get);
        }

        public static List<string> modify_type_with_equivalences(List<string> to_modify, int index, string new_type){
            string m = to_modify[index];
            if(!are_equivalent_enhanced(m, new_type))
                throw new Exception($"ERROR : uncompatible types {m} and {new_type}");
            while(is_variadic(m) && is_variadic(new_type)){
                m = unvariadic(m);
                new_type = unvariadic(new_type);
            }
            //TODO : replace with the good polymorphic id
            List<string> to_return = new List<string>();
            foreach (var t in to_modify){
                to_return.Add(t.Replace(m, new_type));
            }
            return to_return;
        }

        public static string descendent_verification(Parser.SExpression body, string return_contraint = ""){
            List<string> current_function_type = body.has_data ? type_of(body) : type_of(body.first());

            if(return_contraint == "" && !body.has_data && body.first().has_data && body.second().has_data && body.second().data == "NULL")
                return unvariadic(type_of(body.first())[0]);

            if(return_contraint != "" && is_polymorphic(return_contraint)) return_contraint = type_of(body)[0];

            if(current_function_type.Count == 1){
                if(are_equivalent_enhanced(return_contraint, type_of(body)[0]) || return_contraint == ""){
                    return type_of(body)[0];
                } else{
                    throw new Exception($"{Tools.get_from_ast(body)} is of type {Tools.get_pretty_type(type_of(body))} but should be {return_contraint} in {Tools.get_from_ast(body)}.");
                }
            }
            try{
                if(return_contraint != "") 
                    current_function_type = modify_type_with_equivalences(current_function_type, current_function_type.Count-1, return_contraint);
            } catch(Exception err){
                throw new Exception($"At {Tools.get_from_ast(body)} => {err.Message}");
            }
            var parameters = Tools.tail(body.LoadAllChild());
            bool is_arity_respected = parameters.Count == current_function_type.Count-1;
            string last_argument_type = is_arity_respected ? current_function_type[current_function_type.Count - 2] : ( 
                is_variadic(current_function_type[current_function_type.Count - 2]) ? unvariadic(current_function_type[current_function_type.Count - 2]) :
                throw new Exception($"arity error in {body}"));
            int i = 0;
            foreach(var scrut in parameters){
                if(i < current_function_type.Count - 2){
                    var v = descendent_verification(scrut, current_function_type[i]);
                    if(!are_equivalent_enhanced(v, current_function_type[i])){
                        throw new Exception($"{Tools.get_from_ast(scrut)} is of type {v} but should be {current_function_type[i]} in {Tools.get_from_ast(body)}.");
                    }
                    current_function_type = modify_type_with_equivalences(current_function_type, i, v);
                } else{
                    var v = descendent_verification(scrut, last_argument_type);
                    if(!are_equivalent_enhanced(last_argument_type, v))
                        throw new Exception($"{Tools.get_from_ast(scrut)} is of type {v} but should be {current_function_type[i]} in {Tools.get_from_ast(body)}.");
                }
                i++;
            }
            return current_function_type[current_function_type.Count-1];
        }

        public static string get_type_of_in(string token, Parser.SExpression body, bool check_syntax=true/*out List<List<string>> equivalences*/, string return_contraint = ""){
            
            var temp = body.Clone();
            List<string> founds = new List<string>();

            string fun = temp.first().data;
            if(fun == token)
                return unvariadic(type_of(body.second())[0]);
            
            List<string> current_function_type = type_of(temp.first());
            if(return_contraint != "" && !is_polymorphic(return_contraint))
                current_function_type = modify_type_with_equivalences(current_function_type, current_function_type.Count-1, return_contraint);
            //bool is_function = current_function_type.Count > 1;
            
            if(temp.has_data && temp.data == "NULL")
                return "#-1";

            temp = temp.second();

            if(temp.has_data && temp.data == "NULL")
                return "#-1";

            for (int i = 1; !temp.has_data || temp.data != "NULL"; temp = temp.second().Clone()){
                if(temp.first().has_data){
                    if(temp.first().data == token){
                        if(is_variadic(current_function_type[current_function_type.Count-2])){      //ERREUR EST ICI
                            if(i-1 < current_function_type.Count-2 ||  body.LoadAllChild().Count <= current_function_type.Count ){
                                founds.Add(current_function_type[i-1]);
                                //founds.Add(current_function_type[current_function_type.Count-2]);
                            } else{
                                founds.Add(unvariadic(current_function_type[current_function_type.Count-2]));
                            }
                        }else{
                            if(i < current_function_type.Count){
                                founds.Add(current_function_type[i-1]);
                            } else{
                                throw new Exception("ARITY ERROR");
                            }
                        }
                    }
                } else{
                    
                    var expected = "";
                    if(!(i-1 < current_function_type.Count-2 ||  body.LoadAllChild().Count <= current_function_type.Count)){
                        expected = unvariadic(current_function_type[current_function_type.Count-2]);
                    } else{
                        expected = current_function_type[i-1];
                    }
                    var rec = get_type_of_in(token, temp.first(), false, expected);
                    if(rec != "#-1")
                        founds.Add(rec);
                }
                i++;
            }

            ///// VÉRIFIER LA COHÉRENCE DES TYPES ET SIMPLIFIER
            if(founds.Count > 0){
                string not_polymorphic = "#-1";
                int list_lvl = -1;
                
                foreach (var scrut in founds){
                    if(is_polymorphic(scrut)){
                        if(list_lvl == -1){
                            list_lvl = list_level(scrut);
                        } else{
                            if(return_contraint != "" && list_level(scrut) != list_lvl){
                                string message = $"{token} can't be of type {Tools.repeat_string("list", " of ", (uint)list_level(scrut))} ";
                                message += $"and of type {Tools.repeat_string("list", " of ", (uint)list_lvl)}.";
                                throw new Exception(message);
                            }
                        }

                    } else {

                        if(return_contraint != "" && list_lvl != -1 && list_level(scrut) < list_lvl){
                            string message = $"{token} can't be of type {Tools.repeat_string("list", " of ", (uint)list_level(scrut))} ";
                            message += $"and of type {Tools.repeat_string("list", " of ", (uint)list_lvl)}.";
                            throw new Exception(message);
                        }

                        if(not_polymorphic == "#-1"){
                            not_polymorphic = scrut;
                        } else{
                            if(not_polymorphic != scrut){
                                throw new Exception($"INCOHERENT TYPE IN : {Tools.get_from_ast(body)}\n{token} can't be of type {not_polymorphic} and {scrut}");
                            }
                        }
                    }



                }
                if(founds.Count == 1){
                    not_polymorphic = founds[0];
                }
                //if(not_polymorphic == "#-1" && founds.Count == 1)
                //    not_polymorphic = founds[0];
                
                //if(check_syntax)
                //    descendent_verification(body);
                return not_polymorphic;
            } else{
                if(check_syntax)
                    descendent_verification(body);
                return "#-1";
            }
        }

        public static string get_function_return_type(Parser.SExpression body, UniqueSymbolTable inferred){
            List<string> current_function_type_in_lexer = new List<string>(type_of(body.first()));
            bool is_function = current_function_type_in_lexer.Count > 1;

            if(!is_function){
                var r = get_type_of(body, inferred);
                return r[r.Count-1];
            }
                
            if(is_function && !is_polymorphic(current_function_type_in_lexer[current_function_type_in_lexer.Count-1])){
                return current_function_type_in_lexer[current_function_type_in_lexer.Count-1];
            }

            int i = 0;
            for(Parser.SExpression scrut = body.second(); !scrut.has_data || scrut.data != "NULL"; scrut = scrut.second() ){
                current_function_type_in_lexer = modify_type_with_equivalences(current_function_type_in_lexer, i, type_of(scrut.first())[0]);
                i++;
            }
            /*foreach (var t in inferred){
                current_function_type_in_lexer = modify_type_with_equivalences(current_function_type_in_lexer, i, t.type[0]);
                i++;
            }*/

            return current_function_type_in_lexer[current_function_type_in_lexer.Count-1];
        }

        public void check_syntax(Parser.SExpression to_check){
            throw new NotImplementedException("check_syntax");
        }


        /*Should include the defun */
        public static List<string> type_of_func_not_patched(Parser.SExpression func){
            if(!func.first().has_data || func.first().data != Langconfig.function_name)
                throw new NotSupportedException(Tools.get_from_ast(func) + "is not a function.");
            
            //List<List<string>> equivalences = new List<List<string>>();

            Parser.SExpression scrut = func.second().first();
            string fn_symbol = "";

            uint y = 0;
            do {
                if(!scrut.first().has_data)
                    throw new Exception( Tools.get_from_ast(scrut.first()) + "should be an atom.");
                if(y==0)
                    fn_symbol = scrut.first().data;
                else
                    Lexer.locals.Add(new Lexer.Symbol(scrut.first().data, (uint)0));
                scrut = scrut.second();
                y++;
            } while (!scrut.has_data);           // <=> NULL encountered

            Lexer.Symbol fn = new Lexer.Symbol(fn_symbol, (uint)Lexer.locals.Count);
            fn.type[fn.type.Count - 1] = type_of(func.second().second().first())[0];
            Lexer.locals.Add(fn);

            y = 0;
            foreach (var parameter in Lexer.locals){

                var t = get_type_of_in(parameter.symbol, func.second().second().first());
                parameter.type = new List<string>(){t};
                if(t != "#-1" && (!is_polymorphic(t) || is_variadic(t)))
                    fn.type = modify_type_with_equivalences(fn.type, (int)y, t);
                y++;

            }

            y = 0;
            foreach(var parameter in Lexer.locals){
                parameter.type = new List<string>(){fn.type[(int)y]};
                y++;
            }

            fn.type[fn.type.Count-1] = get_function_return_type(func.second().second().first(), Lexer.locals);

            descendent_verification(func.second().second().first());
            
            Lexer.locals = new List<Lexer.Symbol>();
            Lexer.globals.Add(fn);            
            return fn.type;
        }






        /*Should include the defun */
        public static List<string> type_of_func(Parser.SExpression func){

            Lexer.locals = new List<Lexer.Symbol>();

            if(!func.first().has_data || func.first().data != Langconfig.function_name)
                throw new NotSupportedException(Tools.get_from_ast(func) + "is not a function.");

            
            //List<List<string>> equivalences = new List<List<string>>();

            if(!func.second().first().has_data)
                throw new NotSupportedException(Tools.get_from_ast(func) + "is not a function : Name unreachable.");

            string fn_symbol = func.second().first().data;
            
            Parser.SExpression scrut = func.second().second().first();

            uint y = 0;
            do {
                if(!scrut.first().has_data)
                    throw new Exception( Tools.get_from_ast(scrut.first()) + "should be an atom.");
                //if(y==0)
                //    fn_symbol = scrut.first().data;
                //else
                Lexer.locals.Add(new Lexer.Symbol(scrut.first().data, (uint)0));
                scrut = scrut.second();
                y++;
            } while (!scrut.has_data);           // <=> NULL encountered

            Lexer.Symbol fn = new Lexer.Symbol(fn_symbol, (uint)Lexer.locals.Count);
            Lexer.globals.Add(fn);
            
            //fn.type[fn.type.Count-1] = get_function_return_type(func.second().second().second().first(), new List<Lexer.Symbol>(){});
            //Lexer.globals.Add(fn);

            y = 0;
            foreach (var parameter in Lexer.locals){

                var t = get_type_of_in(parameter.symbol, func.second().second().second().first());
                if(is_polymorphic(t)){
                    var lvl = list_level(t);
                    t = parameter.type[0];
                    for(int i = 0; i < lvl; i++){
                        t += "...";
                    }
                }
                parameter.type = new List<string>(){t};
                if(t != "#-1" && (!is_polymorphic(t) || is_variadic(t)))
                    fn.type = modify_type_with_equivalences(fn.type, (int)y, t);
                y++;

            }

            y = 0;
            foreach(var parameter in Lexer.locals){
                parameter.type = new List<string>(){fn.type[(int)y]};
                y++;
            }

            fn.type[fn.type.Count-1] = get_function_return_type(func.second().second().second().first(), Lexer.locals);

            descendent_verification(func.second().second().second().first(), fn.type[fn.type.Count-1]);
            
            Lexer.globals.Add(fn);            
            return fn.type;
        }
    }

}