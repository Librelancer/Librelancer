using System.Numerics;
using System.Runtime.CompilerServices;
using WattleScript.Interpreter;
using WattleScript.Interpreter.Interop.BasicDescriptors;
using WattleScript.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;

namespace LibreLancer.Interface.WattleMaths;

class WattleVector3 : HardwiredUserDataDescriptor
{
    public WattleVector3() : base(typeof(Vector3))
    {
        AddMember("X", new DescX());
        AddMember("Y", new DescY());
        AddMember("Z", new DescZ());
    }

    public static void CreateTable(Script script)
    {
        var tbl = new Table(script);
        tbl.Kind = TableKind.Class;
        script.Globals["Vector3"] = tbl;
        tbl["new"] = (float x, float y, float z) => new Vector3(x, y, z);
        tbl["dot"] = (Vector3.Dot);
        tbl["cross"] = (Vector3.Cross);
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
            return Unsafe.Unbox<Vector3>(obj).X;
        }

        protected override void SetValueImpl(Script script, object obj, object value)
        {
            Unsafe.Unbox<Vector3>(obj).X = (float) value;
        }
    }

    class DescY : HardwiredMemberDescriptor
    {
        public DescY() :
            base(typeof(float), "Y",
                false, MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite)
        {
        }

        protected override object GetValueImpl(Script script, object obj)
        {
            return Unsafe.Unbox<Vector3>(obj).Y;
        }

        protected override void SetValueImpl(Script script, object obj, object value)
        {
            Unsafe.Unbox<Vector3>(obj).Y = (float) value;
        }
    }

    class DescZ : HardwiredMemberDescriptor
    {
        public DescZ() :
            base(typeof(float), "Z",
                false, MemberDescriptorAccess.CanRead | MemberDescriptorAccess.CanWrite)
        {
        }

        protected override object GetValueImpl(Script script, object obj)
        {
            return Unsafe.Unbox<Vector3>(obj).Z;
        }

        protected override void SetValueImpl(Script script, object obj, object value)
        {
            Unsafe.Unbox<Vector3>(obj).Z = (float) value;
        }
    }
}
