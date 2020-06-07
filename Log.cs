using System;
using System.Collections.Generic;
using System.Text;

namespace mCTF
{
    public static class Log
    {
        //Debug log.
        public static void Debug(string msg)
        {
            PrintWithColouredName(msg, "DEBUG", ConsoleColor.DarkYellow);
        }

        //Memory log.
        public static void Memory(string msg)
        {
            PrintWithColouredName(msg, "MEMORY", ConsoleColor.Cyan);
        }

        //Fatal error has occured.
        public static void Fatal(string msg)
        {
            throw new Exception(msg);
        }

        //Generic error has occured.
        public static void Error(string msg)
        {
            PrintWithColouredName(msg, "ERROR", ConsoleColor.Red);
        }

        /// <summary>
        /// Writes a log message with a coloured header.
        /// </summary>
        public static void PrintWithColouredName(string msg, string name, ConsoleColor colour)
        {
            Console.ForegroundColor = colour;
            Console.Write($"[{name}]");
            Console.ResetColor();
            Console.Write($" {msg}\n");
        }
    }
}
