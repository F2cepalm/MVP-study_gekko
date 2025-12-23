using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MVP_for_StudyGekko.Models;

namespace MVP_for_StudyGekko.Services;

public class WordDocumentBuilder : IDocumentBuilder
{
    public Task<byte[]> BuildAsync(WorkResult result, WorkRequest request)
    {
        using var stream = new MemoryStream();

        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            // Настройки страницы
            var sectionProps = new SectionProperties();
            var pageMargin = new PageMargin
            {
                Top = 1134,      // 2 см
                Bottom = 1134,   // 2 см
                Left = 1701,     // 3 см
                Right = 850      // 1.5 см
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

        var paragraphProps = new ParagraphProperties
        {
            Justification = new Justification { Val = JustificationValues.Center },
            SpacingBetweenLines = new SpacingBetweenLines { After = "200" }
        };
        paragraph.Append(paragraphProps);

        var run = new Run();
        var runProps = new RunProperties
        {
            Bold = new Bold(),
            FontSize = new FontSize { Val = "32" },
            RunFonts = new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" }
        };
        run.Append(runProps);
        run.Append(new Text(text));

        paragraph.Append(run);
        return paragraph;
    }

    private Paragraph CreateHeading(string text)
    {
        var paragraph = new Paragraph();

        var paragraphProps = new ParagraphProperties
        {
            SpacingBetweenLines = new SpacingBetweenLines { Before = "300", After = "150" }
        };
        paragraph.Append(paragraphProps);

        var run = new Run();
        var runProps = new RunProperties
        {
            Bold = new Bold(),
            FontSize = new FontSize { Val = "28" },
            RunFonts = new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" }
        };
        run.Append(runProps);
        run.Append(new Text(text));

        paragraph.Append(run);
        return paragraph;
    }

    private Paragraph CreateParagraph(string text)
    {
        var paragraph = new Paragraph();

        var paragraphProps = new ParagraphProperties
        {
            Justification = new Justification { Val = JustificationValues.Both },
            SpacingBetweenLines = new SpacingBetweenLines { Line = "360", LineRule = LineSpacingRuleValues.Auto },
            Indentation = new Indentation { FirstLine = "720" }
        };
        paragraph.Append(paragraphProps);

        var run = new Run();
        var runProps = new RunProperties
        {
            FontSize = new FontSize { Val = "28" },
            RunFonts = new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" }
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
}