using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LibreLancer.ContentEdit
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FrameRect
        {
            public UInt32 Index;
            public float U0;
            public float V0;
            public float U1;
            public float V1;
        }
    public static class FrameRectCalculator
    {
        
        public static List<FrameRect> GenerateFrameRects(int gridSizeX, int gridSizeY, int frameCount,uint textureIndex = 0)
        {
            var rects = new List<FrameRect>(frameCount);

            float stepU = 1.0f / gridSizeX;
            float stepV = 1.0f / gridSizeY;

            int generated = 0;

            for (int y = 0; y < gridSizeY && generated < frameCount; y++)
            {
                for (int x = 0; x < gridSizeX && generated < frameCount; x++)
                {
                    rects.Add(new FrameRect
                    {
                        Index = textureIndex,
                        U0 = x * stepU,
                        V0 = y * stepV,
                        U1 = (x + 1) * stepU,
                        V1 = (y + 1) * stepV
                    });

                    generated++;
                }
            }

            return rects;
        }
        public static byte[] GenerateFrameRects(int texWidth, int texHeight, int gridSizeX, int gridSizeY, int frameCount)
        {
            byte[] data = new byte[frameCount * 20];

            float cellU = 1f / gridSizeX;
            float cellV = 1f / gridSizeY;

            int frame = 0;

            for (int y = 0; y < gridSizeY; y++)
            {
                for (int x = 0; x < gridSizeX; x++)
                {
                    if (frame >= frameCount)
                        break;

                    float u0 = x * cellU;
                    float v0 = y * cellV;
                    float u1 = u0 + cellU;
                    float v1 = v0 + cellV;

                    WriteFrameRect(
                        data,
                        frame * 20,
                        u0, v0,
                        u1, v1
                    );

                    frame++;
                }
            }

            return data;
        }
        public static List<FrameRect> ParseFrameRects(byte[] data)
        {
            const int SIZE = 20;

            if (data == null || data.Length <= 1)
                throw new ArgumentException("No data on selected node");

            if (data.Length % SIZE != 0)
                throw new ArgumentException("Invalid or corrupt frame rect data");

            return new List<FrameRect>(MemoryMarshal.Cast<byte, FrameRect>(data).ToArray());
        }
        static void WriteFrameRect(Span<byte> buffer, int offset, float u0, float v0, float u1, float v1)
        {
            BitConverter.GetBytes(0u).CopyTo(buffer[offset..]);       // index
            BitConverter.GetBytes(u0).CopyTo(buffer[(offset + 4)..]);
            BitConverter.GetBytes(v0).CopyTo(buffer[(offset + 8)..]);
            BitConverter.GetBytes(u1).CopyTo(buffer[(offset + 12)..]);
            BitConverter.GetBytes(v1).CopyTo(buffer[(offset + 16)..]);
        }
        public static byte[] SerializeFrameRects(List<FrameRect> rects)
        {
            return UnsafeHelpers.CastArray(rects.ToArray());
        }
    }
}
