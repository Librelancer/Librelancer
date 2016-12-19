/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
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

		Dictionary<string,object> DoTextFile(Stream stream)
		{
			LuaLexer lex = new LuaLexer (
				LuaLexer.EatWhitespace,
				LuaLexer.ParseFloat,
				LuaLexer.ParseStringLiteral,
				LuaLexer.ParseIdentifier,
				LuaLexer.ParseChars (
					'{', '}', ',', '=', '+', ';'
				)
			);
			IEnumerable<object> tokstream;
			using (var reader = new StreamReader (stream)) {
				tokstream = lex.Process (reader.ReadToEnd ());
			}
			Dictionary<string,object> env = new Dictionary<string, object> ();
			using (IEnumerator<object> e = tokstream.GetEnumerator ()) {
				//Get first element
				e.MustMoveNext ();
				LKeyValue lkv;
				while ((lkv = ParseAssignment (e)) != null) {
					if (e.Current.Equals (';'))
						e.MoveNext ();
					env.Add (lkv.Key, lkv.Value);
				}
			}
			return env;
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
						objs.Add (ident);
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

