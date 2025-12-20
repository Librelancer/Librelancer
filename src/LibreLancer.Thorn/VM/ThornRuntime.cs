// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.IO;
using LibreLancer.Thorn.Bytecode;
using LibreLancer.Thorn.Libraries;

namespace LibreLancer.Thorn.VM
{
    public delegate object ThornRuntimeFunction(object[] args);
    public delegate void ThornStdoutEvent(string output);


    public partial class ThornRuntime
    {
        const int LFIELDS_PER_FLUSH = 64;
        public Dictionary<string, object> Env = new Dictionary<string, object>();
        public Dictionary<string, object> Globals = new Dictionary<string, object>();

        public int MaxCallDepth = 25;
        public int MaxInstructions = 1_000_000;

        public event ThornStdoutEvent OnStdout;

        public ReadFileCallback OnReadFile;

        internal void Write(string str) => OnStdout?.Invoke(str);

        //Library state
        internal Random Random; //Purposely not initialized until first random() call

        public object DoString(string code, string source = "[file]")
        {
            var compiledBytes = ThornCompiler.Compile(code, source);
            int instCount = 0;
            Undump.Load(new MemoryStream(compiledBytes), out var p);
            return RunMethod(ThornClosure.Default(p), Array.Empty<object>(), 0, ref instCount);
        }

        public object DoStream(Stream stream, string source = "[file]")
        {
            int instCount = 0;
            return DoStreamInternal(stream, source, 0, ref instCount);
        }

        internal object DoStreamInternal(Stream stream, string source, int callDepth, ref int instCount)
        {
            if (Undump.Load(stream, out var p))
            {
                return RunMethod(ThornClosure.Default(p), Array.Empty<object>(), callDepth, ref instCount);
            }
            else
            {
                stream.Position = 0;
                return DoString(new StreamReader(stream).ReadToEnd(), source);
            }
        }

        public void SetBuiltins()
        {
            ThornIO.SetBuiltins(Env, this);
            ThornMath.SetBuiltins(Env, this);
            ThornString.SetBuiltins(Env, this);
            ThornTables.SetBuiltins(Env, this);
            ThornOS.SetBuiltins(Env, this);
            Env["tostring"] = (ThornRuntimeFunction)((e) => e[0] == null ? "nil" : e[0].ToString());
            Env["type"] = (ThornRuntimeFunction)((e) => e[0] switch
            {
                null => "nil",
                float _ => "number",
                int _ => "number",
                ThornTable _ => "table",
                LuaPrototype _ => "function",
                ThornRuntimeFunction _ => "function",
                ThornClosure => "function",
                Enum => "number",
                _ => "userdata"
            });
        }

        object[] GetCallArgs(ThornStack stack, int nArgs)
        {
            if (stack[stack.Count - 1] is ThornTuple tuple)
            {
                var callArgs = new object[nArgs - 1 + tuple.Values.Length];
                for (int i = 0; i < nArgs - 1; i++)
                {
                    callArgs[i] = stack[stack.Count - nArgs + i];
                }
                for (int i = 0; i < tuple.Values.Length; i++)
                {
                    callArgs[nArgs - 1 + i] = tuple.Values[i];
                }
                return callArgs;
            }
            else
            {
                var callArgs = new object[nArgs];
                for (int i = 0; i < nArgs; i++)
                {
                    callArgs[i] = stack[stack.Count - nArgs + i];
                }
                return callArgs;
            }
        }

        void PushResult(object result, int nResults)
        {

        }


