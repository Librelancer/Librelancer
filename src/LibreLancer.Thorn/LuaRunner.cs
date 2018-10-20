// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LibreLancer.Thorn
{
	public class LuaRunner
	{
		public Dictionary<string,object> Env;
		public LuaRunner(Dictionary<string,object> env)
		{
			Env = env;
		}

		public Dictionary<string,object> DoFile(string filename)
		{
			using (var stream = File.OpenRead (filename)) {
				LuaPrototype p;
				if (Undump.Load(stream, out p)) {
					var runtime = new LuaBinaryRuntime (p);
					runtime.Env = Env;
					runtime.Run ();
					return runtime.Globals;
				} else {
					stream.Position = 0;
					return DoTextFile (stream);
				}
			}
		}

		public Dictionary<string, object> DoString(string str)
		{
			LuaLexer lex = new LuaLexer(
				LuaLexer.EatWhitespace,
				LuaLexer.ParseComment,
				LuaLexer.ParseFloat,
				LuaLexer.ParseStringLiteral,
				LuaLexer.ParseIdentifier,
				LuaLexer.ParseChars(
					'{', '}', ',', '=', '+', ';'
				)
			);
			IEnumerable<object> tokstream = lex.Process(str);
			Dictionary<string, object> env = new Dictionary<string, object>();
			using (IEnumerator<object> e = tokstream.GetEnumerator())
			{
				//Get first element
				e.MustMoveNext();
				LKeyValue lkv;
				while ((lkv = ParseAssignment(e)) != null)
				{
					if (e.Current.Equals(';'))
						e.MoveNext();
					env.Add(lkv.Key, lkv.Value);
				}
			}
			return env;
		}

		Dictionary<string,object> DoTextFile(Stream stream)
		{
			using (var reader = new StreamReader (stream)) {
				return DoString(reader.ReadToEnd());
			}
		}

		LKeyValue ParseAssignment (IEnumerator<object> e)
		{
			if (!(e.Current is string) && e.MoveNext () == false)
				return null;
			var ident = e.Current as string;
			if (ident == null)
				throw new Exception ("Expected identifier");
			e.MustMoveNext ();
			bool eqsign = ((char)e.Current == '=');
			if (!eqsign)
				throw new Exception ("'=' expected after identifier");
			e.MustMoveNext ();
			var val = ParseValue (e);
			return new LKeyValue(ident, val);
		}
		public class LKeyValue
		{
			public string Key;
			public object Value;
			public LKeyValue(string k, object v)
			{
				Key = k;
				Value = v;
			}
		}

		LuaTable ParseTable(IEnumerator<object> e) {
			var objs = new List<object> ();
			bool isArray = true;
			while (!e.Current.Equals ('}')) {
				if (e.Current is float) {
					objs.Add (e.Current);
					e.MustMoveNext ();
				} else if (e.Current is char) {
					e.AssertChar ('{');
					objs.Add (ParseTable (e));
				} else if (e.Current is string) {
					var ident = (string)e.Current;
					e.MustMoveNext ();
					if (e.Current.Equals ('=')) {
						e.MustMoveNext ();
						objs.Add (new LKeyValue (ident, ParseValue (e)));
						isArray = false;
					} else {
						objs.Add (Env[ident]);
					}
				} else if (e.Current is StringBuilder) {
					objs.Add (e.Current.ToString ());
					e.MustMoveNext ();
				}
				if (!e.Current.Equals ('}'))
					e.AssertChar (',');
			}
			e.MoveNext ();
			var table = new LuaTable (objs.Count);
			if (isArray) {
				table.SetArray (0, objs.ToArray ());
			} else {
				var stuff = new Dictionary<string,object> ();
				foreach (var o in objs) {
					var kv = (LKeyValue)o;
					stuff.Add (kv.Key, kv.Value);
				}
				table.SetMap (stuff);
			}
			return table;
		}

		object ParseValue(IEnumerator<object> e) {
			var obj = e.Current;
			if (e.Current is float) {
				e.MustMoveNext ();
			} else if (e.Current is char) {
				e.AssertChar ('{');
				obj = ParseTable (e);
			} else if (e.Current is string) {
				List<object> objs = new List<object> ();
				objs.Add (Env[(string)obj]);
				e.MustMoveNext ();
				while (e.Current.Equals ('+')) {
					e.MustMoveNext ();
					objs.Add (Env[(string)e.Current]);
					e.MustMoveNext ();
				}
				if (objs.Count > 0) {
					object result = objs [0];
					for (int i = 1; i < objs.Count; i++) {
						result = DynamicOr (result, objs [i]);
					}
					obj = result;
				} else
					obj = objs [0];
			} else if (e.Current is StringBuilder) {
				obj = obj.ToString ();
				e.MustMoveNext ();
			} else {
				throw new Exception ();
			}
			return obj;
		}

		object DynamicOr(object n1, object n2)
		{
			dynamic a = n1;
			dynamic b = n2;
			return (object)(a | b);
		}
	}
}

