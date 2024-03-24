// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace LibreLancer.Thorn.Bytecode
{
	static class Undump
	{
		public const int IDCHUNK = 27;
		public const string SIGNATURE = "Lua";
		public const int VERSION = 0x32;
		public const int VERSION0 = 0x32;
		public const double TESTNUMBER = 3.14159265358979323846E8;

		static byte ReadByte (Stream stream)
		{
			int i = stream.ReadByte ();
			if (i == -1)
				throw new EndOfStreamException ();
			else
				return (byte)i;
		}

		static ushort LoadWord (Stream stream)
		{
			ushort hi = (ushort)ReadByte (stream);
			ushort lo = (ushort)ReadByte (stream);
			return (ushort)((hi << 8) | lo);
		}

		static uint LoadLong (Stream stream)
		{
			ushort hi = (ushort)LoadWord (stream);
			ushort lo = (ushort)LoadWord (stream);
			return (uint)((hi << 16) | lo);
		}

		static int LoadInt (Stream stream)
		{
			return (int)LoadLong (stream);
		}

		static byte[] ReadBytes (Stream stream, int length)
		{
			byte[] buf = new byte[length];
			stream.Read (buf, 0, length);
			return buf;
		}

		static double LoadNumber (Stream stream, bool native)
		{
			if (native) {
				return BitConverter.ToDouble (ReadBytes (stream, 8), 0);
			} else {
				int size = ReadByte (stream);
				return double.Parse (Encoding.ASCII.GetString (ReadBytes (stream, size)), CultureInfo.InvariantCulture);
			}
		}

		static string LoadTString (Stream stream)
		{
			var len = LoadInt (stream);
			if (len != 0)
				return Encoding.ASCII.GetString (ReadBytes (stream, len)).TrimEnd ('\0');
			else
				return null;
		}

		public static bool Load (Stream stream, out LuaPrototype result)
		{
			int c = stream.ReadByte ();
			if (c == IDCHUNK) {
				result = LoadChunk (stream);
				return result != null;
			} else {
				result = null;
				return false;
			}
		}

		static LuaPrototype LoadChunk (Stream stream)
		{
			if (LoadSignature (stream)) {
				return LoadFunction (stream, LoadHeader (stream));
			} else
				return null;
		}

		static byte[] LoadCode (Stream stream)
		{
			int size = LoadInt (stream);
			var bytes = ReadBytes (stream, size);
			return bytes;
		}

		static void LoadLocals (Stream stream, LuaPrototype proto)
		{
			int n = LoadInt (stream);
			if (n == 0)
				return;
			proto.Locals = new LuaLocal[n + 1];
			for (int i = 0; i < n; i++) {
				proto.Locals [i].Line = LoadInt (stream);
				proto.Locals [i].Name = LoadTString (stream);
			}
			proto.Locals [n].Line = -1;
			proto.Locals [n].Name = null;
		}

		static void LoadConstants (Stream stream, LuaPrototype proto, bool native)
		{
			int n = LoadInt (stream);
			proto.Constants = new LuaObject[n];
			for (int i = 0; i < n; i++) {
				var o = new LuaObject ();
				int t = -((int)ReadByte (stream));
				o.Type = (LuaTypes)t;
				switch (o.Type) {
				case LuaTypes.Number:
					o.Value = (float)LoadNumber (stream, native);
					break;
				case LuaTypes.String:
					o.Value = LoadTString (stream);
					break;
				case LuaTypes.Proto:
					o.Value = LoadFunction (stream, native);
					break;
				case LuaTypes.Nil:
					break;
				default:
					throw new Exception ("Constant can't be type " + o.Type.ToString ());
				}
				proto.Constants [i] = o;
			}
		}

		static LuaPrototype LoadFunction (Stream stream, bool native)
		{
			var proto = new LuaPrototype ();
			proto.LinesDefined = LoadInt (stream);
			proto.Source = LoadTString (stream);
			if (proto.Source == null)
				proto.Source = "";
			proto.Code = LoadCode (stream);
			LoadLocals (stream, proto);
			LoadConstants (stream, proto, native);
			return proto;
		}

		static bool LoadSignature (Stream stream)
		{
			byte[] buffer = new byte[3];
			stream.Read (buffer, 0, 3);
			if (buffer [0] != (byte)SIGNATURE [0] || buffer [1] != (byte)SIGNATURE [1] || buffer [2] != (byte)SIGNATURE [2])
				return false;
			return true;
		}

		static bool LoadHeader (Stream stream)
		{
			int version;
			int sizeofR;
			bool native;
			version = ReadByte (stream);
			if (version > VERSION)
				throw new Exception ("Lua too new at version " + version.ToString ("X"));
			if (version < VERSION0)
				throw new Exception ("Lua too old at version " + version.ToString ("X"));
			sizeofR = ReadByte (stream);
			native = sizeofR != 0;
			if (native) {
				var testnumber = LoadNumber (stream, native);
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (testnumber != TESTNUMBER)
					throw new Exception ("Bad number format");
			}
			return native;
		}
	}
}

