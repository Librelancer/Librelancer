using System;
using System.Globalization;
using System.Text;

namespace LibreLancer.Data;

public class CommentEscaping
{
    public static string Unescape(string comment)
    {
        if (comment == null) return null;
        if (comment.Length < 1 || comment[0] != '@')
            return comment;
        var sb = new StringBuilder();
        for (int i = 1; i < comment.Length; i++) {
            if (comment[i] == '\\') {
                if (i + 1 >= comment.Length) {
                    sb.Append('\\');
                    continue;
                }
                switch (comment[i + 1]) {
                    case 'E':
                        sb.Append('=');
                        i++;
                        break;
                    case 'C':
                        sb.Append(',');
                        i++;
                        break;
                    case 'S':
                        sb.Append(';');
                        i++;
                        break;
                    case 'n':
                        sb.Append('\n');
                        i++;
                        break;
                    case 't':
                        sb.Append('\t');
                        i++;
                        break;
                    case '\\':
                        sb.Append('\\');
                        i++;
                        break;
                    case 'x' when i + 3 < comment.Length:
                        if (int.TryParse(
                                comment.AsSpan().Slice(i + 2, 2),
                                NumberStyles.HexNumber, null, out int uchar))
                        {
                            sb.Append((char) uchar);
                            i += 3;
                        }
                        else
                        {
                            sb.Append("\\");
                        }
                        continue;
                    default:
                        sb.Append('\\');
                        break;
                }
            }
            else {
                sb.Append(comment[i]);
            }
        }
        return sb.ToString();
    }

    public static string Escape(string comment)
    {
        if (comment == null) return null;
        if (string.IsNullOrWhiteSpace(comment)) return "";
        bool needsEscape = comment.AsSpan().Trim().Length != comment.Length;
        for (int i = 0; i < comment.Length; i++)
        {
            switch (comment[i])
            {
                case '\\':
                case '=':
                case ';':
                case '\n':
                case '\t':
                case ',':
                case var c when c < 32 || c > 126:
                    needsEscape = true;
                    break;
            }
            if (needsEscape) break;
        }
        if (comment[0] != '@' && !needsEscape) return comment;
        var sb = new StringBuilder();
        sb.Append("@");
        for (int i = 0; i < comment.Length; i++) {
            switch (comment[i])
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '=':
                    sb.Append("\\E");
                    break;
                case ';':
                    sb.Append("\\S");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                case ',':
                    sb.Append("\\C");
                    break;
                case var c when c >= 32 && c <= 126:
                    sb.Append(c);
                    break;
                case var c when c < 32 || c > 126:
                    sb.Append("\\x");
                    sb.Append(((uint) c).ToString("X2"));
                    break;
            }
        }
        return sb.ToString();
    }
}
