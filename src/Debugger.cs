using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Azurite
{
    public class Debugger
    {
        public static bool no_menu = true;

        private enum COMMANDS_TYPE
        {
            CONTINUE,
            STEP,
            STEPIN,
            BREAK,
            PRINT,
            REMOVE,
            UP,
            DOWN,
            EMPTY,
            INVALID,
        }


        private class Command
        {
            public COMMANDS_TYPE type;
            public string argument;
        }



        public static Stack<Debugger> stack = new Stack<Debugger>();
        public static HashSet<string> Breakpoints = new HashSet<string>();



        public static Debugger create(string breakpoint)
        {
            stack.Push(new Debugger(breakpoint, stack.Count));
            return stack.Peek();
        }

        public static void remove()
        {
            if (Azurite.debugger)
            {
                Debug.Assert(stack.Count > 0, "No debugger to remove");
                stack.Pop();
            }
        }

        private static Command ParseCommand(string entry)
        {
            if (entry == null)
            {
                return new Command { type = COMMANDS_TYPE.INVALID, argument = "NULL_COMMAND" };
            }

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
                {"up", COMMANDS_TYPE.UP},
                {"u", COMMANDS_TYPE.UP},
                {"down", COMMANDS_TYPE.DOWN},
                {"d", COMMANDS_TYPE.DOWN},
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

        private int level;

        public bool stepIn = false;
        public bool step = false;
        private static Command lastCommand = new Command { type = COMMANDS_TYPE.EMPTY, argument = "" };
        public Dictionary<string, string> variables;

        private string breakpoint;

        public Debugger(string breakpoint, int level = 0)
        {
            this.breakpoint = breakpoint;
            this.level = level;
            this.variables = new Dictionary<string, string>();
        }


        public static void AddBreakpoint(string breakpoint)
        {
            Breakpoints.Add(breakpoint);
            System.Console.WriteLine("Breakpoint added: " + breakpoint);
        }

        public static void RemoveBreakpoint(string breakpoint)
        {
            Breakpoints.Remove(breakpoint);
        }

        public bool ShouldBreak()
        {
            if (level > 0)
            {
                return stack.ElementAt(stack.Count - level).stepIn || step || Breakpoints.Contains(breakpoint);
            }
            return step || Breakpoints.Contains(breakpoint);
        }

        public void PrintMenu()
        {
            if (no_menu)
            {
                System.Console.WriteLine("stopped");
                return;
            }

            try
            {
                System.Console.Clear();
            }
            catch (System.Exception)
            {
                // ignore
            }

            if (level > 0)
            {
                System.Console.WriteLine("StepIn: " + stepIn);
            }
            if (variables.ContainsKey("instruction") && variables.ContainsKey("effect"))
            {
                System.Console.WriteLine("Breakpoint hit: " + variables["instruction"]);
                System.Console.WriteLine(breakpoint + "->" + variables["effect"]);
            }
            else
            {
                System.Console.WriteLine("Breakpoint hit: " + breakpoint);
            }

        }

        public void Breakpoint()
        {
            if (level > 1)
            {
                stack.ElementAt(stack.Count - level).stepIn = false;
            }

            PrintMenu();
            while (true)
            {
                var entry = System.Console.ReadLine();
                var command = ParseCommand(entry);
                if (command.type == COMMANDS_TYPE.EMPTY)
                {
                    command = lastCommand;
                }
                else
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
                        step = true;
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
                        for (int i = 0; i < stack.Count; i++)
                        {
                            stack.ElementAt(i).step = false;
                            stack.ElementAt(i).stepIn = false;
                        }
                        return;
                    case COMMANDS_TYPE.REMOVE:
                        RemoveBreakpoint((command.argument != "") ? command.argument : breakpoint);
                        break;
                    case COMMANDS_TYPE.INVALID:
                        System.Console.WriteLine("Invalid command" + entry);
                        break;
                    case COMMANDS_TYPE.BREAK:
                        AddBreakpoint(command.argument);
                        break;
                    case COMMANDS_TYPE.UP:
                        if (level <= 0)
                        {
                            System.Console.WriteLine("Already at the top level");
                        }
                        else
                        {
                            stack.ElementAt(stack.Count - level).Breakpoint();
                            PrintMenu();
                        }
                        break;
                    case COMMANDS_TYPE.DOWN:
                        if (level == stack.Count - 1)
                        {
                            System.Console.WriteLine("Already at the bottom level");
                        }
                        else
                        {
                            return;
                        }
                        break;
                }
            }

        }

    }
}