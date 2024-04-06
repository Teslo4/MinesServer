using Azure;
using Microsoft.EntityFrameworkCore.Query.Internal;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MinesServer
{
    public static class M3Compressor
    {
        public static byte[] Compress(Bitmap image)
        {
            Span<byte> temp = stackalloc byte[2 * 2 + 10 + (image.Width * image.Height) * 4];
            var width = Convert.ToUInt16(image.Width);
            var height = Convert.ToUInt16(image.Height);
            MemoryMarshal.Write(temp[0..2], in width);
            MemoryMarshal.Write(temp[2..4], in width);
            byte op = 4;
            for (int i = 0;i<10;i++)
            {
                
                MemoryMarshal.Write(temp[(4+i)..], in op);
                op = 0;
            }
            for(int y = 0;y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var p = image.GetPixel(x, y);
                    byte r = p.R, g = p.G, b = p.B, a = p.A;
                    var t = (14 + (4 * (y * image.Width + x)));
                    MemoryMarshal.Write(temp[t..], in r);
                    t++;
                    MemoryMarshal.Write(temp[t..], in g);
                    t++;
                    MemoryMarshal.Write(temp[t..], in b);
                    t++;
                    MemoryMarshal.Write(temp[t..], in a);
                }
            }
            return temp.ToArray();
        }
    }
}
