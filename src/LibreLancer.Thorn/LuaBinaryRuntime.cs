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
namespace LibreLancer.Thorn
{
	partial class LuaBinaryRuntime
	{
		LuaPrototype data;
		int PC = 0;
		const int LFIELDS_PER_FLUSH = 64;
		public Dictionary<string, object> Env = new Dictionary<string, object>();
		public Dictionary<string,object> Globals = new Dictionary<string,object>();
		public LuaBinaryRuntime (LuaPrototype proto)
		{
			data = proto;
		}
		public void Run()
		{
			//stack size = data.Code [PC];
			var stack = new LuaStack(data.Code[PC++]);
			//read args
			PC++;
			//first opcode
			while (PC < data.Code.Length) {
				var op = ReadOpcode ();
				switch (op.Code) {
				case LuaOpcodes.PushNumber:
					{
						stack.Push ((float)op.Argument1);
						break;
					}
				case LuaOpcodes.PushNumberNeg:
					{
						stack.Push ((float)(-op.Argument1));
						break;
					}
				case LuaOpcodes.SetGlobal:
					{
						var key = data.Constants [op.Argument1].Cast<string> ();
						if (!Globals.ContainsKey (key))
							Globals.Add (key, stack.Pop ());
						else
							Globals [key] = stack.Pop ();
						break;
					}
				case LuaOpcodes.CreateArray:
					{
						var arr = new LuaTable (op.Argument1);
						stack.Push (arr);
						break;
					}
				case LuaOpcodes.PushConstant:
					{
						stack.Push (data.Constants [op.Argument1].Value);
						break;
					}
				case LuaOpcodes.GetGlobal:
					{
						var key = data.Constants [op.Argument1].Cast<string> ();
						if (!Env.ContainsKey (key)) {
							stack.Push (Globals [key]);
						} else
							stack.Push (Env [key]);
						break;
					}
				case LuaOpcodes.SetList:
					{
						var objects = new object[op.Argument2];
						for (int i = 0; i < op.Argument2; i++) {
							objects [i] = stack [stack.Count - (op.Argument2 - i)];
						}
						//pop objects from stack
						for (int i = 0; i < op.Argument2; i++)
							stack.Pop ();
						var pk = stack.Peek ();
						if (!(pk is LuaTable))
							throw new Exception ("Stack type mismatch");
						((LuaTable)pk).SetArray (op.Argument1 * LFIELDS_PER_FLUSH, objects); //Argument1 is offset
						break;
					}
				case LuaOpcodes.SetMap:
					{
						op.Argument1++;
						//fetch dictionary from stack
						Dictionary<string,object> map = new Dictionary<string, object> (op.Argument1);
						int i = 0;
						while (i < op.Argument1) {
							var idx = (stack.Count - ((op.Argument1 * 2) - i * 2));
							map.Add (stack[idx] as string, stack[(idx + 1)]);
							i++;
						}
						//pop from stack
						for (int j = 0; j < (op.Argument1 * 2); j++)
							stack.Pop ();
						//set to object
						var pk = stack.Peek ();
						if (!(pk is LuaTable))
							throw new Exception ("Stack type mismatch");
						((LuaTable)pk).SetMap (map);
						Console.WriteLine ();
						break;
					}
				case LuaOpcodes.AddOp:
					var b = stack.Pop ();
					var a = stack.Pop ();
					stack.Push(DynamicOr(a, b));
					break;
				case LuaOpcodes.EndCode:
					//Success! Do nothing
					break;
				default:
					throw new NotImplementedException (op.Code.ToString ());
				}
			}
			//end
			PC = 0;
		}

		object DynamicOr(object n1, object n2)
		{
			dynamic a = n1;
			dynamic b = n2;
			return (object)(a | b);
		}

		Opcode ReadOpcode()
		{
			var value = new Opcode ();
			var code = data.Code [PC++];
			var info = Info [code];
			value.Code = info.Code;
			switch (info.Operand) {
			case Arguments.Byte:
				value.Argument1 = data.Code [PC++];
				break;
			case Arguments.ByteByte:
				value.Argument1 = data.Code [PC++];
				value.Argument2 = data.Code [PC++];
				break;
			case Arguments.Word:
				value.Argument1 = (data.Code [PC] << 8) + data.Code [PC + 1];
				PC += 2;
				break;
			case Arguments.WordByte:
				value.Argument1 = (data.Code [PC] << 8) + data.Code [PC + 1];
				PC += 2;
				value.Argument2 = data.Code [PC++];
				break;
			}
			return value;
		}
		class Opcode
		{
			public LuaOpcodes Code;
			public int Argument1;
			public int Argument2;
		}
	}
}

