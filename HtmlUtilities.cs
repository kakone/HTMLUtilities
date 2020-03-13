using System.IO;
using HtmlAgilityPack;

namespace Html
{
    /// <summary>
    /// Html utilities
    /// </summary>
    public static class HtmlUtilities
    {
        /// <summary>
        /// Converts HTML to plain text / strips tags
        /// </summary>
        /// <param name="html">The HTML</param>
        /// <returns>the HTML converted to plain text</returns>
        public static string ConvertToPlainText(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            using var sw = new StringWriter { NewLine = "\r\n" };
            ConvertTo(doc.DocumentNode, sw);
            sw.Flush();
            return sw.ToString();
        }

        private static bool ConvertContentTo(HtmlNode node, TextWriter outText)
        {
            var result = false;
            foreach (var subnode in node.ChildNodes)
            {
                result |= ConvertTo(subnode, outText);
            }
            return result;
        }

        private static bool ConvertTo(HtmlNode node, TextWriter outText)
        {
            var result = false;
            switch (node.NodeType)
            {
                case HtmlNodeType.Document:
                    result = ConvertContentTo(node, outText);
                    break;

                case HtmlNodeType.Text:
                    // script and style must not be output
                    var parentName = node.ParentNode.Name;
                    if (parentName == "script" || parentName == "style")
                    {
                        break;
                    }

                    // get text
                    var html = ((HtmlTextNode)node).Text;
                    // is it in fact a special closing node output as text?
                    if (HtmlNode.IsOverlappedClosingElement(html))
                    {
                        break;
                    }

                    var text = HtmlEntity.DeEntitize(html.Replace("\r\n", " ").Replace("\n", " ").Trim());
                    if (string.IsNullOrEmpty(text))
                    {
                        break;
                    }

                    result = true;
                    outText.Write(text);
                    break;

                case HtmlNodeType.Element:
                    switch (node.Name)
                    {
                        case "br":
                            outText.WriteLine();
                            return false;
                        case "hr":
                            outText.WriteLine(new string('_', 32));
                            return false;
                        case "img":
                            var alt = node.GetAttributeValue("alt", null).Trim();
                            if (!string.IsNullOrEmpty(alt))
                            {
                                result = true;
                                outText.Write($"[{alt}]");
                            }
                            break;
                        case "li":
                            outText.Write("- ");
                            break;
                    }

                    if (node.HasChildNodes)
                    {
                        result = ConvertContentTo(node, outText);
                    }

                    if (result)
                    {
                        switch (node.Name)
                        {
                            case "p":
                            case "div":
                            case "tr":
                            case "li":
                                outText.WriteLine();
                                break;
                            case "a":
                                var href = node.GetAttributeValue("href", null);
                                if (!string.IsNullOrEmpty(href))
                                {
                                    outText.Write($"<{href}>");
                                }
                                break;
                        }
                    }
                    break;
            }

            return result;
        }
    }
}
