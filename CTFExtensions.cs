using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mCTF
{
    public static class mCTFExtensions
    {
        /// <summary>
        /// Reverses the given uint16.
        /// </summary>
        public static ushort Reverse(this ushort s)
        {
            return BitConverter.ToUInt16(BitConverter.GetBytes(s).Reverse().ToArray(), 0);
        }

        /// <summary>
        /// Rotates the ushort by a given amount left.
        /// </summary>
        public static ushort RotateLeft(this ushort value, int count)
        {
            return (ushort)((value << count) | (value >> (16 - count)));
}

        /// <summary>
        /// Rotates the ushort by a given amount right.
        /// </summary>
        public static ushort RotateRight(this ushort value, int count)
        {
            return (ushort)((value >> count) | (value << (16 - count)));
        }

        /// <summary>
        /// Returns whether the character is valid in ISO-8859-1.
        /// </summary>
        public static bool IsValidISO(this char input)
        {
            byte[] bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(input.ToString());
            string result = Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
            return string.Equals(input.ToString(), result);
        }

        /// <summary>
        /// Returns the address of the next instruction based on the current arguments.
        /// </summary>
        public static int GetPointerToAfter(this List<IArgument> args, int iptr)
        {
            if (args.Where(x => x is MemoryArgument || x is ValueArgument).Count() > 0)
            {
                return args.Where(x => x is MemoryArgument || x is ValueArgument).Last().ArgumentSCODELocation + 1;
            }
            else if (args.Where(x => x is RegisterArgument).Count() > 0)
            {
                return iptr + 2; //all register arguments (or some value args, but we don't care about those)
            }
            else
            {
                return iptr + 1; //no args, just next instr
            }
        }
    }
}
