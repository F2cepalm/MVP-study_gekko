namespace MVP_for_StudyGekko.Models;

/// <summary>
/// JSON-структура требований к форматированию документа
/// Все поля опциональны, при отсутствии значения используется "default"
/// </summary>
public class DocumentRequirements
{
    public PageSettings? Page { get; set; } = new();
    public FontSettings? Font { get; set; } = new();
    public ParagraphSettings? Paragraph { get; set; } = new();
    public HeadingSettings? Heading { get; set; } = new();
    public TitleSettings? Title { get; set; } = new();
}

public class PageSettings
{
    /// <summary>Верхний отступ в twips (1/1440 дюйма). Default: "1134" (2см)</summary>
    public string TopMargin { get; set; } = "default";

    /// <summary>Нижний отступ в twips. Default: "1134" (2см)</summary>
    public string BottomMargin { get; set; } = "default";

    /// <summary>Левый отступ в twips. Default: "1701" (3см)</summary>
    public string LeftMargin { get; set; } = "default";

    /// <summary>Правый отступ в twips. Default: "850" (1.5см)</summary>
    public string RightMargin { get; set; } = "default";

    /// <summary>Размер страницы (A4, Letter и т.д.). Default: "A4"</summary>
    public string PageSize { get; set; } = "default";

    /// <summary>Ориентация страницы (portrait, landscape). Default: "portrait"</summary>
    public string Orientation { get; set; } = "default";
}

public class FontSettings
{
    /// <summary>Название шрифта. Default: "Times New Roman"</summary>
    public string Name { get; set; } = "default";

    /// <summary>Размер шрифта для обычного текста в half-points. Default: "28" (14pt)</summary>
    public string Size { get; set; } = "default";

    /// <summary>Цвет шрифта в hex формате. Default: "000000"</summary>
    public string Color { get; set; } = "default";
}

public class ParagraphSettings
{
    /// <summary>Выравнивание (left, right, center, both). Default: "both"</summary>
    public string Alignment { get; set; } = "default";

    /// <summary>Межстрочный интервал в twips. Default: "360" (1.5)</summary>
    public string LineSpacing { get; set; } = "default";

    /// <summary>Правило межстрочного интервала (auto, exact, atLeast). Default: "auto"</summary>
    public string LineSpacingRule { get; set; } = "default";

    /// <summary>Отступ первой строки в twips. Default: "720" (1.25см)</summary>
    public string FirstLineIndent { get; set; } = "default";

    /// <summary>Интервал перед параграфом в twips. Default: "0"</summary>
    public string SpaceBefore { get; set; } = "default";

    /// <summary>Интервал после параграфа в twips. Default: "0"</summary>
    public string SpaceAfter { get; set; } = "default";
}

public class HeadingSettings
{
    /// <summary>Размер шрифта заголовка в half-points. Default: "28" (14pt)</summary>
    public string FontSize { get; set; } = "default";

    /// <summary>Жирное начертание (true/false). Default: "true"</summary>
    public string Bold { get; set; } = "default";

    /// <summary>Выравнивание заголовка. Default: "left"</summary>
    public string Alignment { get; set; } = "default";

    /// <summary>Интервал перед заголовком в twips. Default: "300"</summary>
    public string SpaceBefore { get; set; } = "default";

    /// <summary>Интервал после заголовка в twips. Default: "150"</summary>
    public string SpaceAfter { get; set; } = "default";
}

public class TitleSettings
{
    /// <summary>Размер шрифта заголовка в half-points. Default: "32" (16pt)</summary>
    public string FontSize { get; set; } = "default";

    /// <summary>Жирное начертание (true/false). Default: "true"</summary>
    public string Bold { get; set; } = "default";

    /// <summary>Выравнивание заголовка. Default: "center"</summary>
    public string Alignment { get; set; } = "default";

    /// <summary>Интервал после заголовка в twips. Default: "200"</summary>
    public string SpaceAfter { get; set; } = "default";
}
