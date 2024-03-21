using System;
using System.Xml.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;

namespace HtmlToLabel;

public static class HtmlToLabel
{
    public static async void Convert(Label l, string html, bool ignoreNewLines = true, bool hasStyle = true)
    {
        l.Text = "";
        FormattedString fstring = new FormattedString();
        try
        {
            var xml = $@"<html>{html}</html>";

            XElement root = XElement.Parse(xml);

            ProcessNodes(fstring, root, new StyleContainer(l), ignoreNewLines, l, hasStyle);

        }
        catch (Exception ex)
        {
            fstring.Spans.Add(new Span
            {
                Text = $"Error: {ex.Message}",
                TextColor = Colors.Red,
                FontAttributes = FontAttributes.Bold
            });
        }
        l.FormattedText = fstring;
    }

    private static void ProcessNodes(FormattedString fstring, XElement xe, StyleContainer cont, bool ignoreNewLines, Label l, bool hasStyle)
    {
        foreach (var node in xe.Nodes())
        {
            var element = node as XElement;
            switch (node.NodeType)
            {
                case System.Xml.XmlNodeType.Element:
                    if (element.Name.LocalName.ToLower() == "p")
                    {
                        var boldCont = cont.Clone();
                        ProcessStyles(element, boldCont, hasStyle);
                        ProcessNodes(fstring, element, boldCont, ignoreNewLines, l, hasStyle);
                        fstring.Spans[fstring.Spans.Count - 1].Text += Environment.NewLine;
                    }
                    else if (element.Name.LocalName.ToLower() == "a")
                    {
                        var boldCont = cont.Clone();
                        ProcessStyles(element, boldCont, hasStyle);
                        boldCont.Decorations |= TextDecorations.Underline;
                        ProcessUrl(element, boldCont);
                        ProcessNodes(fstring, element, boldCont, ignoreNewLines, l, hasStyle);
                    }
                    else if (element.Name.LocalName.ToLower() == "b")
                    {
                        var boldCont = cont.Clone();
                        boldCont.FontAttributes |= FontAttributes.Bold;
                        ProcessStyles(element, boldCont, hasStyle);
                        ProcessNodes(fstring, element, boldCont, ignoreNewLines, l, hasStyle);
                    }
                    else if (element.Name.LocalName.ToLower() == "i")
                    {
                        var boldCont = cont.Clone();
                        ProcessStyles(element, boldCont, hasStyle);
                        boldCont.FontAttributes |= FontAttributes.Italic;
                        ProcessNodes(fstring, element, boldCont, ignoreNewLines, l, hasStyle);
                    }
                    else if (element.Name.LocalName.ToLower() == "u")
                    {
                        var boldCont = cont.Clone();
                        ProcessStyles(element, boldCont, hasStyle);
                        boldCont.Decorations |= TextDecorations.Underline;
                        ProcessNodes(fstring, element, boldCont, ignoreNewLines, l, hasStyle);
                    }
                    else if (element.Name.LocalName.ToLower() == "br")
                    {
                        fstring.Spans[fstring.Spans.Count - 1].Text += Environment.NewLine;
                    }
                    else
                    {
                        var boldCont = cont.Clone();
                        ProcessStyles(element, boldCont, hasStyle);
                        ProcessNodes(fstring, element, boldCont, ignoreNewLines, l, hasStyle);
                    }
                    break;
                case System.Xml.XmlNodeType.Text:
                    var span = cont.ToSpan();
                    var txt = node.ToString();
                    if (ignoreNewLines)
                        txt = txt.Replace("\n", "").Replace("\r", "");
                    span.Text = txt;
                    fstring.Spans.Add(span);
                    break;
                default:
                    break;
            }

        }
    }

