using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace mCTF
{
    public partial class mCPU
    {
        /// <summary>
        /// Moves the second argument to the location of the first, with an offset.
        /// </summary>
        private bool RCPF(List<IArgument> args, bool isSigned)
        {
            //Is the first argument a memory one (as required)?
            if (!(args[0] is MemoryArgument))
            {
                Log.Fatal("RCPF first argument is not a memory address, as required by the spec (at SCODE location " + args[0].ArgumentSCODELocation + ").");
                return false;
            }

            //Get the offset from RTRGT.
            int offset = mem.RTRGT;

            //If sign isn't set, reverse the offset to go backwards.
            if (!isSigned) { offset = -offset; }

            //Copy the first argument into the second w/ offset.
            var memArg = args[0] as MemoryArgument;
            if (memArg.Area == MemArgArea.SIN)
            {
                mem.SIN[memArg.Location + offset] = args[1].Read();
            }
            else if (memArg.Area == MemArgArea.SMAIN)
            {
                mem.SMAIN[memArg.Location + offset] = args[1].Read();
            }
            else
            {
                Log.Fatal("Unknown memory area to copy to in RCPF (SCODE location " + memArg.ArgumentSCODELocation + ").");
                return true;
            }

            //Set FZERO.
            mem.FZERO = args[1].Read() == 0;
            return false;
        }

        /// <summary>
        /// Moves the first argument to the location of the second, with an offset.
        /// </summary>
        private bool RCPT(List<IArgument> args, bool isSigned)
        {
            //Is the second argument a memory one (as required)?
            if (!(args[1] is MemoryArgument))
            {
                Log.Fatal("RCPT second argument is not a memory address, as required by the spec (at SCODE location " + args[1].ArgumentSCODELocation + ").");
                return false;
            }

            //Get the offset from RTRGT.
            int offset = mem.RTRGT;

            //If sign isn't set, reverse the offset to go backwards.
            if (!isSigned) { offset = -offset; }

            //Copy the first argument into the second w/ offset.
            var memArg = args[1] as MemoryArgument;
            if (memArg.Area == MemArgArea.SIN)
            {
                mem.SIN[memArg.Location + offset] = args[0].Read();
            }
            else if (memArg.Area == MemArgArea.SMAIN)
            {
                mem.SMAIN[memArg.Location + offset] = args[0].Read();
            }
            else
            {
                Log.Fatal("Unknown memory area to copy to in RCPT (SCODE location " + memArg.ArgumentSCODELocation + ").");
                return true;
            }

            //Set FZERO.
            mem.FZERO = args[0].Read() == 0;
            return false;
        }

        /// <summary>
        /// Swaps the first and second argument.
        /// </summary>
        private bool SWAP(List<IArgument> args)
        {
            ushort temp = args[0].Read();
            args[0].Write(args[1].Read(), Instructions.SWAP);
            args[1].Write(temp, Instructions.SWAP);

            //Set FZERO.
            mem.FZERO = args[0].Read() == 0 || args[1].Read() == 0;
            return false;
        }

        /// <summary>
        /// XORs the second argument onto the first.
        /// </summary>
        private bool XOR(List<IArgument> args)
        {
            args[0].Write((ushort)(args[0].Read(false) ^ args[1].Read(false)), Instructions.XOR, false);
            mem.FZERO = args[0].Read() == 0;
            return false;
        }

        /// <summary>
        /// Clears FCRRY.
        /// </summary>
        private bool CCRY()
        {
            mem.FCRRY = false;
            return false;
        }

        /// <summary>
        /// Clears FZERO.
        /// </summary>
        private bool CZRO()
        {
            mem.FZERO = false;
            return false;
        }

        /// <summary>
        /// Jumps to RTRGT if FSF is set.
        /// </summary>>
        private bool JSF()
        {
            if (mem.FSF) { return JUMP(); }
            return false;
        }
        /// <summary>
        /// Jumps to RTRGT if FSE is set.
        /// </summary>>
        private bool JSE()
        {
            if (mem.FSE) { return JUMP(); }
            return false;
        }

        /// <summary>
        /// Jumps to RTRGT if FINF is set.
        /// </summary>>
        private bool JINF()
        {
            if (mem.FINF) { return JUMP(); }
            return false;
        }

        /// <summary>
        /// Jumps to RTRGT if FCRRY is set.
        /// </summary>>
        private bool JCRY()
        {
            if (mem.FCRRY) { return JUMP(); }
            return false;
        }

        /// <summary>
        /// Jumps to RTRGT if FGT is set.
        /// </summary>>
        private bool JGT()
        {
            if (mem.FGT) { return JUMP(); }
            return false;
        }

        /// <summary>
        /// Jumps to RTRGT if FLT is set.
        /// </summary>>
        private bool JLT()
        {
            if (mem.FLT) { return JUMP(); }
            return false;
        }

        /// <summary>
        /// Jumps to RTRGT if FEQUL is set.
        /// </summary>>
        private bool JEQU()
        {
            if (mem.FEQUL) { return JUMP(); }
            return false;
        }

        /// <summary>
        /// Jumps to RTRGT if FZERO is set.
        /// </summary>>
        private bool JZRO()
        {
            if (mem.FZERO) { return JUMP(); }
            return false;
        }

        /// <summary>
        /// Sets the instruction pointer to RTRGT.
        /// </summary>
        private bool JUMP()
        {
            //Jumping to 0x0000 is a halt command.
            if (mem.RTRGT == 0x0) { return true; }

            //Set ISPTR.
            mem.ISPTR = mem.RTRGT;
            return false;
        }

        /// <summary>
        /// Swaps the low and high bytes of first argument.
        /// </summary>
        private bool BSWP(List<IArgument> args)
        {
            args[0].Write((ushort)((ushort)(args[0].Read(false) << 8) | (ushort)(args[0].Read(false) >> 8)), Instructions.BSWP, false);
            mem.FZERO = args[0].Read() == 0;
            return false;
        }

        /// <summary>
        /// Moves the instruction pointer into the first argument.
        /// </summary>
        private bool CIP(List<IArgument> args)
        {
            args[0].Write(mem.ISPTR, Instructions.CIP);
            return false;
        }

        /// <summary>
        /// Rotates the first argument right by the second argument's length.
        /// </summary>
        private bool RTR(List<IArgument> args)
        {
            args[0].Write(args[0].Read(false).RotateRight(args[1].Read()), Instructions.RTR, false);
            mem.FZERO = args[0].Read() == 0;
            return false;
        }

        /// <summary>
        /// Rotates the first argument left by the second argument's length.
        /// </summary>
        private bool RTL(List<IArgument> args)
        {
            args[0].Write(args[0].Read(false).RotateLeft(args[1].Read()), Instructions.RTL, false);
            mem.FZERO = args[0].Read() == 0;
            return false;
        }

        /// <summary>
        /// Returns zero from a function.
        /// </summary>
        private bool RTRV()
        {
            mem.RTRGT = 0x0;
            return RTRN();
        }

        /// <summary>
        /// Returns a value from a function (value is stored in RTRGT).
        /// </summary>
        private bool RTRN()
        {
            //Set FZERO.
            mem.FZERO = mem.RTRGT == 0;

            //1. Move RSR into RSK.
            mem.RSK = mem.RSR;

            //2. Pop RX off the stack.
            var valueArg = new ValueArgument(0x0);
            POP(new List<IArgument>() { valueArg });
            ushort offset = valueArg.Value;

            //3. Set the instruction pointer to the value of RCALL plus the value of RX.
            mem.ISPTR = (ushort)(mem.RCALL + offset);

            //4. Pop RCALL, RSTAT, RX, RY and RZ off the stack, in reverse order.
            for (int i = 0; i < 5; i++)
            {
                POP(new List<IArgument>() { valueArg });
            }

            //5. Push RTRGT onto the stack.
            PUSH(new List<IArgument>() { new RegisterArgument((ushort)ArgRegisterType.RTRGT, mem, 0x0) });

            //6. Clear RTRGT.
            mem.RTRGT = 0x0;
            return false;
        }

        /// <summary>
        /// Calls a function pointed at by RTRGT.
        /// </summary>
        private bool CALL(List<IArgument> args, ushort iptr)
        {
            //Is 0x0 being called? If so, ignore.
            if (mem.RTRGT == 0x0) { return false; }

            //1. Push RCALL, RSTAT, RX, RY and RZ onto the stack, in order.
            PUSH(new List<IArgument>() { new RegisterArgument((ushort)ArgRegisterType.RCALL, mem, 0x0) });
            PUSH(new List<IArgument>() { new RegisterArgument((ushort)ArgRegisterType.RSTAT, mem, 0x0) });
            PUSH(new List<IArgument>() { new RegisterArgument((ushort)ArgRegisterType.RX, mem, 0x0) });
            PUSH(new List<IArgument>() { new RegisterArgument((ushort)ArgRegisterType.RY, mem, 0x0) });
            PUSH(new List<IArgument>() { new RegisterArgument((ushort)ArgRegisterType.RZ, mem, 0x0) });

            //2. Push the length of this [CALL] instruction onto the stack.
            //Calculated by (last argument address + 1) - baseAddr.
            ushort callLen = (ushort)(args.GetPointerToAfter(iptr) - iptr);
            PUSH(new List<IArgument>() { new ValueArgument(callLen.Reverse()) });

            //3. Move RSK into RSR.
            mem.RSR = mem.RSK;

            //4. Move the function's parameters into RX, RY and RZ respectively.
            mem.RX = args[0].Read();
            mem.RY = args[1].Read();
            mem.RZ = args[2].Read();

            //5. Move the address this [CALL] instruction was executed from into RCALL.
            mem.RCALL = iptr;

            //6. Set the instruction pointer to the value of RTRGT.
            mem.ISPTR = mem.RTRGT;

            //7. Clear RTRGT and RSTAT.
            mem.RTRGT = 0x0;
            mem.RSTAT = 0x0;
            return false;
        }

        /// <summary>
        /// Pops a value off the stack into the first argument.
        /// </summary>
        private bool POP(List<IArgument> args)
        {
            //If RSK is maximum (stack empty) return empty (0x0).
            if (mem.RSK == 0xFFFF)
            {
                mem.FSE = true;
                mem.FZERO = true;
                args[0].Write(0x0, Instructions.POP);
                return false;
            }

            //Read from the stack.
            args[0].Write(mem.SSK[mem.RSK], Instructions.POP);

            //Increase stack pointer.
            mem.RSK++;

            //Set stack empty (FSE) and FZERO.
            if (mem.RSK == 0xFFFF) { mem.FSE = true; }
            mem.FZERO = args[0].Read() == 0x0;
            return false;
        }

        /// <summary>
        /// Pushes the first argument to the stack.
        /// </summary>
        private bool PUSH(List<IArgument> args)
        {
            //If RSK is zero (stack full) don't do anything.
            if (mem.RSK == 0x0) 
            {
                mem.FSF = true;
                return false; 
            }

            //Decrement RSK.
            mem.RSK--;

            //Write to SSK with the argument.
            mem.SSK[mem.RSK] = args[0].Read();

            //Set stack full (FSF) and FZERO.
            if (mem.RSK == 0x0) { mem.FSF = true; }
            mem.FZERO = args[0].Read() == 0;
            return false;
        }

        /// <summary>
        /// Shifts arg1 right by arg2.
        /// </summary>
        private bool RSHF(List<IArgument> args, bool isSigned)
        {
            ushort toWrite = (ushort)(args[0].Read() >> args[1].Read());

            //If signed and leftmost bit is 1, fill to left with "1" (default is zero)
            if (isSigned && (args[0].Read() & 0x8000) == 0x8000)
            {
                ushort bitMask = 0x8000;
                for (int i=0; i<16 && i<args[1].Read(); i++)
                {
                    toWrite |= bitMask;
                    bitMask = (ushort)(bitMask >> 1);
                }
            }

            //Write value.
            args[0].Write(toWrite, Instructions.RSHF);

            //Set FZERO.
            mem.FZERO = args[0].Read() == 0;
            return false;
        }

        /// <summary>
        /// Shifts arg1 left by arg2.
        /// </summary>
        private bool LSHF(List<IArgument> args)
        {
            args[0].Write((ushort)(args[0].Read() << args[1].Read()), Instructions.LSHF);

            //Set FZERO.
            mem.FZERO = args[0].Read() == 0;
            return false;
        }

        /// <summary>
        /// Bitwise CMPL arg1 onto arg1.
        /// </summary>
        private bool CMPL(List<IArgument> args)
        {
            args[0].Write((ushort)~args[0].Read(), Instructions.CMPL);

            //Set FZERO.
            mem.FZERO = args[0].Read() == 0;
            return false;
        }

        /// <summary>
        /// Bitwise ORs the second argument onto the first.
        /// </summary>
        private bool OR(List<IArgument> args)
        {
            args[0].Write((ushort)(args[0].Read() | args[1].Read()), Instructions.AND);

            //Set FZERO.
            mem.FZERO = args[0].Read() == 0;
            return false;
        }

        /// <summary>
        /// Bitwise ANDs the second argument onto the first.
        /// </summary>
        private bool AND(List<IArgument> args)
        {
            args[0].Write((ushort)(args[0].Read() & args[1].Read()), Instructions.AND);

            //Set FZERO.
            mem.FZERO = args[0].Read() == 0;
            return false;
        }

        /// <summary>
        /// Compares arg1 to arg2, and sets flags accordingly.
        /// </summary>
        private bool CMP(List<IArgument> args)
        {
            //Set FZERO.
            mem.FZERO = (args[0].Read() == 0) || (args[1].Read() == 0);

            //Set FEQUL.
            mem.FEQUL = args[0].Read() == args[1].Read();

            //Set FLT and FGT.
            mem.FLT = args[0].Read() < args[1].Read();
            mem.FGT = args[0].Read() > args[1].Read();
            return false;
        }

        /// <summary>
        /// Copies immediate value arg1 into arg2.
        /// </summary>
        private bool ICPY(List<IArgument> args)
        {
            //Check the argument type is valid.
            if (!(args[0] is MemoryArgument))
            {
                Log.Fatal("Invalid immediate passed to ICPY (register argument instead of memory argument) at SCODE location " + args[0].ArgumentSCODELocation + ".");
                return true;
            }

            //Get the immediate value.
            var imm = args[0] as MemoryArgument;
            ushort immediate = imm.Location;

            //Copy into arg2.
            args[1].Write(immediate, Instructions.ICPY, false);

            //Set FZERO.
            mem.FZERO = args[1].Read() == 0;
            return false;
        }

        /// <summary>
        /// Copies arg1 bitwise & arg3 into arg2.
        /// </summary>
        private bool MCPY(List<IArgument> args)
        {
            args[1].Write((ushort)(args[0].Read() & args[2].Read()), Instructions.MCPY);

            //Set FZERO.
            mem.FZERO = args[1].Read() == 0;
            return false;
        }

        /// <summary>
        /// Copies arg1 into arg2.
        /// </summary>
        private bool CPY(List<IArgument> args)
        {
            args[1].Write(args[0].Read(), Instructions.CPY);

            //Set FZERO.
            mem.FZERO = args[0].Read() == 0;
            return false;
        }

        /// <summary>
        /// Writes a single character from memory to STDOUT.
        /// </summary>
        private bool WRIT(List<IArgument> args)
        {
            byte[] charBytes = BitConverter.GetBytes((ushort)(args[0].Read() & 0x0F));
            string toPrint = Encoding.GetEncoding("ISO-8859-1").GetString(charBytes);

            //Write to STDOUT.
            Console.Write(toPrint);
            return false;
        }

        /// <summary>
        /// Reads a single character from STDIN into memory.
        /// </summary>
        private bool READ(List<IArgument> args)
        {
            char nextChar = Console.ReadKey().KeyChar;

            //Set FZERO based on encoding.
            if (!nextChar.IsValidISO())
            {
                mem.FZERO = true;
                return false;
            }
            
            //Write to memory.
            ushort toWrite = BitConverter.ToUInt16(BitConverter.GetBytes(nextChar));
            args[0].Write(nextChar, Instructions.READ);
            return false;
        }

        /// <summary>
        /// Subtracts first and second argument + fcarry flag.
        /// </summary>
        private bool SUBC(List<IArgument> args, bool isSigned)
        {
            ushort initialVal = args[0].Read();
            ushort toWrite;

            //Get the value to write.
            if (!isSigned)
            {
                toWrite = (ushort)(args[0].Read() - args[1].Read());
            }
            else
            {
                toWrite = (ushort)((short)args[0].Read() - (short)args[1].Read());
            }
            mem.FCRRY = (initialVal & 0x8000) == (args[0].Read() & 0x8000);

            //Increment w/ FCRRY.
            if (mem.FCRRY) { toWrite++; }
            args[0].Write(toWrite, Instructions.ADDC);

            //Set FZERO and FCRRY
            mem.FZERO = args[0].Read() == 0;
            mem.FCRRY = (initialVal & 0x8000) == (args[0].Read() & 0x8000);
            return false;
        }

        /// <summary>
        /// Adds or subtracts first and second argument + fcarry flag.
        /// </summary>
        private bool ADDC(List<IArgument> args, bool isSigned)
        {
            ushort initialVal = args[0].Read();
            ushort toWrite;

            //Get the value to write.
            if (!isSigned)
            {
                toWrite = (ushort)(args[0].Read() + args[1].Read());
            }
            else
            {
                toWrite = (ushort)((short)args[0].Read() + (short)args[1].Read());
            }

            //Increment w/ FCRRY.
            if (mem.FCRRY) { toWrite++; }
            args[0].Write(toWrite, Instructions.ADDC);

            //Set FZERO and FCRRY
            mem.FZERO = args[0].Read() == 0;
            mem.FCRRY = (initialVal & 0x8000) == (args[0].Read() & 0x8000);
            return false;
        }

        /// <summary>
        /// Divides first argument by the second.
        /// </summary>
        private bool DIV(List<IArgument> args, bool isSigned)
        {
            //Divisor is zero?
            if (args[1].Read() == 0)
            {
                //FINF.
                mem.FZERO = true;
                mem.FINF = true;
                args[0].Write(0, Instructions.DIV);
                return false;
            }

            //Non-zero divisor, actually do the operation.
            if (isSigned)
            {
                args[0].Write((ushort)((short)args[0].Read() / (short)args[1].Read()), Instructions.DIV);
            }
            else
            {
                args[0].Write((ushort)(args[0].Read() / args[1].Read()), Instructions.DIV);
            }

            //Set FZERO flag.
            mem.FZERO = args[0].Read() == 0;
            return false;
        }

        /// <summary>
        /// Multiplies first arg by second.
        /// </summary>
        private bool MUL(List<IArgument> args, bool isSigned)
        {
            if (isSigned)
            {
                args[0].Write((ushort)((short)args[0].Read() * (short)args[1].Read()), Instructions.MUL);
            }
            else
            {
                args[0].Write((ushort)(args[0].Read() * args[1].Read()), Instructions.MUL);
            }

            //Set FZERO flag.
            mem.FZERO = args[0].Read() == 0;
            return false;
        }

        /// <summary>
        /// Increments arg1.
        /// </summary>
        private bool INC(List<IArgument> args, bool isSigned)
        {
            if (isSigned)
            {
                args[0].Write((ushort)((short)(args[0].Read()) + 1), Instructions.INC);
            }
            else
            {
                args[0].Write((ushort)(args[0].Read() + 1), Instructions.INC);
            }

            mem.FEQUL = (args[0].Read() == 0);
            return false;
        }

        /// <summary>
        /// Decrements arg1.
        /// </summary>
        private bool DEC(List<IArgument> args, bool isSigned)
        {
            if (isSigned)
            {
                args[0].Write((ushort)((short)(args[0].Read()) - 1), Instructions.INC);
            }
            else
            {
                args[0].Write((ushort)(args[0].Read() - 1), Instructions.INC);
            }

            mem.FEQUL = (args[0].Read() == 0);
            return false;
        }

        /// <summary>
        /// Adds or subtracts arg2 to/from arg1.
        /// </summary>
        private bool ADDSUB(List<IArgument> args, bool signed, bool isAdd)
        {
            ushort initial = args[0].Read();
            ushort valToWrite;

            //Make the value to write.
            if (signed)
            {
                if (isAdd)
                {
                    //ADD
                    valToWrite = (ushort)((short)args[0].Read() + (short)args[1].Read());
                }
                else
                {
                    //SUB
                    valToWrite = (ushort)((short)args[0].Read() - (short)args[1].Read());
                }


            }
            else
            {
                //Non-signed.
                if (isAdd)
                {
                    //ADD
                    valToWrite = (ushort)(args[0].Read() + args[1].Read());
                }
                else
                {
                    //SUB
                    valToWrite = (ushort)(args[0].Read() - args[1].Read());
                }
            }

            //Write the value.
            if (isAdd)
                args[0].Write(valToWrite, Instructions.ADD);
            else
                args[0].Write(valToWrite, Instructions.SUB);

            //Set carry & zero flag.
            mem.FEQUL = (args[0].Read() == 0);
            mem.FCRRY = (initial & 0x8000) != (args[0].Read() & 0x8000);
            return false;
        }
    }
}
