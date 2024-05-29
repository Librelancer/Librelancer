using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;
using LibreLancer.Thorn.Bytecode;
using LibreLancer.Thorn.VM;

namespace LibreLancer.Thorn
{
	/// <summary>
	/// A class representing a Thorn (Lua 3.2) table.
	/// </summary>
	public class ThornTable
    {
        public static Dictionary<string, string> EnumReverse = new Dictionary<string, string>();

		private const int ARRAY_PART_THRESHOLD = 5;

		private readonly LinkedList<ThornTablePair> valueList = new LinkedList<ThornTablePair>();
		readonly Dictionary<object, LinkedListNode<ThornTablePair>> valueMap = new Dictionary<object, LinkedListNode<ThornTablePair>>();
		private object[] arrayPart = null;
		int arrayLength = 0;

        private bool containsNilEntries = false;

		/// <summary>
		/// Removes all items from the Table.
		/// </summary>
		public void Clear()
		{
			valueList.Clear();
			valueMap.Clear();
			arrayLength = 0;
			arrayPart = null;
		}

		/// <summary>
		/// Gets the integral key from a double.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool TryGetIntegralKey(object dv, out int k)
		{
            if (dv is string) {
                k = -1;
                return false;
            }
            if (!Conversion.TryGetNumber(dv, out var num))
            {
                k = -1;
                return false;
            }
			k = (int)num;
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (num == k)
				return true;
			return false;
		}

		/// <summary>
		/// Gets or sets the <see cref="System.Object"/> with the specified key(s).
		/// This will marshall CLR and WattleScript objects in the best possible way.
		/// </summary>
		/// <value>
		/// The <see cref="System.Object"/>.
		/// </value>
		/// <param name="key">The key.</param>
		/// <returns></returns>
		public object this[object key]
		{
			get
            {
                return Get(key);
            }
			set
            {
                Set(key, value);
            }
		}

        public void Append(object value)
        {
            PerformTableSet(Length + 1, value);
        }

		#region Set

		LinkedListNode<ThornTablePair> MapFind(object key)
		{
			valueMap.TryGetValue(key, out var pair);
			return pair;
		}

		internal void MapAdd(object key, object value)
		{
			var node = valueList.AddLast(new ThornTablePair(key, value));
			valueMap.Add(key, node);
		}

		ThornTablePair MapSet(object key, object value)
		{
			LinkedListNode<ThornTablePair> node = MapFind(key);

			if (node == null)
			{
				MapAdd(key, value);
				return default;
			}
			else
			{
				ThornTablePair val = node.Value;
				node.Value = new ThornTablePair(key, value);
				return val;
			}
		}

		bool KeyInArray(int ik)
		{
			return (ik >= 0 && ik <= (arrayPart?.Length ?? 0) + ARRAY_PART_THRESHOLD);
		}

		int GetNewLength(int minLength)
		{
			int i = arrayPart == null ? ARRAY_PART_THRESHOLD : arrayPart.Length * 2;
			while (valueMap.TryGetValue((object)(i), out var node) && node.Value.Value != null)
				i++;
			if (i < minLength) return minLength;
			return i;
		}

		void MapToArray(int oldLength)
		{
			if (valueMap.Count == 0) return; //Pure array
			for (int i = oldLength; i < arrayPart.Length; i++)
			{
				var k = (object)(i);
				if (valueMap.TryGetValue(k, out var node)) {
					arrayPart[i] = node.Value.Value;
					valueList.Remove(node);
					valueMap.Remove(k);
					if (arrayLength < i && arrayPart[i] != null) arrayLength = i + 1;
				}
			}
		}

		private void PerformTableSet(object key, object value)
		{
			if (TryGetIntegralKey(key, out var ik) &&
			    KeyInArray(ik))
			{
				if (arrayPart == null) {
					arrayPart = new object[GetNewLength(ik + 1)];
					MapToArray(0);
				}
				else if (arrayPart.Length <= ik) {
					int oldLength = arrayPart.Length;
					Array.Resize(ref arrayPart, GetNewLength(ik + 1));
					MapToArray(oldLength);
				}

				arrayPart[ik] = value;
				if (value == null && arrayLength > ik)
					arrayLength = ik;
				else if (value != null && arrayLength <= ik)
				{
					//Scan to find new array length
					for(; ik < arrayPart.Length; ik++) {
						if (arrayPart[ik] == null) break;
					}
					arrayLength = ik;
				}
			}
			else
			{
				ThornTablePair prev = MapSet(key, value);
				// If this is an insert, we can invalidate all iterators and collect dead keys
				if (containsNilEntries && value != null && prev.Value == null)
				{
					CollectDeadKeys();
				}
				// If this value is nil (and we didn't collect), set that there are nil entries
				if (value == null)
					containsNilEntries = true;
			}
		}

