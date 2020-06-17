using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace mCTF
{
    /// <summary>
    /// Represents the memory for mCTF.
    /// </summary>
    public class Memory
    {
        //General registers X, Y and Z.
        private ushort RX_ = 0x0, RY_ = 0x0, RZ_ = 0x0;

        //Target register, write only.
        private ushort RTRGT_ = 0x0;

        //Flags register, read only.
        public ushort RSTAT = 0x0;

        //Call register, read only.
        private ushort RCALL_ = 0x0;

        //Stack pointer register, can only be written/read by the PUSH/POP instruction.
        public ushort RSK = 0x0;

        //Stack return register, can only be written/read by the CALL instruction.
        public ushort RSR = 0x0;

        //Literal use registers all expect bigendian data.
        public ushort RX { get { return RX_.Reverse(); } set { RX_ = value.Reverse(); } }
        public ushort RY { get { return RY_.Reverse(); } set { RY_ = value.Reverse(); } }
        public ushort RZ { get { return RZ_.Reverse(); } set { RZ_ = value.Reverse(); } }
        public ushort RTRGT { get { return RTRGT_.Reverse(); } set { RTRGT_ = value.Reverse(); } }
        public ushort RCALL { get { return RCALL_.Reverse(); } set { RCALL_ = value.Reverse(); } }

        //Stack space. 65536 words long.
        //Only readable/writeable from PUSH/POP.
        public ushort[] SSK = new ushort[0x10000];

        //Input memory space. 65536 words long.
        public ushort[] SIN = new ushort[0x10000];

        //General purpose memory space.
        public ushort[] SMAIN = new ushort[0x10000];

        //Code memory space, hidden from the program completely.
        //No reading or writing to this from within program at all!!
        public ushort[] SCODE = new ushort[0x10000];

        //The instruction pointer.
        public ushort ISPTR_ = 0x0;
        public ushort ISPTR { get { return ISPTR_.Reverse(); } set { ISPTR_ = value.Reverse(); } }

        /// <summary>
        /// Memory setup for initial states.
        /// </summary>
        public Memory()
        {
            //Enable FSE (0x6) in RSTAT to begin with.
            RSTAT |= 0x40;

            //Set RSK to 0xFFFF (stack grows downwards).
            RSK = 0xFFFF;

            //Set the instruction pointer to 0x1.
            ISPTR = 0x1;
        }

        /// <summary>
        /// Processes a contiguous array of memory blocks from an mCTF image.
        /// MUST be uncompressed.
        /// </summary>
        public void ProcessBlocks(byte[] blocks)
        {
            Log.Memory("Beginning memory block loading...");

            //Start at zero index.
            int curIndex = 0;
            bool sinProcessed = false, sCodeProcessed = false, sMainProcessed = false;

            //Read blocks.
            while (curIndex < blocks.Length)
            {
                //Make sure this block type hasn't already been processed.
                if (sinProcessed && blocks[curIndex] == 0x1 ||
                    sCodeProcessed && blocks[curIndex] == 0x2 ||
                    sMainProcessed && blocks[curIndex] == 0x3)
                {
                    Log.Fatal("Duplicate memory block detected (" + blocks[curIndex].ToString() + ").");
                    return;
                }

                //Read the length of this block. It's little endian, so reverse first.
                if (curIndex + 2 >= blocks.Length) { Log.Fatal("Invalid block at end of sequence (no length)."); }
                byte[] blockLenBytes = new byte[] { blocks[curIndex + 1], blocks[curIndex + 2] };
                ushort blockLen = BitConverter.ToUInt16(blockLenBytes, 0);

                //Make sure the length is valid.
                if (blockLen > 65534)
                {
                    Log.Fatal("Invalid data length for block, too large.");
                }

                //Read out the data based on the block length.
                if (curIndex + 2 + blockLen*2 >= blocks.Length) { Log.Fatal("Invalid block at end of sequence (invalid length)"); }
                byte[] newData = blocks.Skip(curIndex + 3).Take((int)blockLen * 2).ToArray();

                //Copy the raw byte data into words, place into word array.
                ushort[] newWords = new ushort[newData.Length / 2];
                Buffer.BlockCopy(newData, 0, newWords, 0, newData.Length);

                //Read the "space index" (where the memory will be copied to).
                switch (blocks[curIndex])
                {
                    case 0x1:
                        Log.Memory("Block at index [" + curIndex + "] is destined for SIN.");
                        newWords.CopyTo(SIN, 1);
                        sinProcessed = true;
                        break;
                    case 0x2:
                        Log.Memory("Block at index [" + curIndex + "] is destined for SCODE.");
                        newWords.CopyTo(SCODE, 1);
                        sCodeProcessed = true;
                        break;
                    case 0x3:
                        Log.Memory("Block at index [" + curIndex + "] is destined for SMAIN.");
                        newWords.CopyTo(SMAIN, 1);
                        sMainProcessed = true;
                        break;
                    default:
                        Log.Fatal("Invalid memory block space ID: " + blocks[curIndex].ToString());
                        return;
                }

                //Advance the index.
                curIndex += 3 + (int)blockLen * 2;
            }
        }

        /// <summary>
        /// Clears the first memory location in each major memory area.
        /// </summary>
        public void ClearLocationZero()
        {
            SSK[0] = 0x0;
            SIN[0] = 0x0;
            SMAIN[0] = 0x0;
            SCODE[0] = 0x0;
        }

        //Accesses FZERO.
        public bool FZERO
        {
            get
            {
                return (RSTAT & 0x1) == 0x1;
            }

            set
            {
                if (value) { RSTAT |= 0x1; return; } //set true
                RSTAT &= (ushort.MaxValue - 1); //set false
            }
        }

        //Accesses FEQUL.
        public bool FEQUL
        {
            get
            {
                return (RSTAT & 0x2) == 0x2;
            }

            set
            {
                if (value) { RSTAT |= 0x2; return; } //set true
                RSTAT &= 0xFFFD; //set false
            }
        }

        //Accesses FLT.
        public bool FLT
        {
            get
            {
                return (RSTAT & 0x4) == 0x4;
            }

            set
            {
                if (value) { RSTAT |= 0x4; return; } //set true
                RSTAT &= 0xFFFB; //set false
            }
        }

        //Accesses FGT.
        public bool FGT
        {
            get
            {
                return (RSTAT & 0x8) == 0x8;
            }

            set
            {
                if (value) { RSTAT |= 0x8; return; } //set true
                RSTAT &= 0xFFF7; //set false
            }
        }

        //Accesses FCRRY.
        public bool FCRRY
        {
            get
            {
                return (RSTAT & 0x10) == 0x10;
            }

            set
            {
                if (value) { RSTAT |= 0x10; return; } //set true
                RSTAT &= 0xFFEF; //set false
            }
        }

        //Accesses FINF.
        public bool FINF
        {
            get
            {
                return (RSTAT & 0x20) == 0x20;
            }

            set
            {
                if (value) { RSTAT |= 0x20; return; } //set true
                RSTAT &= 0xFFDF; //set false
            }
        }

        //Accesses FSE.
        public bool FSE
        {
            get
            {
                return (RSTAT & 0x40) == 0x40;
            }

            set
            {
                if (value) { RSTAT |= 0x40; return; } //set true
                RSTAT &= 0xFFBF; //set false
            }
        }

        //Accesses FSF.
        public bool FSF
        {
            get
            {
                return (RSTAT & 0x80) == 0x80;
            }

            set
            {
                if (value) { RSTAT |= 0x80; return; } //set true
                RSTAT &= 0xFF7F; //set false
            }
        }
    }
}
