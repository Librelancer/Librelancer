// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using SharpDX;
using SharpDX.DirectWrite;

namespace LibreLancer.Graphics.Text.DirectWrite
{
    class CustomFontFileEnumerator : CallbackBase, FontFileEnumerator
    {
        private Factory _factory;
        private FontFileLoader _loader;
        private DataStream keyStream;
        private FontFile _currentFontFile;

        public CustomFontFileEnumerator(Factory factory, FontFileLoader loader, DataPointer key)
        {
            _factory = factory;
            _loader = loader;
            keyStream = new DataStream(key.Pointer, key.Size, true, false);
        }

        bool FontFileEnumerator.MoveNext()
        {
            bool moveNext = keyStream.RemainingLength != 0;
            if (moveNext)
            {
                if (_currentFontFile != null)
                    _currentFontFile.Dispose();

                _currentFontFile = new FontFile(_factory, keyStream.PositionPointer, 4, _loader);
                keyStream.Position += 4;
            }
            return moveNext;
        }

        FontFile FontFileEnumerator.CurrentFontFile
        {
            get
            {
                ((IUnknown)_currentFontFile).AddReference();
                return _currentFontFile;
            }
        }
    }
}
