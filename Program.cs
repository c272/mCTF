using System;
using System.IO;

namespace mCTF
{
    /// <summary>
    /// An emulator of the mCTF specification, for RACTF 2020.
    /// </summary>
    class Program
    {
        public static bool DebugMode = false;
        static void Main(string[] args)
        {
            //Load the first argument as the mCTF program to run.
            if (args.Length < 1) { Log.Error("No program provided."); return; }
            if (args.Length >= 2 && args[1] == "--debug") { DebugMode = true; }

            //Run.
            try
            {
                var cpu = new mCPU(File.ReadAllBytes(args[0]));
                cpu.Execute();
            }
            catch (Exception e)
            {
                Log.Error("Error running program: " + e.ToString());
            }
        }
    }
}
