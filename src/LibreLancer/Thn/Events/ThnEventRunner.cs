using System;
namespace LibreLancer
{
    public interface IThnEventRunner
    {
        void Process(ThnEvent ev, Cutscene cs);
    }
    public class ThnEventRunnerAttribute : Attribute
    {
        public EventTypes Event;
        public ThnEventRunnerAttribute(EventTypes ev)
        {
            Event = ev;
        }
    }
}
