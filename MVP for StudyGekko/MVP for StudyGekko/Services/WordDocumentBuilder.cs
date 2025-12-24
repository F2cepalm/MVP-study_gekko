using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MVP_for_StudyGekko.Models;

namespace MVP_for_StudyGekko.Services;

public class WordDocumentBuilder : IDocumentBuilder
{
    private DocumentRequirements _requirements = new();

    public Task<byte[]> BuildAsync(WorkResult result, WorkRequest request, DocumentRequirements? requirements = null)
    {
        // Используем переданные требования или значения по умолчанию
        _requirements = requirements ?? new DocumentRequirements();

        using var stream = new MemoryStream();

        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // Настройки страницы с поддержкой требований
            var sectionProps = new SectionProperties();
            var pageMargin = new PageMargin
            {
                Top = GetIntValue(_requirements.Page?.TopMargin, 1134),      // 2 см по умолчанию
                Bottom = GetIntValue(_requirements.Page?.BottomMargin, 1134),   // 2 см
                Left = GetIntValue(_requirements.Page?.LeftMargin, 1701),     // 3 см
                Right = GetIntValue(_requirements.Page?.RightMargin, 850)      // 1.5 см
            };
            sectionProps.Append(pageMargin);

            // Заголовок
            body.Append(CreateTitle(request.Topic));
            body.Append(CreateEmptyParagraph());

            // Контент по секциям
            var sections = ParseContent(result.Content);
            foreach (var section in sections)
            {
                if (section.IsHeading)
                {
                    body.Append(CreateHeading(section.Text));
                }
                else
                {
                    body.Append(CreateParagraph(section.Text));
                }
            }

            body.Append(sectionProps);
        }

