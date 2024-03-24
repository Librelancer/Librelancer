// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Thorn.Bytecode;

namespace LibreLancer.Thorn.VM
{
	public partial class ThornRuntime
	{
		static readonly OpcodeInfo[] Info = new OpcodeInfo[] {
			new OpcodeInfo(LuaOpcodes.EndCode, LuaOpcodes.EndCode, Arguments.None),
			new OpcodeInfo(LuaOpcodes.RetCode, LuaOpcodes.RetCode, Arguments.Byte),
			new OpcodeInfo(LuaOpcodes.Call, LuaOpcodes.Call, Arguments.ByteByte),
			new OpcodeInfo(LuaOpcodes.TailCall, LuaOpcodes.TailCall, Arguments.ByteByte),
			new OpcodeInfo (LuaOpcodes.PushNil, LuaOpcodes.PushNil, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.Pop, LuaOpcodes.Pop, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.PushNumberW, LuaOpcodes.PushNumber, Arguments.Word),
			new OpcodeInfo (LuaOpcodes.PushNumber, LuaOpcodes.PushNumber, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.PushNumberNegW, LuaOpcodes.PushNumberNeg, Arguments.Word),
			new OpcodeInfo (LuaOpcodes.PushNumberNeg, LuaOpcodes.PushNumberNeg, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.PushConstantW, LuaOpcodes.PushConstant, Arguments.Word),
			new OpcodeInfo (LuaOpcodes.PushConstant, LuaOpcodes.PushConstant, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.PushUpValue, LuaOpcodes.PushUpValue, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.PushLocal, LuaOpcodes.PushLocal, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.GetGlobalW, LuaOpcodes.GetGlobal, Arguments.Word),
			new OpcodeInfo (LuaOpcodes.GetGlobal, LuaOpcodes.GetGlobal, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.GetTable, LuaOpcodes.GetTable, Arguments.None),
			new OpcodeInfo (LuaOpcodes.GetDottedW, LuaOpcodes.GetDotted, Arguments.Word),
			new OpcodeInfo (LuaOpcodes.GetDotted, LuaOpcodes.GetDotted, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.PushSelfW, LuaOpcodes.PushSelf, Arguments.Word),
			new OpcodeInfo (LuaOpcodes.PushSelf, LuaOpcodes.PushSelf, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.CreateArrayW, LuaOpcodes.CreateArray, Arguments.Word),
			new OpcodeInfo (LuaOpcodes.CreateArray, LuaOpcodes.CreateArray, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.SetLocal, LuaOpcodes.SetLocal, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.SetGlobalW, LuaOpcodes.SetGlobal, Arguments.Word),
			new OpcodeInfo (LuaOpcodes.SetGlobal, LuaOpcodes.SetGlobal, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.SetTablePop, LuaOpcodes.SetTablePop, Arguments.None),
			new OpcodeInfo (LuaOpcodes.SetTable, LuaOpcodes.SetTable, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.SetListW, LuaOpcodes.SetList, Arguments.WordByte),
			new OpcodeInfo (LuaOpcodes.SetList, LuaOpcodes.SetList, Arguments.ByteByte),
			new OpcodeInfo (LuaOpcodes.SetMap, LuaOpcodes.SetMap, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.NeqOp, LuaOpcodes.NeqOp, Arguments.None),
			new OpcodeInfo (LuaOpcodes.EqOp, LuaOpcodes.EqOp, Arguments.None),
			new OpcodeInfo (LuaOpcodes.LtOp, LuaOpcodes.LtOp, Arguments.None),
			new OpcodeInfo (LuaOpcodes.LeOp, LuaOpcodes.LeOp, Arguments.None),
			new OpcodeInfo (LuaOpcodes.GtOp, LuaOpcodes.GtOp, Arguments.None),
			new OpcodeInfo (LuaOpcodes.GeOp, LuaOpcodes.GeOp, Arguments.None),
			new OpcodeInfo (LuaOpcodes.AddOp, LuaOpcodes.AddOp, Arguments.None),
			new OpcodeInfo (LuaOpcodes.SubOp, LuaOpcodes.SubOp, Arguments.None),
			new OpcodeInfo (LuaOpcodes.MultOp, LuaOpcodes.MultOp, Arguments.None),
			new OpcodeInfo (LuaOpcodes.DivOp, LuaOpcodes.DivOp, Arguments.None),
			new OpcodeInfo (LuaOpcodes.PowOp, LuaOpcodes.PowOp, Arguments.None),
			new OpcodeInfo (LuaOpcodes.ConcOp, LuaOpcodes.ConcOp, Arguments.None),
			new OpcodeInfo (LuaOpcodes.MinusOp, LuaOpcodes.MinusOp, Arguments.None),
			new OpcodeInfo (LuaOpcodes.NotOp, LuaOpcodes.NotOp, Arguments.None),
			new OpcodeInfo (LuaOpcodes.OntJmpW, LuaOpcodes.OntJmp, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.OntJmp, LuaOpcodes.OntJmp, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.OnfJmpW, LuaOpcodes.OnfJmp, Arguments.Word),
			new OpcodeInfo (LuaOpcodes.OnfJmp, LuaOpcodes.OnfJmp, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.JmpW, LuaOpcodes.Jmp, Arguments.Word),
			new OpcodeInfo (LuaOpcodes.Jmp, LuaOpcodes.Jmp, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.IffJmpW, LuaOpcodes.IffJmp, Arguments.Word),
			new OpcodeInfo (LuaOpcodes.IffJmp, LuaOpcodes.IffJmp, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.IftUpJmpW, LuaOpcodes.IftUpJmp, Arguments.Word),
			new OpcodeInfo (LuaOpcodes.IftUpJmp, LuaOpcodes.IftUpJmp, Arguments.Byte),
            new OpcodeInfo (LuaOpcodes.IffUpJmpW, LuaOpcodes.IffUpJmp, Arguments.Word),
            new OpcodeInfo (LuaOpcodes.IffUpJmp, LuaOpcodes.IffUpJmp, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.ClosureW, LuaOpcodes.Closure, Arguments.WordByte),
			new OpcodeInfo (LuaOpcodes.Closure, LuaOpcodes.Closure, Arguments.ByteByte),
			new OpcodeInfo (LuaOpcodes.SetLineW, LuaOpcodes.SetLine, Arguments.Word),
			new OpcodeInfo (LuaOpcodes.SetLine, LuaOpcodes.SetLine, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.LongArgW, LuaOpcodes.LongArg, Arguments.Word),
			new OpcodeInfo (LuaOpcodes.LongArg, LuaOpcodes.LongArg, Arguments.Byte),
			new OpcodeInfo (LuaOpcodes.CheckStack, LuaOpcodes.CheckStack, Arguments.Byte)
		};
		enum Arguments
		{
			None,
			Byte,
			Word,
			ByteByte,
			WordByte
		}
		class OpcodeInfo
		{
			public string DisplayName;
			public LuaOpcodes Value;
			public LuaOpcodes Code;
			public Arguments Operand;
			public OpcodeInfo(LuaOpcodes value, LuaOpcodes code, Arguments operand)
			{
				DisplayName = Value.ToString().ToUpperInvariant();
				Value = value;
				Code = code;
				Operand = operand;
			}
		}
	}
}

