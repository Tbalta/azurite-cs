using System;
using System.Collections.Generic;
using System.Reflection;

namespace Azurite
{
    public static class Langconfig
    {
        public static void load(string filepath = "")
        {
            if (filepath == "")
                filepath = Azurite.stdlib + "/stdlib/langconfig.azur";
            //add more errors
            is_loaded = true;

            var sexpr = new Parser.SExpression(System.IO.File.ReadAllText(filepath));
            List<Parser.SExpression> sexprs = sexpr.LoadAllChild();
            foreach (Parser.SExpression expr in sexprs)
            {
                List<string> data = expr.LoadAllData();
                switch (data[0])
                {
                    case "functions":
                        function_name = data[1];
                        break;
                    case "macros":
                        macro_name = data[1];
                        break;
                    case "procedures":
                        procedures_name = data[1];
                        break;
                    /*case "declaration":
                        declaration_name = data[1];
                        break;*/
                    case "import":
                        import_name = data[1];
                        break;
                    case "translate":
                        translate_name = data[1];
                        break;
                    case "libpath":
                        libpath = data[1];
                        break;
                    case "compilation":
                        compilation = data[1];
                        break;
                    case "variables":
                        variables = data[1];
                        break;
                    default:
                        throw new Exception("ERROR on reading config files categories. Please check syntax.");
                }
            }

            Type type = typeof(Langconfig); // MyClass is static class with static properties

            foreach (var p in type.GetFields())
            {
                var v = p.GetValue(null); // static classes cannot be instanced, so use null...
                if(v is null)
                    throw new Azurite.Ezception(505, $"Lang config invalid: {p.Name} is not set.");
            }
            if(Azurite.DEBUG)
                Console.WriteLine("Lang config loaded");


        }
        public static string function_name;
        public static string macro_name;
        public static string procedures_name;
        //public static string declaration_name;
        public static string import_name;
        public static string translate_name;
        public static string libpath;
        public static string compilation;
        public static string variables;
        public static bool is_loaded = false;

    }

}