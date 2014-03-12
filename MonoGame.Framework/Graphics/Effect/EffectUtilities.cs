using System;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework
{
    public class EffectUtilities
    {
        public static Dictionary<string, string> ReadableEffectCode = new Dictionary<string, string>();

        public static string  Join<T>(IEnumerable<T> data)
        {
            return "[" + string.Join(", ", from value in data select "" + value) + "]";
        }

        public static string Params(params object[] parameters)
        {
            string str = "";
            for (int i = 0; i+1 < parameters.Length; i += 2)
            {
                if (i > 0)
                    str += "; ";
                str += parameters[i] + "=" + parameters[i + 1];
            }
            return str;
        }

        public static string[] SplitLines(string code)
        {
            return code.Split(new [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static bool MatchesMonoGameMetaLine(string line, string commandName, out string command)
        {
            string[] starts = new string[] { "#monogame " + commandName + "(", "#monogame " + commandName + " " };
            foreach (string start in starts)
            {
                if (line.StartsWith(start))
                {
                    command = line.Substring(start.Length);
                    command = command.Trim(' ', '\t');
                    if (start.Contains("(") && command.EndsWith(")"))
                    {
                        command = command.Substring(0, command.Length - 1);
                    }
                    return true;
                }
            }
            command = "";
            return false;
        }

        public static string ParseParam(string command, string name, string defaultValue)
        {
            string key = name + "=";
            if (command.Contains(key))
            {
                int begin = command.IndexOf(key) + key.Length;
                if (begin + 1 < command.Length)
                {
                    int end = command.IndexOf(";", begin);
                    if (end != -1)
                        return command.Substring(begin, end - begin);
                    else
                        return command.Substring(begin);
                }
            }
            return defaultValue;
        }

        public static int ParseParam(string command, string name, int defaultValue)
        {
            string str = ParseParam(command, name, "");
            if (string.IsNullOrEmpty(str))
            {
                return defaultValue;
            }
            else
            {
                int result;
                if (int.TryParse(str, out result))
                {
                    return result;
                }
                else
                {
                    throw new Exception("Invalid number <" + str + "> in command: <" + command + ">");
                }
            }
        }

        public static int[] ParseParam(string command, string name, int[] defaultValue)
        {
            string str = ParseParam(command, name, "");
            if (string.IsNullOrEmpty(str))
            {
                return defaultValue;
            }
            else if (str.StartsWith("[") && str.EndsWith("]"))
            {
                str = str.Trim('[', ']');
                List<int> resultValues = new List<int>();
                foreach (string _value in str.Split(new [] { ',' }))
                {
                    string value = _value.Trim(' ', '\t');
                    int result;
                    if (int.TryParse(value, out result))
                    {
                        resultValues.Add(result);
                    }
                    else
                    {
                        throw new Exception("Invalid number <" + str + "> in command: <" + command + ">");
                    }
                }
                return resultValues.ToArray();
            }
            else
            {
                return defaultValue;
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}

