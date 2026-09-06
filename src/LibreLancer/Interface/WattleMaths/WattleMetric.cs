using System.Runtime.CompilerServices;
using WattleScript.Interpreter;
using WattleScript.Interpreter.Interop.BasicDescriptors;
using WattleScript.Interpreter.Interop.StandardDescriptors.HardwiredDescriptors;

namespace LibreLancer.Interface.WattleMaths;

internal class WattleMetric : HardwiredUserDataDescriptor
{
    public WattleMetric() : base(typeof(Metric))
    {
        AddMember("Unit", new DescUnit());
        AddMember("Value", new DescValue());
        AddMember("Constant", new DescConstant());
    }

    private class DescUnit()
        : HardwiredMemberDescriptor(typeof(MetricUnit), "Unit", false, MemberDescriptorAccess.CanRead)
    {
        protected override object GetValueImpl(Script script, object obj)
        {
            return Unsafe.Unbox<Metric>(obj).Unit;
        }
    }

    private class DescValue() : HardwiredMemberDescriptor(typeof(float), "Value", false, MemberDescriptorAccess.CanRead)
    {
        protected override object GetValueImpl(Script script, object obj)
        {
            return Unsafe.Unbox<Metric>(obj).Value;
        }
    }

    private class DescConstant()
        : HardwiredMemberDescriptor(typeof(float), "Constant", false, MemberDescriptorAccess.CanRead)
    {
        protected override object GetValueImpl(Script script, object obj)
        {
            return Unsafe.Unbox<Metric>(obj).Constant;
        }
    }
}
