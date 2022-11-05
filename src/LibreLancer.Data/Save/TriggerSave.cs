using System.Text;
using LibreLancer.Ini;

namespace LibreLancer.Data.Save;

public class TriggerSave : IWriteSection
{
    [Entry("trigger")]
    public int Trigger;
    
    public void WriteTo(StringBuilder builder)
    {
        builder.AppendLine("[TriggerSave]")
            .AppendEntry("trigger", (uint) Trigger)
            .AppendLine();
    }
}