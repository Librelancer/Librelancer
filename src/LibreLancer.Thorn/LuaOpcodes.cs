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

namespace LibreLancer.Thorn
{
	public enum LuaOpcodes : byte
	{
		EndCode,
		RetCode,
		Call,
		TailCall,
		PushNill,
		Pop,
		PushNumberW,
		PushNumber,
		PushNumberNegW,
		PushNumberNeg,
		PushConstantW,
		PushConstant,
		PushUpValue,
		PushLocal,
		GetGlobalW,
		GetGlobal,
		GetTable,
		GetDottedW,
		GetDotted,
		PushSelfW,
		PushSelf,
		CreateArrayW,
		CreateArray,
		SetLocal,
		SetGlobalW,
		SetGlobal,
		SetTablePop,
		SetTable,
		SetListW,
		SetList,
		SetMap,
		NeqOp,
		EqOp,
		LtOp,
		LeOp,
		GtOp,
		GeOp,
		AddOp,
		SubOp,
		MultOp,
		DivOp,
		PowOp,
		ConcOp,
		MinusOp,
		NotOp,
		OntJmpW,
		OntJmp,
		OnfJmpW,
		OnfJmp,
		JmpW,
		Jmp,
		IffJmpW,
		IffJmp,
		IftUpJmpW,
		IftUpJmp,
		IffUpJmpW,
		IffUpJmp,
		ClosureW,
		Closure,
		SetLineW,
		SetLine,
		LongArgW,
		LongArg,
		CheckStack
	}
}

