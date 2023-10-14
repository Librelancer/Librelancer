using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using WattleScript.Interpreter;
using WattleScript.Interpreter.Interop;
using WattleScript.Interpreter.Interop.BasicDescriptors;
using WattleScript.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;

namespace LibreLancer.Interface.WattleMaths;

public class WattleVector2 : HardwiredUserDataDescriptor
{
    public static void CreateTable(Script script)
    {
        var tbl = new Table(script);
        tbl.Kind = TableKind.Class;
        script.Globals["Vector2"] = tbl;
        tbl["new"] = (float x, float y) => new Vector2(x, y);
    }
    public WattleVector2() : base(typeof(Vector2))
    {
        AddMember("X", new DescX());
        AddMember("Y", new DescY());
        AddMember("op_Addition", new Add());
        AddMember("op_Subtraction", new Sub());
        AddMember("op_UnaryNegation", new Negate());
        AddMember("op_Multiply", new OverloadedMethodMemberDescriptor(
            "op_Multiply",
            typeof(Vector2),
            new IOverloadableMemberDescriptor[] { new MulScalar(), new MulVec() }
            ));
        AddMember("op_Division", new OverloadedMethodMemberDescriptor(
            "op_Division",
            typeof(Vector2),
            new IOverloadableMemberDescriptor[] { new DivScalar(), new DivVec() }
        ));
    }

    class Negate : HardwiredMethodMemberDescriptor
    {
        public Negate()
        {
            Initialize("op_UnaryNegation", true, new ParameterDescriptor[]
            {
                new("value", typeof(Vector2)),
            }, false);
        }

        protected override object Invoke(Script script, object obj, object[] pars, int argscount) =>
            -(Vector2) pars[0];
    }
    class MulScalar : HardwiredMethodMemberDescriptor
    {
        public MulScalar()
        {
            Initialize("op_Multiply", true, new ParameterDescriptor[]
            {
                new("left", typeof(Vector2)),
                new("right", typeof(float))
            }, false);
        }

        protected override object Invoke(Script script, object obj, object[] pars, int argscount) =>
            (Vector2) pars[0] * (float) pars[1];
    }

    class MulVec : HardwiredMethodMemberDescriptor
    {
        public MulVec()
        {
            Initialize("op_Multiply", true, new ParameterDescriptor[]
            {
                new("left", typeof(Vector2)),
                new("right", typeof(Vector2))
            }, false);
        }

        protected override object Invoke(Script script, object obj, object[] pars, int argscount) =>
            (Vector2) pars[0] * (Vector2) pars[1];
    }

    class DivScalar : HardwiredMethodMemberDescriptor
    {
        public DivScalar()
        {
            Initialize("op_Division", true, new ParameterDescriptor[]
            {
                new("left", typeof(Vector2)),
                new("right", typeof(float))
            }, false);
        }

        protected override object Invoke(Script script, object obj, object[] pars, int argscount) =>
            (Vector2) pars[0] / (float) pars[1];
    }

    class DivVec : HardwiredMethodMemberDescriptor
    {
        public DivVec()
        {
            Initialize("op_Division", true, new ParameterDescriptor[]
            {
                new("left", typeof(Vector2)),
                new("right", typeof(Vector2))
            }, false);
        }

        protected override object Invoke(Script script, object obj, object[] pars, int argscount) =>
            (Vector2) pars[0] / (Vector2) pars[1];
    }

    class Add : HardwiredMethodMemberDescriptor
    {
        public Add()
        {
            Initialize("op_Addition", true, new ParameterDescriptor[]
            {
                new("left", typeof(Vector2)),
                new("right", typeof(Vector2))
            }, false);
        }

        protected override object Invoke(Script script, object obj, object[] pars, int argscount) =>
            (Vector2) pars[0] + (Vector2) pars[1];
    }

    class Sub : HardwiredMethodMemberDescriptor
    {
        public Sub()
        {
            Initialize("op_Subtraction", true, new ParameterDescriptor[]
            {
                new("left", typeof(Vector2)),
                new("right", typeof(Vector2))
            }, false);
        }

        protected override object Invoke(Script script, object obj, object[] pars, int argscount) =>
            (Vector2) pars[0] - (Vector2) pars[1];
    }


    class DescX : HardwiredMemberDescriptor
    {
        public DescX() :
            base(typeof(float), "X",
                false, MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite)
        {
        }

        protected override object GetValueImpl(Script script, object obj)
        {
            return Unsafe.Unbox<Vector2>(obj).X;
        }

        protected override void SetValueImpl(Script script, object obj, object value)
        {
            Unsafe.Unbox<Vector2>(obj).X = (float)value;
        }
    }

    class DescY : HardwiredMemberDescriptor
    {
        public DescY() :
            base(typeof(float), "X",
                false, MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite)
        {
        }

        protected override object GetValueImpl(Script script, object obj)
        {
            return Unsafe.Unbox<Vector2>(obj).Y;
        }

        protected override void SetValueImpl(Script script, object obj, object value)
        {
            Unsafe.Unbox<Vector2>(obj).Y = (float)value;
        }
    }

}
