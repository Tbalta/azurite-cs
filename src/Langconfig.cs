using System;
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
            for (; !sexpr.has_data; sexpr = sexpr.second())
            {
                if (sexpr.first().has_data)
                    throw new Exception("ERROR on parsing config files. Please check syntax.");

                switch (sexpr.first().first().data)
                {
                    case "functions":
                        function_name = sexpr.first().second().first().data;
                        break;
                    case "macros":
                        macro_name = sexpr.first().second().first().data;
                        break;
                    case "procedures":
                        procedures_name = sexpr.first().second().first().data;
                        break;
                    /*case "declaration":
                        declaration_name = sexpr.first().second().first().data;
                        break;*/
                    case "import":
                        import_name = sexpr.first().second().first().data;
                        break;
                    case "translate":
                        translate_name = sexpr.first().second().first().data;
                        break;
                    case "libpath":
                        libpath = sexpr.first().second().first().data;
                        break;
                    case "compilation":
                        compilation = sexpr.first().second().first().data;
                        break;
                    case "variables":
                        variables = sexpr.first().second().first().data;
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