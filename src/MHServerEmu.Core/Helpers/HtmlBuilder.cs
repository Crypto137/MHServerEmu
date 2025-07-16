using System.Text;

namespace MHServerEmu.Core.Helpers
{
    /// <summary>
    /// Interface for data structures that can be represented in HTML using <see cref="HtmlBuilder"/>.
    /// </summary>
    public interface IHtmlDataStructure
    {
        public void BuildHtml(StringBuilder sb);
    }

    /// <summary>
    /// Helper functions to build HTML using <see cref="StringBuilder"/>.
    /// </summary>
    public static class HtmlBuilder
    {
        #region Headers

        public static void AppendHeader1(StringBuilder sb, string text)
        {
            AppendDataLine(sb, "h1", text);
        }

        public static void AppendHeader2(StringBuilder sb, string text)
        {
            AppendDataLine(sb, "h2", text);
        }

        public static void AppendHeader3(StringBuilder sb, string text)
        {
            AppendDataLine(sb, "h3", text);
        }

        public static void AppendHeader4(StringBuilder sb, string text)
        {
            AppendDataLine(sb, "h4", text);
        }

        public static void AppendHeader5(StringBuilder sb, string text)
        {
            AppendDataLine(sb, "h5", text);
        }

        public static void AppendHeader6(StringBuilder sb, string text)
        {
            AppendDataLine(sb, "h6", text);
        }

        #endregion

        #region Text

        public static void AppendParagraph(StringBuilder sb, string text)
        {
            AppendDataLine(sb, "p", text);
        }

        #endregion

        #region Lists

        public static void BeginUnorderedList(StringBuilder sb)
        {
            sb.AppendLine("<ul>");
        }

        public static void EndUnorderedList(StringBuilder sb)
        {
            sb.AppendLine("</ul>");
        }

        public static void AppendListItem(StringBuilder sb, string item)
        {
            AppendDataLine(sb, "li", item);
        }

        #endregion

        #region Tables

        public static void BeginTable(StringBuilder sb)
        {
            sb.AppendLine("<table>");
        }

        public static void EndTable(StringBuilder sb)
        {
            sb.AppendLine("</table>");
        }

        public static void AppendTableRow(StringBuilder sb, params object[] data)
        {
            sb.Append("<tr>");
            foreach (object dataIt in data)
                AppendTableRowData(sb, dataIt);
            sb.AppendLine("</tr>");
        }

        private static void AppendTableRowData(StringBuilder sb, object data)
        {
            AppendData(sb, "td", data);
        }

        #endregion

        #region Custom Data Structures

        public static void AppendDataStructure<T>(StringBuilder sb, in T htmlBuilder) where T: IHtmlDataStructure
        {
            htmlBuilder.BuildHtml(sb);
        }

        #endregion

        #region Internal Common

        private static void AppendData(StringBuilder sb, string tag, object data)
        {
            sb.AppendFormat("<{0}>{1}</{0}>", tag, data);
        }

        private static void AppendDataLine(StringBuilder sb, string tag, object data)
        {
            sb.AppendFormat("<{0}>{1}</{0}>", tag, data);
            sb.AppendLine();
        }

        #endregion
    }
}