    private static void ProcessStyles(XElement element, StyleContainer styleCont, bool hasStyle)
    {
        if (!hasStyle) return;
        if (!element.HasAttributes) return;
        var styleStr = element.Attribute(XName.Get("style"))?.Value;
        if (string.IsNullOrEmpty(styleStr)) return;
        styleStr = styleStr.Replace(" ", "");
        var styles = styleStr.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
        foreach (var styleEntry in styles)
        {
            var parts = styleEntry.ToLower().Split(":".ToCharArray());
            if (parts[0] == "background-color")
            {
                styleCont.BackgroundColor = ReadHexColor(parts[1]);
            }
            else if (parts[0] == "color")
            {
                styleCont.ForegroundColor = ReadHexColor(parts[1]);
            }
            else if (parts[0] == "font-weight")
            {
                if (parts[1] == "bold")
                {
                    styleCont.FontAttributes |= FontAttributes.Bold;
                }
                if (parts[1] == "normal")
                {
                    styleCont.FontAttributes &= ~FontAttributes.Bold;
                }
            }
            else if (parts[0] == "font-style")
            {
                if (parts[1] == "italic")
                {
                    styleCont.FontAttributes |= FontAttributes.Bold;
                }
                if (parts[1] == "normal")
                {
                    styleCont.FontAttributes &= ~FontAttributes.Bold;
                }
            }
            else if (parts[0] == "font-family")
            {
                styleCont.FontFamily = parts[1];
            }
            else if (parts[0] == "font-size")
            {
                styleCont.FontSize = double.Parse(Regex.Replace(parts[1], @"\D", ""));
            }
        }
    }

    private static void ProcessUrl(XElement element, StyleContainer styleCont)
    {
        if (!element.HasAttributes) return;
        var url = element.Attribute(XName.Get("href"))?.Value;
        if (string.IsNullOrEmpty(url) || url == "#") return;
        styleCont.TapGesture = new TapGestureRecognizer
        {
            Command = new Command(async () =>
        {
            try
            {
                Uri uri = new Uri(url);
                await Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
            }
            catch (Exception ex)
            {
                // An unexpected error occurred. No browser may be installed on the device.
            }
        })
        };
        return;
    }

    private static Color ReadHexColor(string strColor)
    {
        return Color.Parse(strColor);
    }

    private class StyleContainer
    {
        public Color BackgroundColor { get; set; }
        //public Font Font { get; set; }
        public Color ForegroundColor { get; set; }
        public FontAttributes FontAttributes { get; set; }
        //public Font Font { get; private set; }
        public string FontFamily { get; set; }
        public double FontSize { get; set; }
        public TextDecorations Decorations { get; set; }
        public TapGestureRecognizer TapGesture { get; set; }

        public StyleContainer Clone()
        {
            var clone = new StyleContainer();

            if (this.BackgroundColor != Colors.Transparent)
                clone.BackgroundColor = this.BackgroundColor;

            if (this.ForegroundColor != Colors.Black)
                clone.ForegroundColor = this.ForegroundColor;

            clone.FontAttributes = this.FontAttributes;
            clone.FontFamily = this.FontFamily;
            clone.FontSize = this.FontSize;
            clone.Decorations = this.Decorations;
            clone.TapGesture = this.TapGesture;

            return clone;
        }

        public Span ToSpan()
        {
            var span = new Span
            {
                BackgroundColor = this.BackgroundColor,
                //Font = this.Font,
                TextColor = this.ForegroundColor,
                FontAttributes = this.FontAttributes,
                FontFamily = this.FontFamily,
                FontSize = this.FontSize,
                TextDecorations = this.Decorations                
            };
            if (this.TapGesture != null)
                span.GestureRecognizers.Add(this.TapGesture);
            return span;
        }

        public StyleContainer()
        {

        }

        public StyleContainer(Label l)
        {
            this.BackgroundColor = l.BackgroundColor;
            this.ForegroundColor = l.TextColor;
            this.FontAttributes = l.FontAttributes;
            this.FontFamily = l.FontFamily;
            this.FontSize = l.FontSize;
            this.Decorations = l.TextDecorations;
            this.TapGesture = null;
        }
    }
}
