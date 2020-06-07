using System;
using System.Collections.Generic;
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
    }
}
