using System;
using System.Linq;

namespace mCTF
{
    /// <summary>
    /// Represents a single instruction argument.
    /// </summary>
    public interface IArgument
    {
        /// <summary>
        /// Read the location of the argument.
        /// This returns BIG ENDIAN data.
        /// </summary>
        ushort Read(bool bigEndian = true, bool forceRead = false);

        /// <summary>
        /// Write to the argument location.
        /// This assumes you are writing BIG ENDIAN data.
        /// </summary>
        void Write(ushort data, Instructions cur, bool bigEndian = true);

        /// <summary>
        /// The location of this argument in SCODE.
        /// </summary>
        ushort ArgumentSCODELocation { get; set; }
    }

    /// <summary>
    /// A literal value argument that only returns a value, cannot be written to.
    /// Used for internal developer purposes, cannot be used in code.
    /// </summary>
    public class ValueArgument : IArgument
    {
        public ushort ArgumentSCODELocation { get; set; }
        public ushort Value { get; private set; }

        /// <summary>
        /// Creates a new value argument.
        /// Expects LITTLE ENDIAN data.
        /// </summary>
        public ValueArgument(ushort data, ushort scLoc)
        {
            ArgumentSCODELocation = scLoc;
            Value = data;
        }

        public ushort Read(bool bigEndian = true, bool forceRead = false)
        {
            if (bigEndian) { return Value.Reverse(); }
            return Value;
        }

        public void Write(ushort data, Instructions cur, bool bigEndian = true)
        {
            Value = data;
        }
    }

    /// <summary>
    /// Represents a single register instruction argument.
    /// </summary>
    public class MemoryArgument : IArgument
    {
        //The memory area this argument points to.
        public MemArgArea Area { get; private set; }

        //The memory location this argument points to.
        public ushort Location { get; private set; }

        //The SCODE location of this argument.
        public ushort ArgumentSCODELocation { get; set; }

        //Private memory accessor.
        private Memory mem;

        /// <summary>
        /// Constructs a memory argument given a location, area and memory
        /// manager.
        /// </summary>
        public MemoryArgument(ushort loc, MemArgArea area, Memory mem_, ushort scLoc)
        {
            Area = area;
            Location = loc;
            mem = mem_;
            ArgumentSCODELocation = scLoc;
        }

        public ushort Read(bool bigEndian = true, bool forceRead = false)
        {
            //If the location is zero, return empty byte.
            if (Location == 0x0) { return 0x0; }
            ushort retVal = 0x0;

            switch (Area)
            {
                case MemArgArea.SIN:
                    retVal = mem.SIN[Location];
                    break;
                case MemArgArea.SMAIN:
                    retVal = mem.SMAIN[Location];
                    break;
                default:
                    Log.Fatal("Unrecognized memory area for memory argument (" + Area.ToString() + ") at SCODE location " + ArgumentSCODELocation + ".");
                    return 0x0;
            }

            if (!bigEndian) { return retVal; }
            return retVal.Reverse();
        }

        public void Write(ushort data, Instructions cur, bool bigEndian = true)
        {
            //If the location is zero, don't write anything.
            if (Location == 0x0) { return; }

            //Reverse the data if big endian.
            if (bigEndian)
            {
                data = data.Reverse();
            }

            switch (Area)
            {
                case MemArgArea.SIN:
                    //No writing to SIN (it's an input area).
                    break;
                case MemArgArea.SMAIN:
                    mem.SMAIN[Location] = data;
                    break;
                default:
                    Log.Fatal("Unrecognized memory area for memory argument (" + Area.ToString() + ") at SCODE location " + ArgumentSCODELocation + ".");
                    return;
            }
        }
    }

    /// <summary>
    /// Represents the areas of memory 
    /// </summary>
    public enum MemArgArea
    {
        SIN = 0x0,
        SMAIN = 0x1
    }

    /// <summary>
    /// Represents a single register instruction argument.
    /// </summary>
    public class RegisterArgument : IArgument
    {
        public ArgRegisterType Register { get; private set; }
        public ushort ArgumentSCODELocation { get; set; }
        private Memory mem;

        /// <summary>
        /// Creates the register argument based on a register index.
        /// </summary>
        public RegisterArgument(ushort regCode, Memory mem_, ushort scLoc)
        {
            mem = mem_;
            ArgumentSCODELocation = scLoc;
            switch (regCode)
            {
                case 0x1:
                    Register = ArgRegisterType.RX;
                    break;
                case 0x2:
                    Register = ArgRegisterType.RY;
                    break;
                case 0x3:
                    Register = ArgRegisterType.RZ;
                    break;
                case 0x4:
                    Register = ArgRegisterType.RTRGT;
                    break;
                case 0x5:
                    Register = ArgRegisterType.RSTAT;
                    break;
                case 0x6:
                    Register = ArgRegisterType.RCALL;
                    break;
                default:
                    Register = ArgRegisterType.NULL;
                    break;
            }
        }

        public ushort Read(bool bigEndian = true, bool forceRead = false) 
        {
            ushort retVal = 0x0;

            //Switch on the reg, read.
            switch (Register)
            {
                case ArgRegisterType.RX:
                    retVal = mem.RX;
                    break;
                case ArgRegisterType.RY:
                    retVal = mem.RY;
                    break;
                case ArgRegisterType.RZ:
                    retVal = mem.RZ;
                    break;
                case ArgRegisterType.RTRGT:
                    if (forceRead) { retVal = mem.RTRGT; break; }
                    retVal = 0x0;
                    break;
                case ArgRegisterType.RSTAT:
                    retVal = mem.RSTAT;
                    break;
                case ArgRegisterType.RCALL:
                    retVal = mem.RCALL;
                    break;
                case ArgRegisterType.NULL:
                    retVal = 0x0;
                    break;
                default:
                    Log.Fatal("Unrecognized register argument type to read (" + Register.ToString() + ") at SCODE location " + ArgumentSCODELocation + ".");
                    return 0x0;
            }

            //Reverse the return value if big endian.
            if (bigEndian) { return retVal; }
            return retVal.Reverse();
        }

        public void Write(ushort data, Instructions cur, bool bigEndian = true) 
        {
            //Reverse the data if not big endian (all literal registers expect big endian).
            if (!bigEndian)
            {
                data = data.Reverse();
            }

            //Switch on the reg, write.
            switch (Register)
            {
                case ArgRegisterType.RX:
                    mem.RX = data;
                    break;
                case ArgRegisterType.RY:
                    mem.RY = data;
                    break;
                case ArgRegisterType.RZ:
                    mem.RZ = data;
                    break;
                case ArgRegisterType.RTRGT:
                    mem.RTRGT = data;
                    break;
                case ArgRegisterType.RSTAT:
                    if (cur == Instructions.CALL || cur == Instructions.RTRN || cur == Instructions.RTRV)
                    {
                        mem.RSTAT = data;
                    }
                    break;
                case ArgRegisterType.RCALL:
                    if (cur == Instructions.CALL)
                    {
                        mem.RCALL = data;
                    }
                    break;
                case ArgRegisterType.NULL:
                    break;
                default:
                    Log.Fatal("Unrecognized register argument type to read (" + Register.ToString() + ") at SCODE location " + ArgumentSCODELocation + ".");
                    break;
            }
        }
    }

    /// <summary>
    /// The different types of registers available to Register arguments.
    /// </summary>
    public enum ArgRegisterType
    {
        RX = 0x1,
        RY = 0x2,
        RZ = 0x3,
        RTRGT = 0x4,
        RSTAT = 0x5,
        RCALL = 0x6,
        NULL = 0x7
    }
}