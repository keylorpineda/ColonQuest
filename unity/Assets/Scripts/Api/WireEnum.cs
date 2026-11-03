using System;
using System.Collections.Generic;
using System.Text;

namespace ColonQuest.Api
{
    /// <summary>
    /// Converts a closed enum to and from its snake_case name on the wire, for example
    /// <c>NewInstallment</c> and <c>new_installment</c>. The tables are built once per enum.
    /// </summary>
    public static class WireEnum<T> where T : struct, Enum
    {
        private static readonly Dictionary<string, T> ByWire = new Dictionary<string, T>();
        private static readonly Dictionary<T, string> ToWireTable = new Dictionary<T, string>();

        static WireEnum()
        {
            foreach (T value in Enum.GetValues(typeof(T)))
            {
                string wire = SnakeCase(value.ToString());
                ByWire[wire] = value;
                ToWireTable[value] = wire;
            }
        }

        public static string ToWire(T value)
        {
            return ToWireTable[value];
        }

        public static bool TryParse(string wire, out T value)
        {
            if (wire != null && ByWire.TryGetValue(wire, out value))
            {
                return true;
            }

            value = default;
            return false;
        }

        public static T Parse(string wire, string what)
        {
            if (!TryParse(wire, out T value))
            {
                throw new ApiFormatException($"Unknown {what} '{wire}'.");
            }

            return value;
        }

        /// <summary>Parses a list, skipping values this client does not know so that a newer backend never breaks the game.</summary>
        public static List<T> ParseKnown(IEnumerable<string> wires)
        {
            var list = new List<T>();
            if (wires == null)
            {
                return list;
            }

            foreach (string wire in wires)
            {
                if (TryParse(wire, out T value))
                {
                    list.Add(value);
                }
            }

            return list;
        }

        public static List<string> ToWireList(IEnumerable<T> values)
        {
            var list = new List<string>();
            foreach (T value in values)
            {
                list.Add(ToWire(value));
            }

            return list;
        }

        private static string SnakeCase(string name)
        {
            var builder = new StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c))
                {
                    if (i > 0)
                    {
                        builder.Append('_');
                    }

                    builder.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }
    }

    /// <summary>The response did not match the contract: unknown enum, missing choices or an effect out of range.</summary>
    public sealed class ApiFormatException : Exception
    {
        public ApiFormatException(string message)
            : base(message)
        {
        }
    }
}
