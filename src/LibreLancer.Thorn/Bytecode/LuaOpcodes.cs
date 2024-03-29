﻿// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Thorn.Bytecode
{
	public enum LuaOpcodes : byte
	{
		EndCode,
		RetCode,
		Call,
		TailCall,
		PushNil,
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

