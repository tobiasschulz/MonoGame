using System;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework
{
    public class EffectUtilities
    {
        public static Dictionary<string, string> ReadableEffectCode = new Dictionary<string, string>();

        public static string  Join<T>(T[] data) where T : struct
        {
            return "[" + string.Join(", ", from value in data select "" + value) + "]";
        }

        public static string Params(params object[] parameters)
        {
            string str = "";
            for (int i = 0; i+1 < parameters.Length; i += 2)
            {
                if (i > 0)
                    str += ", ";
                str += parameters[i] + "=" + parameters[i + 1];
            }
            return str;
        }
    }
}

