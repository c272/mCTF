using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mCTF
{
    /// <summary>
    /// Global constants for the program.
    /// </summary>
    public static class Constants
    {
        public static byte[] STD_MCTF_HEADER = new byte[] { 0x6D, 0x43, 0x54, 0x46 };
        public static byte[] ZCOMPRESSED_MCTF_HEADER = new byte[] { 0x6D, 0x43, 0x54, 0x5A };
        
        //Nasty parameter count lookup table.
        public static Dictionary<Instructions, int> ParameterLookup = new Dictionary<Instructions, int>()
        {
            { Instructions.HALT, 0 },
            { Instructions.NOOP, 0 },
            { Instructions.INC, 1 },
            { Instructions.DEC, 1 },
            { Instructions.ADD, 2 },
            { Instructions.SUB, 2 },
            { Instructions.MUL, 2 },
            { Instructions.DIV, 2 },
            { Instructions.ADDC, 2 },
            { Instructions.SUBC, 2 },
            { Instructions.READ, 1 },
            { Instructions.WRIT, 1 },
            { Instructions.CPY, 2 },
            { Instructions.MCPY, 3 },
            { Instructions.ICPY, 2 },
            { Instructions.CMP, 2 },
            { Instructions.AND, 2 },
            { Instructions.OR, 2 },
            { Instructions.CMPL, 1 },
            { Instructions.LSHF, 2 },
            { Instructions.RSHF, 2 },
            { Instructions.PUSH, 1 },
            { Instructions.POP, 1 },
            { Instructions.CFLG, 0 },
            { Instructions.CALL, 3 },
            { Instructions.RTRN, 0 },
            { Instructions.RTRV, 0 },
            { Instructions.RTL, 2 },
            { Instructions.RTR, 2 },
            { Instructions.CIP, 1 },
            { Instructions.BSWP, 1 },
            { Instructions.JUMP, 0 },
            { Instructions.JZRO, 0 },
            { Instructions.JEQU, 0 },
            { Instructions.JLT, 0 },
            { Instructions.JGT, 0 },
            { Instructions.JCRY, 0 },
            { Instructions.JINF, 0 },
            { Instructions.JSE, 0 },
            { Instructions.JSF, 0 },
            { Instructions.CZRO, 0 },
            { Instructions.CCRY, 0 },
            { Instructions.XOR, 2 },
            { Instructions.SWAP, 2 },
            { Instructions.RCPT, 2 },
            { Instructions.RCPF, 2 },
        };
    }

    /// <summary>
    /// A list of possible CPU instructions, with their opcodes as values.
    /// </summary>
    public enum Instructions
    {
        HALT = 0x000,
        NOOP = 0x001,
        INC = 0x002,
        DEC = 0x003,
        ADD = 0x004,
        SUB = 0x005,
        MUL = 0x006,
        DIV = 0x007,
        ADDC = 0x008,
        SUBC = 0x009,
        READ = 0x00A,
        WRIT = 0x00B,
        CPY = 0x00C,
        MCPY = 0x00D,
        ICPY = 0x00E,
        CMP = 0x00F,
        AND = 0x010,
        OR = 0x011,
        CMPL = 0x012,
        LSHF = 0x013,
        RSHF = 0x014,
        PUSH = 0x015,
        POP = 0x016,
        CFLG = 0x017,
        CALL = 0x018,
        RTRN = 0x019,
        RTRV = 0x01A,
        RTL = 0x01B,
        RTR = 0x01C,
        CIP = 0x01D,
        BSWP = 0x01E,
        JUMP = 0x01F,
        JZRO = 0x020,
        JEQU = 0x021,
        JLT = 0x022,
        JGT = 0x023,
        JCRY = 0x024,
        JINF = 0x025,
        JSE = 0x026,
        JSF = 0x027,
        CZRO = 0x030,
        CCRY = 0x034,
        XOR = 0x040,
        SWAP = 0x041,
        RCPT = 0x042,
        RCPF = 0x043
    }
}
