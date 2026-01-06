using MVP_for_StudyGekko.Models;

namespace MVP_for_StudyGekko.Services;

public static class PromptTemplates
{
    public static string GenerateOutline(string topic, string workType, int pages)
    {
        return $"""
            Составь детальный план {workType} на тему: "{topic}".
            
            Требования:
            - Объём: {pages} страниц
            - Структура: введение, основная часть (3-5 разделов), заключение
            - Для каждого раздела укажи ключевые тезисы
            
            Ответь только планом, без пояснений.
            """;
    }

    public static string GenerateSection(string topic, string sectionTitle, string outline)
    {
        return $"""
            Напиши раздел "{sectionTitle}" для работы на тему: "{topic}".
            
            Не подписывай раздел , просто начни с текста.

            Общий план работы:
            {outline}
            
            Требования:
            - Академический стиль
            - Логичные переходы между абзацами
            - Конкретные факты и примеры
            
            Напиши только текст раздела.
            """;
    }

    public static string GenerateIntroduction(string topic, string outline)
    {
        return $"""
            Напиши введение для работы на тему: "{topic}".
            
            Не подписывай раздел , просто начни с текста.

            План работы:
            {outline}
            
            Введение должно содержать:
            - Актуальность темы
            - Цель работы
            - Задачи
            - Краткое описание структуры
            
            Объём: 1 страница.
            """;
    }

    public static string GenerateConclusion(string topic, string outline)
    {
        return $"""
            Напиши заключение для работы на тему: "{topic}".
            
            План работы:
            {outline}
            
            Заключение должно содержать:
            - Краткие выводы по каждому разделу
            - Достигнута ли цель работы
            - Возможные направления дальнейшего исследования
            
            Объём: 0.5-1 страница.
            """;
    }

    public static string GetWorkTypeName(WorkType type)
    {
        return type switch
        {
            WorkType.Essay => "эссе",
            WorkType.Referat => "реферата",
            WorkType.Coursework => "курсовой работы",
            _ => "работы"
        };
    }
}