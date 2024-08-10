using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Azurite
{
    public class Debugger
    {
        public static bool no_menu = false;

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
            BUILT_IN,
            INVALID,
        }

        private class Command
        {
            public COMMANDS_TYPE type;
            public string argument;
        }

        private static Dictionary<string, Action<Debugger>> builtin = new Dictionary<string, Action<Debugger>>
        {
            {"callstack", (Debugger debugger) => { debugger.printCallStack(); }},
        };

        public static Stack<Debugger> stack = new Stack<Debugger>();
        public static HashSet<string> Breakpoints = new HashSet<string>();



        public static Debugger create(Parser.SExpression breakpoint)
        {
            stack.Push(new Debugger(breakpoint, stack.Count));
            return stack.Peek();

            return null;
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
            else if (builtin.ContainsKey(command))
            {
                return new Command { type = COMMANDS_TYPE.BUILT_IN, argument = command };
            }
            return new Command { type = COMMANDS_TYPE.INVALID, argument = command };

        }

        private int level;

        public bool stepIn = false;
        public bool step = false;
        private static Command lastCommand = new Command { type = COMMANDS_TYPE.EMPTY, argument = "" };
        public Dictionary<string, string> variables;

        public int line;

        private Parser.SExpression breakpoint;

        public Debugger(Parser.SExpression breakpoint, int level = 0)
        {
            this.breakpoint = breakpoint;
            this.level = level;
            this.variables = new Dictionary<string, string>();
            this.line = breakpoint.line;
        }


        public static void AddBreakpoint(Parser.SExpression breakpoint)
        {
            Breakpoints.Add(breakpoint.Stringify());
            System.Console.WriteLine("Breakpoint added: " + breakpoint.Stringify());
        }

        public static void RemoveBreakpoint(Parser.SExpression breakpoint)
        {
            Breakpoints.Remove(breakpoint.Stringify());
        }

        public bool ShouldBreak()
        {
            if (level > 0)
            {
                return stack.ElementAt(stack.Count - level).stepIn || step || Breakpoints.Contains(breakpoint.Stringify());
            }
            return step || Breakpoints.Contains(breakpoint.Stringify());
        }

        public string location()
        {
            string name, file, line, col = "";
            if (variables.ContainsKey("instruction") && variables.ContainsKey("instruction_line"))
            {
                name = variables["instruction"];
                file = variables["instruction_file"];
                line = variables["instruction_line"];
                col = variables["instruction_col"];
            }
            else
            {
                name = breakpoint.Stringify();
                file = breakpoint.file;
                line = breakpoint.line.ToString();
                col = breakpoint.column.ToString() + ":" + breakpoint.length.ToString();
            }

            if (no_menu)
                return name + "\n" + file + "\n" + line + "\n" + col;
            
            return name + "@" + file + ":" + line + ":" + col;
        }

        public void printCallStack()
        {
            for (int i = 0; i < stack.Count; i++)
            {
                System.Console.WriteLine(stack.ElementAt(i).location());
            }
        }

        public void PrintMenu()
        {
            if (no_menu)
            {
                System.Console.WriteLine("stopped");
                System.Console.Out.Flush();
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

            System.Console.WriteLine("Breakpoint hit: " + location());
            if (variables.ContainsKey("effect"))
            {
                System.Console.WriteLine(breakpoint.Stringify() + "->" + variables["effect"]);
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
                        try
                        {

                            RemoveBreakpoint((command.argument != "") ? new Parser.SExpression(command.argument) : breakpoint);
                        }
                        catch (Azurite.Ezception)
                        {
                            System.Console.WriteLine("Unable to remove breakpoint: " + command.argument);
                        }
                        break;
                    case COMMANDS_TYPE.INVALID:
                        System.Console.WriteLine("Invalid command" + entry);
                        break;
                    case COMMANDS_TYPE.BREAK:
                        try
                        {
                            AddBreakpoint(new Parser.SExpression(command.argument));
                        }
                        catch (Azurite.Ezception)
                        {
                            System.Console.WriteLine("Unable to add breakpoint: " + command.argument);
                        }
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
                    case COMMANDS_TYPE.BUILT_IN:
                        builtin[command.argument](this);
                        break;
                }
                if (Debugger.no_menu)
                {
                    System.Console.WriteLine("cmd_done");
                }
            }

        }

    }
}