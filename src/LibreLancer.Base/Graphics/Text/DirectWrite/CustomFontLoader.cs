using System.Collections.Generic;
using System.IO;
using LibreLancer.Platforms;
using SharpDX;
using SharpDX.DirectWrite;

namespace LibreLancer.Graphics.Text.DirectWrite
{
    public class CustomFontLoader : CallbackBase, FontCollectionLoader, FontFileLoader
    {
        private readonly List<MemoryFontStream> _fontStreams = new List<MemoryFontStream>();
        private readonly List<CustomFontFileEnumerator> _enumerators = new List<CustomFontFileEnumerator>();
        private readonly DataStream _keyStream;
        private readonly Factory _factory;
        public bool Valid = true;
        public CustomFontLoader(Factory factory)
        {
            _factory = factory;
            foreach(var bytes in ((Win32Platform)Platform.RunningPlatform).TtfFiles)
            {
                var stream = new DataStream(bytes.Length, true, true);
                stream.Write(bytes, 0, bytes.Length);
                stream.Position = 0;
                _fontStreams.Add(new MemoryFontStream(stream));
            }
            Platform.FontLoaded += Platform_FontLoaded;
            _keyStream = new DataStream(sizeof(int) * _fontStreams.Count, true, true);
            for (int i = 0; i < _fontStreams.Count; i++)
                _keyStream.Write((int)i);
            _keyStream.Position = 0;
            _factory.RegisterFontFileLoader(this);
            _factory.RegisterFontCollectionLoader(this);
        }

        private void Platform_FontLoaded()
        {
            Valid = false;
        }

        public DataStream Key
        {
            get
            {
                return _keyStream;
            }
        }

        FontFileEnumerator FontCollectionLoader.CreateEnumeratorFromKey(Factory factory, DataPointer collectionKey)
        {
            var enumerator = new CustomFontFileEnumerator(factory, this, collectionKey);
            _enumerators.Add(enumerator);
            return enumerator;
        }

        FontFileStream FontFileLoader.CreateStreamFromKey(DataPointer fontFileReferenceKey)
        {
            var index = Utilities.Read<int>(fontFileReferenceKey.Pointer);
            _fontStreams[index].AddReference();
            return _fontStreams[index];
        }
    }
}
