using System;
using SharpDX;
using SharpDX.DirectWrite;

namespace LibreLancer.Graphics.Text.DirectWrite
{
    /// <summary>
    /// This FontFileStream implem is reading data from a <see cref="DataStream"/>.
    /// </summary>
    class MemoryFontStream : CallbackBase, FontFileStream
    {
        private readonly DataStream _stream;
        public MemoryFontStream(DataStream stream)
        {
            this._stream = stream;
        }

        void FontFileStream.ReadFileFragment(out IntPtr fragmentStart, long fileOffset, long fragmentSize, out IntPtr fragmentContext)
        {
            lock (this)
            {
                fragmentContext = IntPtr.Zero;
                _stream.Position = fileOffset;
                fragmentStart = _stream.PositionPointer;
            }
        }

        void FontFileStream.ReleaseFileFragment(IntPtr fragmentContext)
        {
            // Nothing to release. No context are used
        }

        long FontFileStream.GetFileSize()
        {
            return _stream.Length;
        }
        long FontFileStream.GetLastWriteTime()
        {
            return 0;
        }
    }
}
