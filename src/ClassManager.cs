// using System;
// using System.Collections.Generic;

// namespace Azurite
// {
//     public static class ClassManager
//     {
//         public class Azuclass
//         {
//             public Func<Parser.SExpression, Parser.SExpression, Parser.SExpression> add;
//             public Func<Parser.SExpression, Parser.SExpression, Parser.SExpression> sub;
//             public Func<Parser.SExpression, Parser.SExpression, Parser.SExpression> mult;
//             public Func<Parser.SExpression, Parser.SExpression, Parser.SExpression> div;
//             public Func<Parser.SExpression, bool> isType;
//             public string name;

//             public Azuclass(Func<Parser.SExpression, Parser.SExpression, Parser.SExpression> add,
//                             Func<Parser.SExpression, Parser.SExpression, Parser.SExpression> sub,
//                             Func<Parser.SExpression, Parser.SExpression, Parser.SExpression> mult,
//                             Func<Parser.SExpression, Parser.SExpression, Parser.SExpression> div,
//                             Func<Parser.SExpression, bool> isType,
//                             string name)
//             {
//                 this.add = add;
//                 this.sub = sub;
//                 this.mult = mult;
//                 this.div = div;
//                 this.isType = isType;
//                 this.name = name;
//             }
//         }

//         public static List<Azuclass> builtins = new List<Azuclass>();
        
//         public void init(){
//             builtins.Add(new Azuclass(
//                 (var a, var b => a +b ),

//             ));
//         }

//     }
// }