        object RunMethod(ThornClosure closure, object[] args, int callDepth, ref int instCount)
        {
            if ((callDepth + 1) > MaxCallDepth)
                throw new ThornLimitsExceededException("Max call depth exceeded");
            int PC = 0;
            //stack size = data.Code [PC];
            int localCount = closure.Method.Code[PC++] + args.Length;
            var stack = new ThornStack(localCount);
            for (int i = 0; i < args.Length; i++) {
                stack.Push(args[i]);
            }
            int stackBase = 0;
            //read args
            PC++;
            //first opcode
            while (PC < closure.Method.Code.Length)
            {
                var op = ReadOpcode(closure.Method, ref PC);
                if (instCount++ > MaxInstructions)
                    throw new ThornLimitsExceededException("Max instructions exceeded");
                switch (op.Code)
                {
                    case LuaOpcodes.PushNumber:
                    {
                        stack.Push((float)op.Argument1);
                        break;
                    }
                    case LuaOpcodes.PushNumberNeg:
                    {
                        stack.Push((float)(-op.Argument1));
                        break;
                    }
                    case LuaOpcodes.TailCall:
                    {
                        int nResults = op.Argument1;
                        int nArgs = op.Argument2;
                        var callArgs = GetCallArgs(stack, nArgs);
                        var func = stack[stack.Count - nArgs - 1];
                        stack.Pop(nArgs + 1);
                        if (func is ThornRuntimeFunction native)
                            return  native(callArgs);
                        else
                        {
                            ThornClosure target;
                            if (func is LuaPrototype pt)
                                target = ThornClosure.Default(pt);
                            else
                                target = (ThornClosure)func;
                            return RunMethod(target, callArgs, callDepth + 1, ref instCount);
                        }
                    }
                    case LuaOpcodes.Call:
                    {
                        int nResults = op.Argument1;
                        int nArgs = op.Argument2;
                        var callArgs = GetCallArgs(stack, nArgs);
                        var func = stack[stack.Count - nArgs - 1];
                        stack.Pop(nArgs + 1);
                        object result;
                        if (func is ThornRuntimeFunction native)
                            result = native(callArgs);
                        else
                        {
                            ThornClosure target;
                            if (func is LuaPrototype pt)
                                target = ThornClosure.Default(pt);
                            else
                                target = (ThornClosure)func;
                            result = RunMethod(target, callArgs, callDepth + 1, ref instCount);
                        }
                        if (nResults == 1) {
                            if(result is ThornTuple tuple)
                                stack.Push(tuple.Values[0]);
                            else
                                stack.Push(result);
                        }
                        else if (nResults > 0) {
                            if (result is ThornTuple tuple)
                            {
                                for(int i = 0; i < tuple.Values.Length; i++)
                                    stack.Push(tuple.Values[i]);
                                nResults -= tuple.Values.Length;
                            }
                            else {
                                stack.Push(result);
                                nResults--;
                            }
                            while(nResults-- > 0)
                                stack.Push(null);
                        }
                        break;
                    }
                    case LuaOpcodes.Closure:
                    {
                        var upvalues = new object[op.Argument2];
                        for (int i = 0; i < op.Argument2; i++) {
                            upvalues[i] = stack[stack.Count - op.Argument2 + i];
                        }
                        stack.Pop(op.Argument2);
                        var func = closure.Method.Constants[op.Argument1].Cast<LuaPrototype>();
                        stack.Push(new ThornClosure() { Method = func, Upvalues = upvalues});
                        break;
                    }
                    case LuaOpcodes.Pop:
                    {
                        for (int i = 0; i < op.Argument1; i++)
                            stack.Pop();
                        break;
                    }
                    case LuaOpcodes.SetGlobal:
                    {
                        var key = closure.Method.Constants[op.Argument1].Cast<string>();
                        if (!Globals.ContainsKey(key))
                            Globals.Add(key, stack.Pop());
                        else
                            Globals[key] = stack.Pop();
                        break;
                    }
                    case LuaOpcodes.CreateArray:
                    {
                        var arr = new ThornTable(); //op.Argument1 - capacity
                        stack.Push(arr);
                        break;
                    }
                    case LuaOpcodes.PushConstant:
                    {
                        stack.Push(closure.Method.Constants[op.Argument1].Value);
                        break;
                    }
                    case LuaOpcodes.PushNil:
                    {
                        int aux = op.Argument1;
                        do {
                            stack.Push(null);
                        } while (aux-- > 0);
                        break;
                    }
                    case LuaOpcodes.PushLocal:
                    {
                        stack.Push(stack[stackBase + op.Argument1]);
                        break;
                    }
                    case LuaOpcodes.PushUpValue:
                    {
                        stack.Push(closure.Upvalues[op.Argument1]);
                        break;
                    }
                    case LuaOpcodes.SetLocal:
                    {
                        stack[op.Argument1] = stack.Pop();
                        break;
                    }
                    case LuaOpcodes.OntJmp:
                    {
                        if (stack.Peek() != null) PC += op.Argument1;
                        else stack.Pop();
                        break;
                    }
                    case LuaOpcodes.OnfJmp:
                    {
                        if (stack.Peek() == null) PC += op.Argument1;
                        else stack.Pop();
                        break;
                    }
                    case LuaOpcodes.IffJmp:
                    {
                        if (stack.Pop() == null)
                        {
                            PC += op.Argument1;
                        }
                        break;
                    }
                    case LuaOpcodes.IftUpJmp:
                    {
                        if (stack.Pop() != null)
                        {
                            PC -= op.Argument1;
                        }
                        break;
                    }
                    case LuaOpcodes.IffUpJmp:
                    {
                        if (stack.Pop() == null)
                        {
                            PC -= op.Argument1;
                        }
                        break;
                    }
                    case LuaOpcodes.GetGlobal:
                    {
                        var key = closure.Method.Constants[op.Argument1].Cast<string>();
                        if (!Env.ContainsKey(key))
                        {
                            stack.Push(Globals[key]);
                        }
                        else
                            stack.Push(Env[key]);

                        break;
                    }
                    case LuaOpcodes.GetTable:
                    {
                        var index = stack.Pop();
                        var table = stack.Pop();
                        stack.Push(((ThornTable)table)[index]);
                        break;
                    }
                    case LuaOpcodes.GetDotted:
                    {
                        var table = stack.Pop();
                        stack.Push(((ThornTable)table)[closure.Method.Constants[op.Argument1].Cast<string>()]);
                        break;
                    }
                    case LuaOpcodes.SetList:
                    {
                        var objects = new object[op.Argument2];
                        for (int i = 0; i < op.Argument2; i++)
                        {
                            objects[i] = stack[stack.Count - (op.Argument2 - i)];
                        }

                        //pop objects from stack
                        for (int i = 0; i < op.Argument2; i++)
                            stack.Pop();
                        var pk = stack.Peek();
                        if (!(pk is ThornTable))
                            throw new Exception("Stack type mismatch");
                        ((ThornTable)pk).SetArray(op.Argument1 * LFIELDS_PER_FLUSH, objects); //Argument1 is offset
                        break;
                    }
                    case LuaOpcodes.SetTable:
                    {
                        var idx = stack.Count - 3 - op.Argument1;
                        var table = (ThornTable)stack[idx];
                        var index = stack[idx + 1];
                        var value = stack.Pop();
                        table[index] = value;
                        break;
                    }
                    case LuaOpcodes.SetTablePop:
                    {
                        var value = stack.Pop();
                        var index = stack.Pop();
                        var table = stack.Pop();
                        var t = (ThornTable)table;
                        t.Set(index, value);
                        break;
                    }
                    case LuaOpcodes.SetMap:
                    {
                        op.Argument1++;
                        //fetch dictionary from stack
                        var check = stack[(stack.Count - ((op.Argument1 * 2)))];
                        bool isArray = check is float;
                        Dictionary<string, object> map = null;
                        object[] array = null;
                        if (isArray)
                        {
                            array = new object[op.Argument1];
                            int i = 0;
                            while (i < op.Argument1)
                            {
                                var idx = (stack.Count - ((op.Argument1 * 2) - i * 2));
                                array[(int)(float)stack[idx] - 1] = stack[(idx + 1)];
                                i++;
                            }
                        }
                        else
                        {
                            map = new Dictionary<string, object>(op.Argument1);
                            int i = 0;
                            while (i < op.Argument1)
                            {
                                var idx = (stack.Count - ((op.Argument1 * 2) - i * 2));
                                map.Add(stack[idx] as string, stack[(idx + 1)]);
                                i++;
                            }
                        }

                        //pop from stack
                        for (int j = 0; j < (op.Argument1 * 2); j++)
                            stack.Pop();
                        //set to object
                        var pk = stack.Peek();
                        if (!(pk is ThornTable))
                            throw new Exception("Stack type mismatch");
                        if (isArray)
                            ((ThornTable)pk).SetArray(0, array);
                        else
                            ((ThornTable)pk).SetMap(map);
                        break;
                    }
                    case LuaOpcodes.AddOp:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(AddOp(a, b));
                        break;
                    }
                    case LuaOpcodes.MultOp:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(MulOp(a, b));
                        break;
                    }
                    case LuaOpcodes.SubOp:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(SubOp(a, b));
                        break;
                    }
                    case LuaOpcodes.PowOp:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        if (!Conversion.TryGetNumber(a, out float x) ||
                            !Conversion.TryGetNumber(b, out float y))
                            throw new InvalidCastException();
                        stack.Push(MathF.Pow(x, y));
                        break;
                    }
                    case LuaOpcodes.Jmp:
                    {
                        PC += op.Argument1;
                        break;
                    }
                    case LuaOpcodes.DivOp:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(DivOp(a, b));
                        break;
                    }
                    case LuaOpcodes.ConcOp:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(a.ToString() + b.ToString());
                        break;
                    }
                    case LuaOpcodes.LtOp:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(DynamicLt(a, b));
                        break;
                    }
                    case LuaOpcodes.LeOp:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(DynamicLe(a,b));
                        break;
                    }
                    case LuaOpcodes.GtOp:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(DynamicGt(a,b));
                        break;
                    }
                    case LuaOpcodes.GeOp:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(DynamicGe(a,b));
                        break;
                    }
                    case LuaOpcodes.EqOp:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(object.Equals(a,b) ? 1f : null);
                        break;
                    }

                    case LuaOpcodes.NeqOp:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(object.Equals(a,b) ? null : 1f);
                        break;
                    }
                    case LuaOpcodes.NotOp:
                    {
                        stack.Push(NotOp(stack.Pop()));
                        break;
                    }
                    case LuaOpcodes.RetCode:
                    {
                        if (stack.Count - op.Argument1 > 1)
                        {
                            var returns = new object[stack.Count - op.Argument1];
                            for (int i = op.Argument1; i < stack.Count; i++)
                            {
                                returns[i - op.Argument1] = stack[i];
                            }
                            return new ThornTuple(returns);
                        }
                        //top of stack?
                        return stack.Peek();
                    }
                    case LuaOpcodes.EndCode:
                        //Success! Do nothing
                        break;
                    default:
                        throw new NotImplementedException(op.Code.ToString());
                }
            }

            //end
            return null;
        }

        bool Numeric(object n1, object n2, out float x, out float y)
        {
            x = y = 0;
            return Conversion.TryGetNumber(n1, out x) && Conversion.TryGetNumber(n2, out y);
        }

        object AddOp(object n1, object n2)
        {
            if (Numeric(n1, n2, out var x, out var y))
                return (object)(x + y);
            else
                throw new InvalidCastException();
        }

        object NotOp(object n1)
        {
            if (Conversion.TryGetNumber(n1, out var x))
                return -x;
            throw new InvalidCastException();
        }

        object MulOp(object n1, object n2)
        {
            if (Numeric(n1, n2, out var x, out var y))
                return (object)(x * y);
            else
                throw new InvalidCastException();
        }

        object SubOp(object n1, object n2)
        {
            if (Numeric(n1, n2, out var x, out var y))
                return (object)(x - y);
            else
                throw new InvalidCastException();
        }

        object DivOp(object n1, object n2)
        {
            if (Numeric(n1, n2, out var x, out var y))
                return (object)(x / y);
            else
                throw new InvalidCastException();
        }

        bool CompareStrings(object a, object b, out int x)
        {
            x = 0;
            if (a is string x1 && b is string x2)
            {
                x = string.Compare(x1, x2, StringComparison.Ordinal);
                return true;
            }
            return false;
        }

        object DynamicLt(object n1, object n2)
        {
            if (CompareStrings(n1, n2, out int r))
                return r < 0 ? 1f : null;
            dynamic a = n1;
            dynamic b = n2;
            return (object)(a < b ? 1f : null);
        }

        object DynamicLe(object n1, object n2)
        {
            if (CompareStrings(n1, n2, out int r))
                return r <= 0 ? 1f : null;
            dynamic a = n1;
            dynamic b = n2;
            return (object)(a <= b ? 1f : null);
        }

        object DynamicGt(object n1, object n2)
        {
            if (CompareStrings(n1, n2, out int r))
                return r > 0 ? 1f : null;
            dynamic a = n1;
            dynamic b = n2;
            return (object)(a > b ? 1f : null);
        }

        object DynamicGe(object n1, object n2)
        {
            if (CompareStrings(n1, n2, out int r))
                return r >= 0 ? 1f : null;
            dynamic a = n1;
            dynamic b = n2;
            return (object)(a >= b ? 1f : null);
        }

        Opcode ReadOpcode(LuaPrototype data, ref int PC)
        {
            var value = new Opcode();
            var code = data.Code[PC++];
            var info = Info[code];
            value.Code = info.Code;
            switch (info.Operand)
            {
                case Arguments.Byte:
                    value.Argument1 = data.Code[PC++];
                    break;
                case Arguments.ByteByte:
                    value.Argument1 = data.Code[PC++];
                    value.Argument2 = data.Code[PC++];
                    break;
                case Arguments.Word:
                    value.Argument1 = (data.Code[PC] << 8) + data.Code[PC + 1];
                    PC += 2;
                    break;
                case Arguments.WordByte:
                    value.Argument1 = (data.Code[PC] << 8) + data.Code[PC + 1];
                    PC += 2;
                    value.Argument2 = data.Code[PC++];
                    break;
            }
            return value;
        }

        class Opcode
        {
            public LuaOpcodes Code;
            public int Argument1;
            public int Argument2;
            public override string ToString() => $"{Code} ({Info[(int)Code].Operand}): {Argument1} {Argument2}";
        }
    }
}
