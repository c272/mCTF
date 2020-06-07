using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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

            //Let the memory process the initial memory blocks.
            mem.ProcessBlocks(memBlocks);
        }

        /// <summary>
        /// Executes the loaded mCTF program.
        /// </summary>
        public void Execute()
        {
            //Execute the next instruction.
            while (true)
            {
                ProcessNextInstruction();
            }
        }

        /// <summary>
        /// Processes a single mCTF instruction.
        /// </summary>
        private void ProcessNextInstruction()
        {
            //Get the instruction pointer from the memory.
            ushort iptr = mem.ISPTR;

            //Read the definition word from memory.
            ushort defWord = mem.SCODE[iptr];

            //Get the appropriate arguments (based on opcode).
            ushort[] args = GetArguments(iptr);

            //Clear memory location zero in all memory areas.
            mem.ClearLocationZero();
        }

        /// <summary>
        /// Gets arguments for an instruction at a given byte index inside SCODE.
        /// <returns></returns>
        private ushort[] GetArguments(int instrStart)
        {
            //Make the argument array (3 words).
            ushort[] args = new ushort[3];

            //Get the opcode (little endian), use lookup table to find the number of arguments.
            byte[] opcodeBits = BitConverter.GetBytes((ushort)((mem.SCODE[instrStart] & 0x1FF)))
                                            .ToArray();
            ushort opcode = BitConverter.ToUInt16(opcodeBits, 0);

            //Get number of arguments.
            int numArgs = Constants.ParameterLookup[(Instructions)opcode];
            if (numArgs == 0) { return args; }

            //Are any of the arguments registers?
            int numRegArgs = 0;
            ushort lookupHex = 0x8000;
            for (int i = 1; i <= 3; i++)
            {
                //Ignore extra ARGs.
                if (numArgs < i) { break; }

                if ((mem.SCODE[instrStart] & lookupHex) != lookupHex)
                {
                    numRegArgs++;
                }

                //Shift to next position (2 down).
                lookupHex = (ushort)(lookupHex >> 2);
            }

            //Get the starting address argument word.
            int addrArgumentStart = instrStart + 1;
            if (numRegArgs > 0)
            {
                addrArgumentStart++;
            }

            //Process each argument.
            lookupHex = 0x8000;
            bool regArgsExist = numRegArgs > 0;
            for (int i=1; i<=3; i++)
            {
                //Done yet?
                if (numArgs < i) { break; }

                //Is the argument a register?
                if ((mem.SCODE[instrStart] & lookupHex) != lookupHex)
                {
                    //Get the register in question from the "registers" word.
                    ushort regWord = mem.SCODE[instrStart + 1];
                    ushort regIndex = (ushort)((regWord >> 4 + 4 * (numRegArgs - 1)) & 0x0F);
                    switch (regIndex)
                    {
                        //RX
                        case 0x1:
                            args[i - 1] = mem.RX;
                            break;

                        //RY
                        case 0x2:
                            args[i - 1] = mem.RY;
                            break;

                        //RZ
                        case 0x3:
                            args[i - 1] = mem.RZ;
                            break;

                        //RTRGT
                        case 0x4:
                            args[i - 1] = mem.RTRGT;
                            break;

                        //RSTAT
                        case 0x5:
                            args[i - 1] = mem.RSTAT;
                            break;

                        //RCALL
                        case 0x6:
                            args[i - 1] = mem.RCALL;
                            break;

                        //Null register.
                        default:
                            args[i - 1] = 0x0;
                            break;
                    }

                    //Reduce the number of register arguments left.
                    numRegArgs--;
                }
                else
                {
                    //No, the argument is a memory argument. What type of memory is it?
                    ushort memoryTypeHex = (ushort)(lookupHex >> 1);

                    if ((mem.SCODE[instrStart] & memoryTypeHex) == memoryTypeHex)
                    {
                        //SMAIN
                        args[i - 1] = mem.SMAIN[mem.SCODE[addrArgumentStart]];
                    }
                    else
                    {
                        //SIN
                        args[i - 1] = mem.SIN[mem.SCODE[addrArgumentStart]];
                    }

                    addrArgumentStart++;
                }

                //Shift to next position (2 down).
                lookupHex = (ushort)(lookupHex >> 2);
            }

            //Return the array of arguments.
            return args;
        }
    }
}
