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

        public static List<FrameRect> GenerateFrameRects(int gridSizeX, int gridSizeY, int frameCount, uint textureIndex = 0)
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

        public static List<FrameRect> ParseFrameRects(byte[] data)
        {
            const int SIZE = 20;

            if (data == null || data.Length <= 1)
                throw new ArgumentException("No data on selected node");

            if (data.Length % SIZE != 0)
                throw new ArgumentException("Invalid or corrupt frame rect data");

            return new List<FrameRect>(MemoryMarshal.Cast<byte, FrameRect>(data).ToArray());
        }

        public static byte[] SerializeFrameRects(List<FrameRect> rects)
        {
            return UnsafeHelpers.CastArray(rects.ToArray());
        }
    }
}
