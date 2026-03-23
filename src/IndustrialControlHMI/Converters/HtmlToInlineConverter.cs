using System.Text.RegularExpressions;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace IndustrialControlHMI.Converters;

/// <summary>
/// 将HTML样式的文本转换为Inline集合的转换器
/// 支持简单的HTML标签：<b>, <i>, <u>, <br/>
/// </summary>
public class HtmlToInlineConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string html)
            return null;

        var textBlock = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            FontFamily = new FontFamily("Consolas, Courier New, Monospace")
        };

        var inlines = textBlock.Inlines;
        ParseHtml(html, inlines);
        
        return textBlock;
    }

    private void ParseHtml(string html, InlineCollection inlines)
    {
        int pos = 0;
        while (pos < html.Length)
        {
            if (html[pos] == '<')
            {
                int endTag = html.IndexOf('>', pos);
                if (endTag == -1)
                    break;

                string tag = html.Substring(pos + 1, endTag - pos - 1).Trim();
                pos = endTag + 1;

                if (tag.StartsWith("/"))
                {
                    // 结束标签，暂时忽略（因为我们不使用嵌套）
                    continue;
                }

                if (tag == "br/")
                {
                    inlines.Add(new LineBreak());
                }
                else if (tag == "b")
                {
                    int closePos = html.IndexOf("</b>", pos, StringComparison.OrdinalIgnoreCase);
                    if (closePos != -1)
                    {
                        string content = html.Substring(pos, closePos - pos);
                        var bold = new Run(content) { FontWeight = FontWeights.Bold };
                        inlines.Add(bold);
                        pos = closePos + 4;
                    }
                }
                else if (tag == "i")
                {
                    int closePos = html.IndexOf("</i>", pos, StringComparison.OrdinalIgnoreCase);
                    if (closePos != -1)
                    {
                        string content = html.Substring(pos, closePos - pos);
                        var italic = new Run(content) { FontStyle = FontStyles.Italic };
                        inlines.Add(italic);
                        pos = closePos + 4;
                    }
                }
                else if (tag == "u")
                {
                    int closePos = html.IndexOf("</u>", pos, StringComparison.OrdinalIgnoreCase);
                    if (closePos != -1)
                    {
                        string content = html.Substring(pos, closePos - pos);
                        var underline = new Run(content) { TextDecorations = TextDecorations.Underline };
                        inlines.Add(underline);
                        pos = closePos + 4;
                    }
                }
                else if (tag.StartsWith("b style="))
                {
                    int closePos = html.IndexOf("</b>", pos, StringComparison.OrdinalIgnoreCase);
                    if (closePos != -1)
                    {
                        string content = html.Substring(pos, closePos - pos);
                        var bold = new Run(content) { FontWeight = FontWeights.Bold };
                        
                        // 提取样式颜色
                        var colorMatch = System.Text.RegularExpressions.Regex.Match(tag, "color:([^;]+)");
                        if (colorMatch.Success)
                        {
                            string colorStr = colorMatch.Groups[1].Value.Trim();
                            try
                            {
                                var color = (Color)ColorConverter.ConvertFromString(colorStr);
                                bold.Foreground = new SolidColorBrush(color);
                            }
                            catch { }
                        }
                        
                        inlines.Add(bold);
                        pos = closePos + 4;
                    }
                }
                else
                {
                    // 未知标签，当作普通文本
                    continue;
                }
            }
            else
            {
                int nextTag = html.IndexOf('<', pos);
                if (nextTag == -1)
                    nextTag = html.Length;

                string text = html.Substring(pos, nextTag - pos);
                inlines.Add(new Run(text));
                pos = nextTag;
            }
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}