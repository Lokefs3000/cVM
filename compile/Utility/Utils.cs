using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace compile.Utility
{
    internal class Utils
    {
        public static int EncodeXY(ushort x, ushort y) => x << 16 | y;
        public static ushort DecodeX(int xy) => (ushort)(xy & 0xffff0000 >> 16);
        public static ushort DecodeY(int xy) => (ushort)(xy & 0x0000ffff);
    }
}