        return Task.FromResult(stream.ToArray());
    }

    private Paragraph CreateTitle(string text)
    {
        var paragraph = new Paragraph();

        var alignment = GetAlignment(_requirements.Title?.Alignment, "center");
        var spaceAfter = GetStringValue(_requirements.Title?.SpaceAfter, "200");

        var paragraphProps = new ParagraphProperties
        {
            Justification = new Justification { Val = alignment },
            SpacingBetweenLines = new SpacingBetweenLines { After = spaceAfter }
        };
        paragraph.Append(paragraphProps);

        var run = new Run();
        var fontSize = GetStringValue(_requirements.Title?.FontSize, "32");
        var fontName = GetStringValue(_requirements.Font?.Name, "Times New Roman");
        var isBold = GetBoolValue(_requirements.Title?.Bold, true);

        var runProps = new RunProperties
        {
            FontSize = new FontSize { Val = fontSize },
            RunFonts = new RunFonts { Ascii = fontName, HighAnsi = fontName }
        };

        if (isBold)
        {
            runProps.Bold = new Bold();
        }

        run.Append(runProps);
        run.Append(new Text(text));

        paragraph.Append(run);
        return paragraph;
    }

    private Paragraph CreateHeading(string text)
    {
        var paragraph = new Paragraph();

        var spaceBefore = GetStringValue(_requirements.Heading?.SpaceBefore, "300");
        var spaceAfter = GetStringValue(_requirements.Heading?.SpaceAfter, "150");
        var alignment = GetAlignment(_requirements.Heading?.Alignment, "left");

        var paragraphProps = new ParagraphProperties
        {
            SpacingBetweenLines = new SpacingBetweenLines { Before = spaceBefore, After = spaceAfter },
            Justification = new Justification { Val = alignment }
        };
        paragraph.Append(paragraphProps);

        var run = new Run();
        var fontSize = GetStringValue(_requirements.Heading?.FontSize, "28");
        var fontName = GetStringValue(_requirements.Font?.Name, "Times New Roman");
        var isBold = GetBoolValue(_requirements.Heading?.Bold, true);

        var runProps = new RunProperties
        {
            FontSize = new FontSize { Val = fontSize },
            RunFonts = new RunFonts { Ascii = fontName, HighAnsi = fontName }
        };

        if (isBold)
        {
            runProps.Bold = new Bold();
        }

        run.Append(runProps);
        run.Append(new Text(text));

        paragraph.Append(run);
        return paragraph;
    }

    private Paragraph CreateParagraph(string text)
    {
        var paragraph = new Paragraph();

        var alignment = GetAlignment(_requirements.Paragraph?.Alignment, "both");
        var lineSpacing = GetStringValue(_requirements.Paragraph?.LineSpacing, "360");
        var lineSpacingRule = GetLineSpacingRule(_requirements.Paragraph?.LineSpacingRule, "auto");
        var firstLineIndent = GetStringValue(_requirements.Paragraph?.FirstLineIndent, "720");
        var spaceBefore = GetStringValue(_requirements.Paragraph?.SpaceBefore, "0");
        var spaceAfter = GetStringValue(_requirements.Paragraph?.SpaceAfter, "0");

        var paragraphProps = new ParagraphProperties
        {
            Justification = new Justification { Val = alignment },
            SpacingBetweenLines = new SpacingBetweenLines
            {
                Line = lineSpacing,
                LineRule = lineSpacingRule,
                Before = spaceBefore,
                After = spaceAfter
            },
            Indentation = new Indentation { FirstLine = firstLineIndent }
        };
        paragraph.Append(paragraphProps);

        var run = new Run();
        var fontSize = GetStringValue(_requirements.Font?.Size, "28");
        var fontName = GetStringValue(_requirements.Font?.Name, "Times New Roman");

        var runProps = new RunProperties
        {
            FontSize = new FontSize { Val = fontSize },
            RunFonts = new RunFonts { Ascii = fontName, HighAnsi = fontName }
        };
        run.Append(runProps);
        run.Append(new Text(text));

        paragraph.Append(run);
        return paragraph;
    }

    private Paragraph CreateEmptyParagraph()
    {
        return new Paragraph(new Run(new Text("")));
    }

    private List<ContentSection> ParseContent(string content)
    {
        var sections = new List<ContentSection>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("## "))
            {
                sections.Add(new ContentSection
                {
                    Text = trimmed.Replace("## ", ""),
                    IsHeading = true
                });
            }
            else if (trimmed.StartsWith("# "))
            {
                sections.Add(new ContentSection
                {
                    Text = trimmed.Replace("# ", ""),
                    IsHeading = true
                });
            }
            else if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("-") && !trimmed.StartsWith("*"))
            {
                sections.Add(new ContentSection
                {
                    Text = trimmed,
                    IsHeading = false
                });
            }
        }

        return sections;
    }

    private class ContentSection
    {
        public string Text { get; set; } = string.Empty;
        public bool IsHeading { get; set; }
    }

    // Вспомогательные методы для работы с требованиями

    /// <summary>Получить строковое значение или значение по умолчанию</summary>
    private string GetStringValue(string? value, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            return defaultValue;
        }
        return value;
    }

    /// <summary>Получить целочисленное значение или значение по умолчанию</summary>
    private int GetIntValue(string? value, int defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            return defaultValue;
        }

        if (int.TryParse(value, out var result))
        {
            return result;
        }

        return defaultValue;
    }

    /// <summary>Получить булево значение или значение по умолчанию</summary>
    private bool GetBoolValue(string? value, bool defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("default", StringComparison.OrdinalIgnoreCase))
        {
            return defaultValue;
        }

        if (bool.TryParse(value, out var result))
        {
            return result;
        }

        return defaultValue;
    }

    /// <summary>Получить значение выравнивания</summary>
    private JustificationValues GetAlignment(string? value, string defaultValue)
    {
        var alignment = GetStringValue(value, defaultValue).ToLowerInvariant();

        return alignment switch
        {
            "left" => JustificationValues.Left,
            "right" => JustificationValues.Right,
            "center" => JustificationValues.Center,
            "both" or "justify" => JustificationValues.Both,
            _ => JustificationValues.Both
        };
    }

    /// <summary>Получить правило межстрочного интервала</summary>
    private LineSpacingRuleValues GetLineSpacingRule(string? value, string defaultValue)
    {
        var rule = GetStringValue(value, defaultValue).ToLowerInvariant();

        return rule switch
        {
            "auto" => LineSpacingRuleValues.Auto,
            "exact" => LineSpacingRuleValues.Exact,
            "atleast" or "at-least" => LineSpacingRuleValues.AtLeast,
            _ => LineSpacingRuleValues.Auto
        };
    }
}