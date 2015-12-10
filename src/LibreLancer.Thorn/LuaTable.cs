using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
namespace LibreLancer.Thorn
{
	public class LuaTable
	{
		public int Capacity;
		bool isArray = false;
		object[] arrayStorage;
		Dictionary<object,object> mapStorage;
		public LuaTable (int capacity)
		{
			Capacity = capacity;
		}
		public LuaTable(object[] array)
		{
			Capacity = array.Length;
			SetArray (0, array);
		}
		public void SetArray(int offset, object[] stuff)
		{
			if (offset == 1 && stuff.Length == 36)
				throw new Exception ();
			isArray = true;
			if (arrayStorage == null)
				arrayStorage = new object[offset + stuff.Length];
			if (arrayStorage.Length < offset + stuff.Length)
				Array.Resize (ref arrayStorage, offset + stuff.Length);
			for (int i = 0; i < stuff.Length; i++) {
				arrayStorage [i + offset] = stuff [i];
			}
		}
		public void SetMap(Dictionary<object,object> stuff)
		{
			isArray = false;
			mapStorage = stuff;
		}
		public Vector3 ToVector3()
		{
			if (!isArray)
				throw new InvalidCastException ();
			if (arrayStorage.Length != 3)
				throw new InvalidCastException ();
			return new Vector3 ((float)this [0], (float)this [1], (float)this [2]);
		}
		public static explicit operator Vector3(LuaTable lt)
		{
			return lt.ToVector3 ();
		}
		public object this[object indexer] {
			get {
				if (isArray) {
					return arrayStorage [(int)indexer];
				} else {
					return mapStorage [indexer];
				}
			}
		}
		string ToStr(object o)
		{
			if (o is string)
				return "\"" + o.ToString () + "\"";
			return o.ToString ();
		}
		public override string ToString ()
		{
			var builder = new StringBuilder ();
			if (isArray) {
				builder.Append ("{");
				for (int i = 0; i < arrayStorage.Length; i++) {
					var item = arrayStorage [i];
					if (item == null)
						break;
					builder.Append (ToStr(item));
					builder.Append (",");
				}
				builder.Remove (builder.Length - 1, 1);
				builder.Append ("}");
			} else {
				builder.AppendLine ("{");
				foreach (var k in mapStorage.Keys) {
					builder.Append (k.ToString ()).Append (" = ").Append (ToStr(mapStorage[k])).AppendLine (",");
				}
				builder.Remove (builder.Length - 2, 2);
				builder.AppendLine ();
				builder.AppendLine ("}");
			}
			return builder.ToString ();
		}
	}
}

