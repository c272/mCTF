using Microsoft.VisualBasic.CompilerServices;
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
    public partial class mCPU
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
                Log.Debug("Compressed mCTF header detected (zstd compression).");

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

            //Manipulate SCODE to remove dumb CTF image bugs.
            mem.SCODE[10] = 13;
            mem.SCODE[0x32] = 0x7D;
            mem.SCODE[81] = 0b0000000000010000;
            mem.SCODE[82] = 0x0;
        }

        /// <summary>
        /// Executes the loaded mCTF program.
        /// </summary>
        public void Execute()
        {
            //Execute the next instruction until halt.
            while (true)
            {
                if (ProcessNextInstruction()) { break; }
            }
        }

        /// <summary>
        /// Processes a single mCTF instruction.
        /// </summary>
        private bool ProcessNextInstruction()
        {
            //Get the instruction pointer from the memory.
            ushort iptr = mem.ISPTR;

            //Read the definition word from memory.
            ushort defWord = mem.SCODE[iptr];
            bool isSigned = (defWord & 0x200) == 0x200;

            //Get the appropriate arguments (based on opcode).
            List<IArgument> args = GetArguments(iptr);

            //Get the opcode for the instruction.
            byte[] opcodeBits = BitConverter.GetBytes((ushort)((mem.SCODE[iptr] & 0x1FF))).ToArray();
            ushort opcode = BitConverter.ToUInt16(opcodeBits, 0);

            //HACK FOR DUMB CTF IMAGE BUG! REMOVE ME!
            //todo: remove
            if (iptr == 0x010) { mem.RZ = 0; };

            //Execute the instruction! If it returns true, then stop execution.
            bool haltExec = ExecuteInstruction(opcode, isSigned, iptr, args);

            //Clear memory location zero in all memory areas.
            mem.ClearLocationZero();

            //If the instruction pointer hasn't been modified, move it to the next one.
            if (mem.ISPTR == iptr)
            {
                if (args.Count == 0) { mem.ISPTR++; }
                else
                {
                    mem.ISPTR = (ushort)(args.GetPointerToAfter(iptr));
                }
            }

            //Return whether the program should halt.
            return haltExec;
        }

        /// <summary>
        /// Gets arguments for an instruction at a given byte index inside SCODE.
        /// <returns></returns>
        private List<IArgument> GetArguments(int instrStart)
        {
            //Make the argument array (3 words).
            List<IArgument> args = new List<IArgument>();

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

                    //Make the argument.
                    args.Add(new RegisterArgument(regIndex, mem, (ushort)(instrStart + 1)));

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
                        args.Add(new MemoryArgument(mem.SCODE[addrArgumentStart], MemArgArea.SMAIN, mem, (ushort)addrArgumentStart));
                    }
                    else
                    {
                        //SIN
                        args.Add(new MemoryArgument(mem.SCODE[addrArgumentStart], MemArgArea.SIN, mem, (ushort)addrArgumentStart));
                    }

                    addrArgumentStart++;
                }

                //Shift to next position (2 down).
                lookupHex = (ushort)(lookupHex >> 2);
            }

            //Return the array of arguments.
            return args;
        }

        /// <summary>
        /// Executes an instruction with a given opcode and list of arguments.
        /// </summary>
        private bool ExecuteInstruction(ushort opcode, bool isSigned, ushort iptr, List<IArgument> args)
        {
            //Print the debug message for the instruction.
            PrintInstruction(opcode, isSigned, iptr, args);

            //Switch on the opcode and execute as required.
            switch (opcode)
            {
                //HALT
                case 0x000:
                    return true;

                //NOOP
                case 0x001:
                    return false;

                //INC (increment first arg)
                case 0x002:
                    return INC(args, isSigned);

                //DEC (decrement first arg)
                case 0x003:
                    return DEC(args, isSigned);

                //ADD (add second arg to first)
                case 0x004:
                    return ADDSUB(args, isSigned, true);

                //SUB (subtract second arg from first)
                case 0x005:
                    return ADDSUB(args, isSigned, false);

                //MUL (multiply first arg by second)
                case 0x006:
                    return MUL(args, isSigned);

                //DIV (divide first arg by second)
                case 0x007:
                    return DIV(args, isSigned);

                //ADDC (adds first and second + carry flag)
                case 0x008:
                    return ADDC(args, isSigned);

                //SUBC (subtracts second from first + carry)
                case 0x009:
                    return SUBC(args, isSigned);

                //READ (reads 1 char into memory)
                case 0x00A:
                    return READ(args);

                //WRIT (writes 1 char to console)
                case 0x00B:
                    return WRIT(args);

                //CPY (copies arg1 into arg2)
                case 0x00C:
                    return CPY(args);

                //MCPY (copies arg1 bitwise & arg3 into arg2.
                case 0x00D:
                    return MCPY(args);

                //ICPY (copies arg1 imm. into arg2)
                case 0x00E:
                    return ICPY(args);

                //CMP (compares arg1 to arg2)
                case 0x00F:
                    return CMP(args);

                //AND (bitwise & arg2 onto arg1)
                case 0x010:
                    return AND(args);

                //OR (bitwise | arg2 onto arg1)
                case 0x011:
                    return OR(args);

                //CMPL (bitwise complement replace arg1)
                case 0x012:
                    return CMPL(args);

                //LSHF (shift arg1 left by arg2)
                case 0x013:
                    return LSHF(args);

                //RSHF (shift arg1 right by arg2)
                case 0x014:
                    return RSHF(args, isSigned);

                //PUSH (pushes arg1 onto the stack)
                case 0x015:
                    return PUSH(args);

                //POP (puts stack val into arg1)
                case 0x016:
                    return POP(args);

                //CFLG (clears RSTAT)
                case 0x017:
                    mem.RSTAT = 0x0;
                    return false;

                //CALL (calls a function at RTRGT)
                case 0x018:
                    return CALL(args, iptr);

                //RTRN (returns val from func)
                case 0x019:
                    return RTRN();

                //RTRV (returns 0 from func)
                case 0x01A:
                    return RTRV();

                //RTL (rotates arg1 left by arg2)
                case 0x01B:
                    return RTL(args);

                //RTR (rotates arg1 right by arg2)
                case 0x01C:
                    return RTR(args);

                //CIP (copies iptr into arg1)
                case 0x01D:
                    return CIP(args);

                //BSWP (swaps low and high bytes of arg1)
                case 0x01E:
                    return BSWP(args);

                //JUMP (jumps to RTRGT)
                case 0x01F:
                    return JUMP();

                //JZRO (jump if fzero set)
                case 0x020:
                    return JZRO();

                //JEQU (jump if fequ set)
                case 0x021:
                    return JEQU();

                //JLT (jump if flt set)
                case 0x022:
                    return JLT();

                //JGT (jump if fgt set)
                case 0x023:
                    return JGT();

                //JCRY (jump if fcrry set)
                case 0x024:
                    return JCRY();

                //JINF (jump if finf set)
                case 0x025:
                    return JINF();

                //JSE (jump if fse set)
                case 0x026:
                    return JSE();

                //JSF (jump if fsf set)
                case 0x027:
                    return JSF();

                //CZRO (clears FZERO)
                case 0x030:
                    return CZRO();

                //CCRY (clears FCRRY)
                case 0x034:
                    return CCRY();

                //XOR (xor arg2 onto arg1)
                case 0x040:
                    return XOR(args);

                //SWAP
                case 0x041:
                    return SWAP(args);

                //RCPT (copies arg1 to arg2 location +/- offset)
                case 0x042:
                    return RCPT(args, isSigned);

                //RCPF (copies arg2 to arg1 location +/- offset)
                case 0x43:
                    return RCPF(args, isSigned);

                default:
                    Log.Fatal("Unknown opcode 0x" + opcode.ToString("X") + " given for instruction at SCODE 0x" + iptr.ToString("X") + ".");
                    return true;
            }
        }

        /// <summary>
        /// Prints the instruction to the console.
        /// </summary>
        private void PrintInstruction(ushort opcode, bool isSigned, ushort iptr, List<IArgument> args)
        {
            string instrMsg = "";

            //iptr
            instrMsg += "0x" + iptr.ToString("X") + " ";

            //name
            instrMsg += ((Instructions)opcode).ToString().ToLower();
            
            //signed
            if (isSigned) { instrMsg += "+ "; }
            else { instrMsg += " "; }

            //args
            foreach (var arg in args)
            {
                if (arg is MemoryArgument)
                {
                    var memArg = arg as MemoryArgument;
                    instrMsg += memArg.Area.ToString().ToLower() + " 0x" + memArg.Location.ToString("X") + ", ";
                }
                else if (arg is RegisterArgument)
                {
                    var regArg = arg as RegisterArgument;
                    instrMsg += regArg.Register.ToString().ToLower() + ", ";
                }
                else if (arg is ValueArgument)
                {
                    var valArg = arg as ValueArgument;
                    instrMsg += valArg.Value.ToString("X") + ", ";
                }
                else
                {
                    instrMsg += "[unknwn], ";
                }
            }
            instrMsg = instrMsg.TrimEnd(',', ' ');

            //Print the instruction.
            Log.Instruction(instrMsg);
        }
    }
}
