using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xwt;
using Xwt.Drawing;
using XwtPlus.TextEditor.Margins;

namespace XwtPlus.TextEditor
{
    public class TextEditor : ScrollView
    {
        TextArea textArea;

        public Caret Caret { get; private set; }
        CaretAnimation CaretAnimation;

        TextSegment selection;
        public TextSegment Selection
        {
            get { return selection; }
            set
            {
                this.selection = value;
                textArea.QueueDraw();
            }
        }

        TextDocument document;
        public TextEditorOptions Options { get; private set; }

        public TextDocument Document {
            get {
                return document;
            }
        }

        IIndentationTracker indentationTracker = null;
        public bool HasIndentationTracker
        {
            get
            {
                return indentationTracker != null;
            }
        }

        public IIndentationTracker IndentationTracker
        {
            get
            {
                if (!HasIndentationTracker)
                    throw new InvalidOperationException("Indentation tracker not installed.");
                return indentationTracker;
            }
            set
            {
                indentationTracker = value;
            }
        }

        public string GetIndentationString(DocumentLocation loc)
        {
            return IndentationTracker.GetIndentationString(loc.Line, loc.Column);
        }

        public string GetIndentationString(int lineNumber, int column)
        {
            return IndentationTracker.GetIndentationString(lineNumber, column);
        }

        public string GetIndentationString(int offset)
        {
            return IndentationTracker.GetIndentationString(offset);
        }

        public int GetVirtualIndentationColumn (DocumentLocation loc)
		{
			return IndentationTracker.GetVirtualIndentationColumn (loc.Line, loc.Column);
		}
		
		public int GetVirtualIndentationColumn (int lineNumber, int column)
		{
			return IndentationTracker.GetVirtualIndentationColumn (lineNumber, column);
		}
		
		public int GetVirtualIndentationColumn (int offset)
		{
			return IndentationTracker.GetVirtualIndentationColumn (offset);
		}

        public TextEditor()
            : this(new TextDocument(""))
        {
        }

        public TextEditor(TextDocument document)
        {
            this.document = document;

            document.TextReplaced += (sender, args) =>
            {
                UpdateContentSize();
            };

            CaretAnimation = new CaretAnimation();
            Caret = new Caret(this);
            Caret.Line = 1;
            Options = new TextEditorOptions();
            Content = textArea = new TextArea(this);

            CaretAnimation.Callback = () =>
            {
                //RedrawLine(Caret.Line);
            };
            CaretAnimation.Start();

            Caret.PositionChanged += (sender, e) =>
            {
                QueueDraw();
            };
            
            KeyPressed += textArea.HandleKeyPressed;
            TextInput += textArea.HandleTextInput;
        }

		public bool ASCIIOnly
		{
			get
			{
				return textArea.ASCIIOnly;
			}
			set
			{
				textArea.ASCIIOnly = value;
			}
		}
        internal int GetWidth()
        {
            return (int)this.HorizontalScrollControl.UpperValue;
        }

        public bool CaretVisible
        {
            get { return CaretAnimation.CaretState; }
        }

        public void ResetCaretState()
        {
            CaretAnimation.Restart();
            QueueDraw();
        }

        public void QueueDraw()
        {
            textArea.QueueDraw();
        }

        void UpdateContentSize()
        {
            //HACK: Workaround for https://github.com/mono/xwt/issues/273
            //Consider replacing by QueueForReallocate

            base.OnReallocate();
            Content = new Button();
            Content = textArea;
        }

        public void RedrawLine(int line)
        {
            textArea.RedrawLine(line);
        }

        public void RedrawLines(int start, int end)
        {
            textArea.RedrawLines(start, end);
        }

        public void RedrawPosition(int line, int column)
        {
            textArea.RedrawPosition(line, column);
        }

        public void RegisterMouseMotionTracker(PointerButton releaseButton, Action<double, double> callback)
        {
            textArea.RegisterMouseMotionTracker(releaseButton, callback);
        }

        private void HandleDocumentLineChanged(object sender, LineEventArgs e)
        {
            UpdateContentSize();
        }
    }
}
