using System.Collections.Generic;
using System.Linq;

namespace Azurite
{
    public class Debugger
    {

        private enum COMMANDS_TYPE
        {
            CONTINUE,
            STEP,
            STEPIN,
            BREAK,
            PRINT,
            REMOVE,
            EMPTY,
            INVALID,
        }


        private class Command
        {
            public COMMANDS_TYPE type;
            public string argument;
        }

        public static bool stepIn = false;
        public static bool step = false;
        private static Command lastCommand = new Command { type = COMMANDS_TYPE.EMPTY, argument = "" };

        private static Command ParseCommand(string entry)
        {
            // parse command like "break (+ 5 2)"
            Dictionary<string, COMMANDS_TYPE> commands = new Dictionary<string, COMMANDS_TYPE>
            {
                {"continue", COMMANDS_TYPE.CONTINUE},
                {"c", COMMANDS_TYPE.CONTINUE},
                {"step", COMMANDS_TYPE.STEP},
                {"s", COMMANDS_TYPE.STEP},
                {"break", COMMANDS_TYPE.BREAK},
                {"b", COMMANDS_TYPE.BREAK},
                {"print", COMMANDS_TYPE.PRINT},
                {"p", COMMANDS_TYPE.PRINT},
                {"remove", COMMANDS_TYPE.REMOVE},
                {"rm", COMMANDS_TYPE.REMOVE},
                {"si", COMMANDS_TYPE.STEPIN},
                {"", COMMANDS_TYPE.EMPTY}
            };
            entry = entry.Trim();
            var input = entry.Split(" ");
            var command = input[0];
            var arg = entry.Substring(command.Length).Trim();
            if (commands.ContainsKey(command))
            {
                return new Command { type = commands[command], argument = arg };
            }
            return new Command { type = COMMANDS_TYPE.INVALID, argument = command };

        }


        public static HashSet<string> Breakpoints = new HashSet<string>();

        public static void AddBreakpoint(string breakpoint)
        {
            Breakpoints.Add(breakpoint);
        }

        public static void RemoveBreakpoint(string breakpoint)
        {
            Breakpoints.Remove(breakpoint);
        }

        public static bool ShouldBreak(string breakpoint)
        {
            return Breakpoints.Contains(breakpoint);
        }

        public static void Breakpoint(string breakpoint, Dictionary<string, string> variables)
        {
            System.Console.Clear();
            if (variables.ContainsKey("instruction") && variables.ContainsKey("effect"))
            {
                System.Console.WriteLine("Breakpoint hit: " + variables["instruction"]);
                System.Console.WriteLine(breakpoint + "->" + variables["effect"]);
            } else 
            {
                System.Console.WriteLine("Breakpoint hit: " + breakpoint);
            }

            while (true)
            {
                var entry = System.Console.ReadLine();
                var command = ParseCommand(entry);
                if (command.type == COMMANDS_TYPE.EMPTY)
                {
                    command = lastCommand;
                } else 
                {
                    lastCommand = command;
                }

                switch (command.type)
                {
                    case COMMANDS_TYPE.STEP:
                        step = true;
                        return;
                    case COMMANDS_TYPE.STEPIN:
                        stepIn = true;
                        return;
                    case COMMANDS_TYPE.PRINT:
                        if (variables.ContainsKey(command.argument))
                        {
                            System.Console.WriteLine(variables[command.argument]);
                        }
                        else
                        {
                            System.Console.WriteLine("Variable not found");
                        }
                        break;
                    case COMMANDS_TYPE.CONTINUE:
                        return;
                    case COMMANDS_TYPE.REMOVE:
                        RemoveBreakpoint((command.argument != "") ? command.argument : breakpoint);
                        break;
                    case COMMANDS_TYPE.INVALID:
                        System.Console.WriteLine("Invalid command");
                        break;
                    case COMMANDS_TYPE.BREAK:
                        AddBreakpoint(command.argument);
                        break;
                }
            }

        }

    }
}