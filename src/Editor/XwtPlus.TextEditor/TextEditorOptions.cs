using Mono.TextEditor.Highlighting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xwt.Drawing;

namespace XwtPlus.TextEditor
{
    public class TextEditorOptions
    {
		public static string CutText = "Cut";
		public static string CopyText = "Copy";
		public static string PasteText = "Paste";
		public static string SelectAllText = "Select All";

        public Font EditorFont = Font.SystemMonospaceFont;
        public IndentStyle IndentStyle = IndentStyle.Auto;
        public int TabSize = 4;
        public Color Background = Colors.White;
        public ColorScheme ColorScheme = SyntaxModeService.DefaultColorStyle;
        public bool CurrentLineNumberBold = true;
    }
}