		/// <summary>
		/// Sets the value associated to the specified key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <param name="value">The value.</param>
		public void Set(object key, object value)
		{
			PerformTableSet(key, value);
		}


		#endregion

        public object Get(object key)
        {
            if (TryGetIntegralKey(key, out int ik) && KeyInArray(ik))
            {
                if (arrayPart == null || arrayPart.Length <= ik) return null;
                return arrayPart[ik];
            }
            var node = MapFind(key);
            return (node != null) ? node.Value.Value : null;
        }


		bool MapRemove(object key)
		{
			LinkedListNode<ThornTablePair> node = MapFind(key);
			if (node != null)
			{
				valueList.Remove(node);
				return valueMap.Remove(key);
			}
			return false;
		}

		private bool PerformTableRemove(object key)
		{
			if (TryGetIntegralKey(key, out int ik) && KeyInArray(ik))
			{
				if (arrayPart == null || ik >= arrayPart.Length) return false;
				var retval = arrayPart[ik] != null;
                arrayPart[ik] = null;
				if (ik < arrayLength) arrayLength = ik;
				return retval;
			}
			return MapRemove(key);
		}


		/// <summary>
		/// Remove the value associated with the specified key from the table.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns><c>true</c> if values was successfully removed; otherwise, <c>false</c>.</returns>
		public bool Remove(object key) => PerformTableRemove(key);


		/// <summary>
		/// Collects the dead keys. This frees up memory but invalidates pending iterators.
		/// It's called automatically internally when the semantics of Lua tables allow, but can be forced
		/// externally if it's known that no iterators are pending.
		/// </summary>
		public void CollectDeadKeys()
		{
			for (LinkedListNode<ThornTablePair> node = valueList.First; node != null; node = node.Next)
			{
				if (node.Value.Value == null)
				{
					Remove(node.Value.Key);
				}
			}

			containsNilEntries = false;
		}


		/// <summary>
		/// Returns the next pair from a value
		/// </summary>

		ThornTablePair? FirstMapNode()
		{
			LinkedListNode<ThornTablePair> node = valueList.First;
            if (node == null)
                return null;
            else
            {
                if (node.Value.Value == null)
                    return NextKey(node.Value.Key);
                else
                    return node.Value;
            }
        }
		public ThornTablePair? NextKey(object v)
		{
			if (v == null)
			{
				if (arrayPart != null)
				{
					for (int i = 0; i < arrayPart.Length; i++)
					{
						if (arrayPart[i] != null)
							return new ThornTablePair((object)(i), arrayPart[i]);
					}
				}
				return FirstMapNode();
			}

			if (arrayPart != null && TryGetIntegralKey(v, out int ik) &&
			    KeyInArray(ik))
			{
				//Invalid array index
				if (ik >= arrayPart.Length || arrayPart[ik] == null)
					return null;
				//Search
				for (int i = ik + 1; i < arrayPart.Length; i++)
				{
					if (arrayPart[i] != null)
						return new ThornTablePair((float)(i), arrayPart[i]);
				}
				return FirstMapNode();
			}

			return GetNextOf(MapFind(v));
		}

		private ThornTablePair? GetNextOf(LinkedListNode<ThornTablePair> linkedListNode)
		{
			while (true)
			{
				if (linkedListNode == null)
					return null;

                if (linkedListNode.Next == null)
                    return default(ThornTablePair);

				linkedListNode = linkedListNode.Next;

				if (linkedListNode.Value.Value != null)
					return linkedListNode.Value;
			}
		}

        internal void SetMap(Dictionary<string, object> kv)
        {
            foreach(var pair in kv)
                Set(pair.Key, pair.Value);
        }

        internal void SetArray(int offset, object[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                Set(offset + 1 + i, values[i]);
            }
        }
		/// <summary>
		/// Gets the length of the "array part".
		/// </summary>
		public int Length => arrayLength - 1 < 0 ? 0 : arrayLength - 1;


		IEnumerable<ThornTablePair> IteratePairs()
		{
			if (arrayPart != null) {
				for (int i = 0; i < arrayPart.Length; i++)
				{
					if (arrayPart[i] != null)
						yield return new ThornTablePair((object)(i), arrayPart[i]);
				}
			}
			foreach (var x in valueList)
				yield return x;
		}

		/// <summary>
		/// Enumerates the key/value pairs.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<ThornTablePair> Pairs => IteratePairs();


		IEnumerable<object> IterateKeys()
		{
			if (arrayPart != null) {
				for (int i = 0; i < arrayPart.Length; i++)
				{
					if (arrayPart[i] != null)
						yield return (object)(i);
				}
			}
			foreach (var x in valueList)
				yield return x.Key;
		}

		/// <summary>
		/// Enumerates the keys.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<object> Keys => IterateKeys();


