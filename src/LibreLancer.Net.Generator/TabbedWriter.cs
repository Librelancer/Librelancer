using System;
using System.Text;
namespace LibreLancer.Net.Generator
{
    public class TabbedWriter
    {
        private StringBuilder builder = new StringBuilder();
        private int tabsCount = 0;

        public TabbedWriter Indent()
        {
            tabsCount++;
            return this;
        }

        public TabbedWriter UnIndent()
        {
            tabsCount--;
            return this;
        }

        private bool lineStarted = false;

        void StartLine()
        {
            if (!lineStarted)
            {
                lineStarted = true;
                for (int i = 0; i < tabsCount; i++) builder.Append("    ");
            }
        }

        public TabbedWriter AppendLine(string line)
        {
            StartLine();
            builder.AppendLine(line);
            lineStarted = false;
            return this;
        }

        public TabbedWriter AppendLine()
        {
            builder.AppendLine();
            lineStarted = false;
            return this;
        }


        public TabbedWriter Append(string text)
        {
            StartLine();
            builder.Append(text);
            return this;
        }

        public override string ToString()
        {
            return builder.ToString();
        }
    }
}
