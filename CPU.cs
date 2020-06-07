using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Zstandard.Net;

namespace mCTF
{
    /// <summary>
    /// The virtual CPU for running mCTF programs.
    /// </summary>
    public class mCPU
    {
        /// <summary>
        /// The memory of mCTF.
        /// </summary>
        private Memory mem;

        public mCPU(byte[] program)
        {
            //Initialize memory.
            mem = new Memory();
            byte[] memBlocks;

            //Check the type of mCTF program it is.
            byte[] signature = program.Take(4).ToArray();
            if (signature.SequenceEqual(Constants.STD_MCTF_HEADER))
            {
                //Copy the entire program into SCODE memory, it's just a standard program.
                Log.Debug("Standard mCTF header detected (no compression).");

                //Remove the header.
                memBlocks = program.Skip(4).ToArray();
            }
            else if (signature.SequenceEqual(Constants.ZCOMPRESSED_MCTF_HEADER))
            {
                Log.Debug("Compressed mCTF header detected (no compression).");

                //Use ZStandard to decompress (past first 4 bytes).
                using (var memoryStream = new MemoryStream(program.Skip(4).ToArray()))
                using (var compressionStream = new ZstandardStream(memoryStream, CompressionMode.Decompress))
                using (var temp = new MemoryStream())
                {
                    compressionStream.CopyTo(temp);
                    memBlocks = temp.ToArray();
                }
            }
            else
            {
                Log.Fatal("Invalid mCTF header: " + Encoding.Default.GetString(signature));
                return;
            }

            //Let the memory process the memory blocks.
            mem.ProcessBlocks(memBlocks);
        }
    }
}
