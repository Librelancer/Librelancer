using System;
using System.Text;
namespace LibreLancer.GeneratorCommon
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

        public struct BlockHandle : IDisposable
        {
            public TabbedWriter Writer;
            public BlockHandle(TabbedWriter tw)
            {
                Writer = tw;
            }
            public void Dispose()
            {
                Writer.UnIndent().AppendLine("}");
            }
        }

        public BlockHandle Block()
        {
            AppendLine("{").Indent();
            return new BlockHandle(this);
        }

        public TabbedWriter AppendEditorHiddenLine()
        {
            StartLine();
            builder.AppendLine("[System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
            lineStarted = false;
            return this;
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