		IEnumerable<object> IterateValues()
		{
			if (arrayPart != null) {
				for (int i = 0; i < arrayPart.Length; i++)
				{
					if (arrayPart[i] != null)
						yield return arrayPart[i];
				}
			}
			foreach (var x in valueList)
				yield return x.Value;
		}

		/// <summary>
		/// Enumerates the values
		/// </summary>
		/// <returns></returns>
		public IEnumerable<object> Values => IterateValues();

        //Conversions
        public bool TryGetVector3(object index, out Vector3 v)
        {
            v = Vector3.Zero;
            var itm = Get(index);
            if (itm is Vector3 v3)
            {
                v = v3;
                return true;
            }
            else if (itm is ThornTable tbl)
            {
                var a = tbl.Get(1);
                var b = tbl.Get(2);
                var c = tbl.Get(3);
                if (a is float x && b is float y && c is float z)
                {
                    v = new Vector3(x, y, z);
                    return true;
                }
            }
            return false;
        }

        public Vector3 ToVector3()
        {
            var a = Get(1);
            var b = Get(2);
            var c = Get(3);
            if (a is float x && b is float y && c is float z)
            {
                return new Vector3(x, y, z);
            }
            throw new InvalidCastException();
        }


        public bool ContainsKey(string key) => Get(key) != null;

        public bool TryGetValue(string key, out object v)
        {
            v = Get(key);
            return v != null;
        }

        public bool TryGetValue(int key, out object v)
        {
            v = Get(key);
            return v != null;
        }

        static StringBuilder Tab(StringBuilder builder, int tabCount)
        {
            for (int i = 0; i < tabCount; i++)
                builder.Append("  ");
            return builder;
        }

        static string Rev(string s)
        {
            string tmp;
            if (EnumReverse.TryGetValue(s, out tmp)) return tmp;
            return s;
        }
        string DumpEnum(object o)
        {
            var t = o.GetType();
            var full = Convert.ToUInt32(o);
            foreach (var v in Enum.GetValues(t))
            {
                if (full == Convert.ToUInt32(v)) return Rev(o.ToString());
            }
            var sb = new StringBuilder();
            int count = 0;
            foreach(var fl in Enum.GetValues(t)) {
                var a = Convert.ToUInt32(fl);
                if (a == 0) continue;
                if((full & a) == a)
                {
                    if (count == 0) sb.Append(Rev(fl.ToString()));
                    else sb.Append(" + ").Append(Rev(fl.ToString()));
                    count++;
                }
            }
            return sb.ToString();
        }
        static string FNice(float f) => f.ToString("0.##########################");

        string DumpValue(object value, int tabCount, bool firstTab) => value switch
        {
            string s => JsonValue.Create(s).ToJsonString(),
            Enum => DumpEnum(value),
            ThornTable t => t.Dump(true, tabCount, firstTab),
            bool b => b ? "Y" : "N",
            float f => FNice(f),
            Vector3 v => $"{{ {FNice(v.X)}, {FNice(v.Y)}, {FNice(v.Z)} }}",
            LuaPrototype => "FUNCTION",
            ThornClosure => "FUNCTION",
            _ => value.ToString()
        };

        public string Dump(bool singleLine, int tabCount = 0, bool firstTab = true)
        {
            var builder = new StringBuilder();
            bool sameLine = valueMap.Count == 0 && singleLine;
            if (sameLine)
            {
                Tab(builder, firstTab ? tabCount : 0).Append("{ ");
                bool first = true;
                foreach(var val in IterateValues())
                {
                    if (!first)
                        builder.Append(", ");
                    first = false;
                    builder.Append(DumpValue(val, tabCount + 1, firstTab));
                }
                builder.Append(" }");
            }
            else
            {
                Tab(builder, firstTab ? tabCount : 0).AppendLine("{");
                if (valueMap.Count == 0)
                {
                    bool first = true;
                    foreach (var val in IterateValues())
                    {
                        if (!first)
                            builder.AppendLine(",");
                        first = false;
                        Tab(builder, tabCount + 1).Append(DumpValue(val, tabCount + 1, true));
                    }
                    builder.AppendLine();
                    Tab(builder, tabCount).Append("}");
                }
                else
                {
                    bool first = true;
                    foreach (var kv in Pairs)
                    {
                        if (!first)
                            builder.AppendLine(",");
                        first = false;
                        string k = kv.Key switch
                        {
                            string s => s,
                            float f => FNice(f),
                            int i => i.ToString(),
                            _ => $"[{kv.Key.GetType()}]"
                        };
                        Tab(builder, tabCount + 1).Append(k).Append(" = ").Append(DumpValue(kv.Value, tabCount + 1, false));
                    }
                    builder.AppendLine();
                    Tab(builder, tabCount).Append("}");
                }
            }
            return builder.ToString();
        }

        public override string ToString() => Dump(true);
    }
}
