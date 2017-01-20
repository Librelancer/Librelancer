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
using System.Globalization;
using System.Linq;
using System.Text;

namespace LibreLancer.Thorn
{
	public class LuaLexer
	{
		public static readonly object Eat = new object ();

		public delegate object ParseFunction (char ch, StringIterator reader);

		public static object ParseComment(char ch, StringIterator reader)
		{
			if (ch != '-' || (char)reader.Peek (2) != '-')
				return null;
			while (true) {
				int pk = reader.Peek ();
				if (pk == -1 || pk == (char)'\n')
					break;
				reader.Read ();
			}
			return Eat;
		}

		public static object ParseFloat (char ch, StringIterator reader)
		{
			if (!char.IsDigit (ch) && !(ch == '-' && char.IsDigit((char)reader.Peek(2))))
				return null;
			var builder = new StringBuilder ();
			builder.Append (ch);
			reader.Read (); //read
			while (true) {
				var pk = reader.Peek ();
				if (pk == -1)
					break;
				ch = (char)pk;
				if (ch == 'e') {
					if (reader.Peek (2) == (int)'-' || reader.Peek (2) == (int)'+') {
						reader.Read ();
						builder.Append ("e");
						builder.Append ((char)reader.Read ());
					}
				} else if (ch != '.' && !char.IsDigit (ch))
					break;
				else {
					builder.Append (ch);
					reader.Read ();
				}
			}
			return float.Parse (builder.ToString (), CultureInfo.InvariantCulture);
		}
		public static object ParseStringLiteral(char ch, StringIterator reader)
		{
			if (ch != '"')
				return null;
			var builder = new StringBuilder ();
			reader.Read ();
			while (true) {
				var pk = reader.Peek ();
				if (pk == -1)
					break;
				ch = (char)pk;
				if (ch == '"') {
					reader.Read ();
					break;
				}
				builder.Append (ch);
				reader.Read ();
			}
			return builder;
		}
		public static object ParseIdentifier (char ch, StringIterator reader)
		{
			if (!char.IsLetter (ch) && ch != '_')
				return null;
			var builder = new StringBuilder ();
			builder.Append (ch);
			reader.Read ();
			while (true) {
				var pk = reader.Peek ();
				if (pk == -1)
					break;
				ch = (char)pk;
				if (!char.IsLetterOrDigit (ch) && ch != '_')
					break;
				builder.Append (ch);
				reader.Read ();
			}
			return builder.ToString ();
		}

		public static ParseFunction ParseChars (params char[] chars)
		{
			return (ch, reader) => { 
				if (chars.Contains (ch)) {
					reader.Read ();
					return ch;
				}
				return null;
			};
		}

		public static object EatWhitespace (char ch, StringIterator reader)
		{
			if (char.IsWhiteSpace (ch)) {
				reader.Read ();
				return Eat;
			}
			else
				return null;
		}

		ParseFunction[] functions;

		public LuaLexer (params ParseFunction[] lexfuncs)
		{
			functions = lexfuncs;
		}

		public IEnumerable<object> Process (string str, int index = 0)
		{
			char ch;
			var reader = new StringIterator (str, index);
			while (reader.Peek () != -1) {
				ch = (char)reader.Peek ();
				bool resolved = false;
				foreach (var func in functions) {
					var result = func (ch, reader);
					if (result == null)
						continue;
					if (result != Eat) {
						yield return result;
					}
					resolved = true;
					break;
				}
				if (!resolved)
					throw new Exception ("Unexpected character " + ch);
			}
		}

	}

	public class StringIterator
	{
		int index = -1;
		string str;

		public StringIterator (string s, int idx)
		{
			str = s;
			index = idx - 1;
		}

		public int Peek (int amount = 1)
		{
			if (index + amount > (str.Length - 1))
				return -1;
			return str [index + amount];
		}

		public int Read ()
		{
			if (index + 1 > (str.Length - 1))
				return -1;
			index++;
			return str [index];
		}
	}
}

