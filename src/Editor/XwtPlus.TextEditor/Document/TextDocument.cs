using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Editor;
using Mono.TextEditor.Highlighting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XwtPlus.TextEditor
{
    public class TextDocument
    {
        readonly IBuffer buffer;
        readonly ILineSplitter splitter;

        ISyntaxMode syntaxMode = null;
        TextSourceVersionProvider versionProvider = new TextSourceVersionProvider();

        bool readOnly;
        ReadOnlyCheckDelegate readOnlyCheckDelegate;

        readonly object syncObject = new object();

        string mimeType;
        public string MimeType {
			get {
				return mimeType;
			}
			set {
				if (mimeType != value) {
					lock (this) {
						mimeType = value;
						SyntaxMode = SyntaxModeService.GetSyntaxMode (this, value);
					}
				}
			}
		}

        public ISyntaxMode SyntaxMode {
			get {
				return syntaxMode ?? new SyntaxMode (this);
			}
			set {
				if (syntaxMode != null)
					syntaxMode.Document = null;
				var old = syntaxMode;
				syntaxMode = value;
				if (syntaxMode != null)
					syntaxMode.Document = this;
				OnSyntaxModeChanged (new SyntaxModeChangeEventArgs (old, syntaxMode));
			}
		}

		protected virtual void OnSyntaxModeChanged (SyntaxModeChangeEventArgs e)
		{
			var handler = SyntaxModeChanged;
			if (handler != null)
				handler (this, e);
		}

		public event EventHandler<SyntaxModeChangeEventArgs> SyntaxModeChanged;

        public bool SuppressHighlightUpdate { get; set; }

        public string Text
        {
            get { return buffer.Text; }
            set
            {
                var args = new DocumentChangeEventArgs (0, Text, value);
                OnTextReplacing (args);
                buffer.Text = value;
                splitter.Initalize(value);
                OnTextReplaced (args);
                versionProvider = new TextSourceVersionProvider ();
                OnTextSet (EventArgs.Empty);
            }
        }

        public TextDocument()
        {
            buffer = new GapBuffer();
            splitter = new LineSplitter();
            splitter.LineChanged += SplitterLineSegmentTreeLineChanged;
        }

        public TextDocument(string text) : this()
        {
            Text = text;
        }

        protected virtual void OnTextReplaced(DocumentChangeEventArgs args)
        {
            if (TextReplaced != null)
                TextReplaced(this, args);
        }

        public event EventHandler<DocumentChangeEventArgs> TextReplaced;

        protected virtual void OnTextReplacing(DocumentChangeEventArgs args)
        {
            if (TextReplacing != null)
                TextReplacing(this, args);
        }
        public event EventHandler<DocumentChangeEventArgs> TextReplacing;

        protected virtual void OnTextSet(EventArgs e)
        {
            EventHandler handler = this.TextSet;
            if (handler != null)
                handler(this, e);
        }
        public event EventHandler TextSet;

        public DocumentLine GetLine(int lineNumber)
        {
            if (lineNumber < DocumentLocation.MinLine)
                return null;

            return splitter.Get(lineNumber);
        }

        public DocumentLine GetLineByOffset(int offset)
        {
            return splitter.GetLineByOffset(offset);
        }

        public int LocationToOffset(int line, int column)
        {
            return LocationToOffset(new DocumentLocation(line, column));
        }

        public int LocationToOffset(DocumentLocation location)
        {
            //			if (location.Column < DocumentLocation.MinColumn)
            //				throw new ArgumentException ("column < MinColumn");
            if (location.Line > this.splitter.Count || location.Line < DocumentLocation.MinLine)
                return -1;
            DocumentLine line = GetLine(location.Line);
            return System.Math.Min(TextLength, line.Offset + System.Math.Max(0, System.Math.Min(line.Length, location.Column - 1)));
        }

        public DocumentLocation OffsetToLocation(int offset)
        {
            int lineNr = splitter.OffsetToLineNumber(offset);
            if (lineNr < DocumentLocation.MinLine)
                return DocumentLocation.Empty;
            DocumentLine line = GetLine(lineNr);
            var col = System.Math.Max(1, System.Math.Min(line.LengthIncludingDelimiter, offset - line.Offset) + 1);
            return new DocumentLocation(lineNr, col);
        }

        public string GetTextAt(int offset, int count)
        {
            if (offset < 0)
                throw new ArgumentException("startOffset < 0");
            if (offset > TextLength)
                throw new ArgumentException("startOffset > Length");
            if (count < 0)
                throw new ArgumentException("count < 0");
            if (offset + count > TextLength)
                throw new ArgumentException("offset + count is beyond EOF");
            return buffer.GetTextAt(offset, count);
        }

        public string GetTextAt(DocumentRegion region)
        {
            return GetTextAt(region.GetSegment(this));
        }

        public string GetTextAt(TextSegment segment)
        {
            return GetTextAt(segment.Offset, segment.Length);
        }

        public char GetCharAt(int offset)
        {
            return buffer.GetCharAt(offset);
        }

        /// <summary>
        /// Gets the line text without the delimiter.
        /// </summary>
        /// <returns>
        /// The line text.
        /// </returns>
        /// <param name='line'>
        /// The line number.
        /// </param>
        public string GetLineText(int line)
        {
            var lineSegment = GetLine(line);
            return lineSegment != null ? GetTextAt(lineSegment.Offset, lineSegment.Length) : null;
        }

        public int LineCount {
			get {
				return splitter.Count;
			}
		}

        public IEnumerable<DocumentLine> GetLinesStartingAt(int startLine)
        {
            return splitter.GetLinesStartingAt(startLine);
        }

        void SplitterLineSegmentTreeLineChanged (object sender, LineEventArgs e)
		{
			if (LineChanged != null)
				LineChanged (this, e);
		}

        public event EventHandler<LineEventArgs> LineChanged;

        public int TextLength
        {
            get
            {
                return buffer.TextLength;
            }
        }

        public int GetOffset (ICSharpCode.NRefactory.TextLocation location)
		{
			return LocationToOffset (location.Line, location.Column);
		}

        public void Insert(int offset, string text, ICSharpCode.NRefactory.Editor.AnchorMovementType anchorMovementType = AnchorMovementType.Default)
        {
            Replace(offset, 0, text, anchorMovementType);
        }

        public void Remove(int offset, int count)
        {
            Replace(offset, count, null);
        }

        public void Remove(TextSegment segment)
        {
            Remove(segment.Offset, segment.Length);
        }

        public void Replace(int offset, int count, string value)
        {
            Replace(offset, count, value, AnchorMovementType.Default);
        }

        public void Replace(int offset, int count, string value, ICSharpCode.NRefactory.Editor.AnchorMovementType anchorMovementType = AnchorMovementType.Default)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", "must be > 0, was: " + offset);
            if (offset > TextLength)
                throw new ArgumentOutOfRangeException("offset", "must be <= Length, was: " + offset);
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "must be > 0, was: " + count);

            //InterruptFoldWorker();
            //int oldLineCount = LineCount;
            var args = new DocumentChangeEventArgs(offset, count > 0 ? GetTextAt(offset, count) : "", value, anchorMovementType);
            OnTextReplacing(args);
            value = args.InsertedText.Text;
            /*UndoOperation operation = null;
            if (!isInUndo)
            {
                operation = new UndoOperation(args);
                if (currentAtomicOperation != null)
                {
                    currentAtomicOperation.Add(operation);
                }
                else
                {
                    OnBeginUndo();
                    undoStack.Push(operation);
                    OnEndUndo(new UndoOperationEventArgs(operation));
                }
                redoStack.Clear();
            }*/

            buffer.Replace(offset, count, value);
            //foldSegmentTree.UpdateOnTextReplace(this, args);
            splitter.TextReplaced(this, args);
            versionProvider.AppendChange(args);
            OnTextReplaced(args);
        }

        public virtual ITextSourceVersion Version
        {
            get
            {
                return versionProvider.CurrentVersion;
            }
        }

        public int OffsetToLineNumber(int offset)
        {
            return splitter.OffsetToLineNumber(offset);
        }

        
		#region Update logic
		List<DocumentUpdateRequest> updateRequests = new List<DocumentUpdateRequest> ();
		
		public IEnumerable<DocumentUpdateRequest> UpdateRequests {
			get {
				return updateRequests;
			}
		}
		// Use CanEdit (int lineNumber) instead for getting a request
		// if a part of a document can be read. ReadOnly should generally not be used
		// for deciding, if a document is readonly or not.
		public bool ReadOnly {
			get {
				return readOnly;
			}
			set {
				readOnly = value;
			}
		}
		
		public ReadOnlyCheckDelegate ReadOnlyCheckDelegate {
			get { return readOnlyCheckDelegate; }
			set { readOnlyCheckDelegate = value; }
		}


		public void RequestUpdate (DocumentUpdateRequest request)
		{
			lock (syncObject) {
				updateRequests.Add (request);
			}
		}
		
		public void CommitDocumentUpdate ()
		{
			lock (syncObject) {
				if (DocumentUpdated != null)
					DocumentUpdated (this, EventArgs.Empty);
				updateRequests.Clear ();
			}
		}
		
		public void CommitLineUpdate (int line)
		{
			RequestUpdate (new LineUpdate (line));
			CommitDocumentUpdate ();
		}
		
		public void CommitLineUpdate (DocumentLine line)
		{
			CommitLineUpdate (line.LineNumber);
		}

		public void CommitUpdateAll ()
		{
			RequestUpdate (new UpdateAll ());
			CommitDocumentUpdate ();
		}

		public void CommitMultipleLineUpdate (int start, int end)
		{
			RequestUpdate (new MultipleLineUpdate (start, end));
			CommitDocumentUpdate ();
		}
		
		public event EventHandler DocumentUpdated;
		#endregion
	

        Stack<OperationType> currentAtomicUndoOperationType =  new Stack<OperationType> ();
		int atomicUndoLevel;

		public bool IsInAtomicUndo {
			get {
				return atomicUndoLevel > 0;
			}
		}

		public OperationType CurrentAtomicUndoOperationType {
			get {
				return currentAtomicUndoOperationType.Count > 0 ?  currentAtomicUndoOperationType.Peek () : OperationType.Undefined;
			}
		}
    }

    public delegate bool ReadOnlyCheckDelegate(int line);
}
