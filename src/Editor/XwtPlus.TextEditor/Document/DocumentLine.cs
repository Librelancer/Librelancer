using Mono.TextEditor.Highlighting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XwtPlus.TextEditor.Margins;

namespace XwtPlus.TextEditor
{
    public abstract class DocumentLine : ICSharpCode.NRefactory.Editor.IDocumentLine
    {
        CloneableStack<Span> startSpan;
        static readonly CloneableStack<Span> EmptySpan = new CloneableStack<Span>();

        public CloneableStack<Span> StartSpan
        {
            get
            {
                return startSpan ?? EmptySpan;
            }
            set
            {
                startSpan = value != null && value.Count == 0 ? null : value;
            }
        }

        /// <summary>
        /// Gets the length of the line.
        /// </summary>
        /// <remarks>The length does not include the line delimeter.</remarks>
        public int Length
        {
            get
            {
                return LengthIncludingDelimiter - DelimiterLength;
            }
        }

        /// <summary>
        /// Gets the start offset of the line.
        /// </summary>
        public abstract int Offset { get; set; }

        /// <summary>
        /// Gets the number of this line.
        /// The first line has the number 1.
        /// </summary>
        public abstract int LineNumber { get; }

        /// <summary>
        /// Gets the next line. Returns null if this is the last line in the document.
        /// </summary>
        public abstract DocumentLine NextLine { get; }

        /// <summary>
        /// Gets the previous line. Returns null if this is the first line in the document.
        /// </summary>
        public abstract DocumentLine PreviousLine { get; }

        /// <summary>
		/// Gets the end offset of the line.
		/// </summary>
		/// <remarks>The end offset does not include the line delimeter.</remarks>
		public int EndOffset {
			get {
				return Offset + Length;
			}
		}

        /// <summary>
        /// Gets the end offset of the line including the line delimiter.
        /// </summary>
        public int EndOffsetIncludingDelimiter
        {
            get
            {
                return Offset + LengthIncludingDelimiter;
            }
        }

        /// <summary>
        /// Gets the length of the line terminator.
        /// Returns 1 or 2; or 0 at the end of the document.
        /// </summary>
        public int DelimiterLength
        {
            get;
            set;
        }

        public bool WasChanged
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the length of the line including the line delimiter.
        /// </summary>
        public int LengthIncludingDelimiter
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the text segment of the line.
        /// </summary>
        /// <remarks>The text segment does not include the line delimeter.</remarks>
        public TextSegment Segment
        {
            get
            {
                return new TextSegment(Offset, Length);
            }
        }

        protected DocumentLine(int length, int delimiterLength)
        {
            LengthIncludingDelimiter = length;
            DelimiterLength = delimiterLength;
        }

        public int GetLogicalColumn(TextEditor editor, int visualColumn)
        {
            int curVisualColumn = 1;
            int offset = Offset;
            int max = offset + Length;
            for (int i = offset; i < max; i++)
            {
                if (i < editor.Document.TextLength && editor.Document.GetCharAt(i) == '\t')
                {
                    curVisualColumn = TextViewMargin.GetNextTabstop(editor, curVisualColumn);
                }
                else
                {
                    curVisualColumn++;
                }
                if (curVisualColumn > visualColumn)
                    return i - offset + 1;
            }
            return Length + (visualColumn - curVisualColumn) + 1;
        }

        public int GetVisualColumn(TextEditor editor, int logicalColumn)
        {
            int result = 1;
            int offset = Offset;
            if (editor.Options.IndentStyle == IndentStyle.Virtual && Length == 0 && logicalColumn > DocumentLocation.MinColumn)
            {
                foreach (char ch in editor.GetIndentationString(Offset))
                {
                    if (ch == '\t')
                    {
                        result += editor.Options.TabSize;
                        continue;
                    }
                    result++;
                }
                return result;
            }
            for (int i = 0; i < logicalColumn - 1; i++)
            {
                if (i < Length && editor.Document.GetCharAt(offset + i) == '\t')
                {
                    result = TextViewMargin.GetNextTabstop(editor, result);
                }
                else
                {
                    result++;
                }
            }
            return result;
        }


        /// <summary>
        /// This method gets the line indentation.
        /// </summary>
        /// <param name="doc">
        /// The <see cref="TextDocument"/> the line belongs to.
        /// </param>
        /// <returns>
        /// The indentation of the line (all whitespace chars up to the first non ws char).
        /// </returns>
        public string GetIndentation(TextDocument doc)
        {
            var result = new StringBuilder();
            int offset = Offset;
            int max = System.Math.Min(offset + LengthIncludingDelimiter, doc.TextLength);
            for (int i = offset; i < max; i++)
            {
                char ch = doc.GetCharAt(i);
                if (ch != ' ' && ch != '\t')
                    break;
                result.Append(ch);
            }
            return result.ToString();
        }

        public static implicit operator TextSegment(DocumentLine line)
        {
            return line.Segment;
        }

        #region IDocumentLine implementation
		int ICSharpCode.NRefactory.Editor.IDocumentLine.TotalLength {
			get {
				return LengthIncludingDelimiter;
			}
		}

		ICSharpCode.NRefactory.Editor.IDocumentLine ICSharpCode.NRefactory.Editor.IDocumentLine.PreviousLine {
			get {
				return this.PreviousLine;
			}
		}

		ICSharpCode.NRefactory.Editor.IDocumentLine ICSharpCode.NRefactory.Editor.IDocumentLine.NextLine {
			get {
				return this.NextLine;
			}
		}

		bool ICSharpCode.NRefactory.Editor.IDocumentLine.IsDeleted {
			get {
				return false;
			}
		}
		#endregion
    }
}
