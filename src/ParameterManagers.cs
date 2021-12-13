using System;
using System.Collections.Generic;
using System.IO;

namespace Azurite
{
    /// <summary>
    /// Contains main method for handling the differents parameters.
    /// </summary>
    public class ParameterManagers
    {
        /// <summary>
        /// A Command is represented by the name, it's aliases and a callback
        /// </summary>
        public class Command : IEquatable<Command>
        {
            /// <summary>
            /// The name of the command
            /// </summary>
            public string name;
            /// <summary>
            /// The aliases of the macro
            /// </summary>
            public List<string> aliases;
            /// <summary>
            /// The call back which will be executed when the command will be found.
            /// </summary>
            public Action<List<string>> callback;

            /// <summary>
            /// If the callback is not execute this will be executed.
            /// </summary>
            public Action DefaultCallback;

            private bool wasExecuted = false;

            public bool ExecuteAfter = false;

            /// <summary>
            /// Get the hash code of the macro.
            /// </summary>
            /// <returns>Return the hashcode of the macro</returns>
            public override int GetHashCode()
            {
                return name.GetHashCode();
            }

            /// <summary>
            /// Check if the command is equal to the obj.
            /// </summary>
            /// <returns>Return true if the command is equal.</returns>
            public override bool Equals(object obj)
            {
                return Equals(obj as Command);
            }

            /// <summary>
            /// Return true if the command is equal to <paramref name="obj"/>
            /// </summary>
            /// <returns>Return true if the command is equal.</returns>
            public bool Equals(Command obj)
            {
                return obj != null &&
                    (obj.name == this.name ||
                        this.aliases.Contains(obj.name) ||
                        obj.aliases.Contains(this.name));

            }

            private List<string> args = null;
            /// <summary>
            /// Execute the callbacks of the commands with the specified arguments.
            /// </summary>
            /// <param name="arguments"></param>
            public void ExecuteCallback(List<string> arguments = null)
            {
                if (this.ExecuteAfter)
                {
                    if (this.args == null)
                        args = arguments;
                    else if (args != null)
                    {

                        this.callback(args);
                        this.wasExecuted = true;
                    }
                }
                else if (!this.wasExecuted && arguments != null)
                {
                    this.callback(arguments);
                    this.wasExecuted = true;
                }
            }

            /// <summary>
            /// Try to execute the default callback if the main callback wasn't executed.
            /// </summary>
            public void ExecuteDefault()
            {
                if (!wasExecuted && this.DefaultCallback != null)
                    this.DefaultCallback();
            }

            ///<summary> A command has an aim multiple alias and a callback wich is executed with a string containing the arguments
            ///<param name="name"> The name of the command.</param>
            ///<param name="callback"> An action containing the callback of the command.</param>
            ///<param name="alias"> A List of the alias of the command.</param>
            ///<param name="DefaultCallback">If the callback is not executed the action to execute.</param>
            ///</summary>
            public Command(string name, Action<List<string>> callback = null, List<string> alias = null, bool ExecuteAfter = false, Action DefaultCallback = null)
            {
                this.name = name;
                this.callback = callback;
                this.aliases = (alias == null) ? new List<string>() : alias;
                this.DefaultCallback = DefaultCallback;
                this.ExecuteAfter = ExecuteAfter;
            }
        }
        static private string input;

        /// <summary>
        /// OnInput will call when he input file will be found.
        /// </summary>
        /// <value>OnInput sets the callback</value>
        // public static Action<string> OnInput { set => onInput = value; }

        // private static Action<string> onInput;
        static List<Command> commandList = new List<Command>();

        ///<summary> Add a command to the list of commands
        ///<param name="command"> The command to add.</param>
        ///</summary>
        public static void registerCommand(Command command)
        {
            commandList.Add(command);
        }

        private static void ExecuteDefault()
        {
            // onInput(input);
            foreach (Command command in commandList)
            {
                command.ExecuteCallback();
                command.ExecuteDefault();
            }
        }

        ///<summary> Run through an list of args and trigger the command.
        ///<param name="args">The args to parse.</param>
        ///</summary>
        public static void Execute(string[] args)
        {
            // input = args[0];

            List<string> parameter = new List<string>();
            Command currentcom = commandList[commandList.IndexOf(new Command(args[0]))];

            for (int i = 1; i < args.Length; i++)
            {

                if (commandList.IndexOf(new Command(args[i])) > -1)
                {
                    currentcom.ExecuteCallback(parameter);

                    currentcom = commandList[commandList.IndexOf(new Command(args[i]))];
                    parameter = new List<string>();

                }
                else
                    parameter.Add(args[i]);
            }
            currentcom.ExecuteCallback(parameter);
            ExecuteDefault();

        }
    }
}