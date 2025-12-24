# Функция анализа требований к оформлению документов

## Описание
Добавлена функция получения файла с требованиями к оформлению документа через Telegram, анализ файла с помощью Gemini AI и автоматическое применение этих требований при генерации Word-документов.

## Реализованные компоненты

### 1. Модель данных (`DocumentRequirements.cs`)
JSON-структура для хранения требований к форматированию:
- **PageSettings**: отступы страницы, размер, ориентация
- **FontSettings**: шрифт, размер, цвет
- **ParagraphSettings**: выравнивание, интервалы, отступы
- **HeadingSettings**: настройки заголовков
- **TitleSettings**: настройки главного заголовка

Все параметры опциональны. Если значение не определено, используется `"default"`.

### 2. SessionService (`SessionService.cs`)
In-memory хранилище требований пользователей с автоматической очисткой через 24 часа:
- `SetRequirements(chatId, requirements)` - сохранить требования
- `GetRequirements(chatId)` - получить требования
- `ClearRequirements(chatId)` - удалить требования
- `HasRequirements(chatId)` - проверить наличие

### 3. RequirementsAnalyzerService (`RequirementsAnalyzerService.cs`)
Сервис для анализа файлов через Gemini multimodal API:
- Принимает файлы форматов: DOCX, DOC, PDF, TXT, RTF
- Отправляет файл в Gemini с промптом для извлечения параметров форматирования
- Возвращает структурированный JSON с требованиями
- При ошибке возвращает пустые требования (все значения "default")

### 4. Обновленный UpdateHandler
Добавлены:
- Команда `/requirements` - инструкция по загрузке файла
- Обработка документов - автоматический анализ загруженных файлов
- Сохранение требований в сессии пользователя
- Применение требований при создании документа

### 5. Модифицированный WordDocumentBuilder
Расширен для поддержки динамических требований:
- Принимает опциональный параметр `DocumentRequirements`
- Применяет все параметры из требований или использует значения по умолчанию
- Вспомогательные методы для безопасного извлечения значений

## Использование

### Для пользователя (Telegram)
1. Отправить команду `/requirements`
2. Загрузить файл с примером оформления (DOCX, PDF и т.д.)
3. Получить подтверждение о сохранении требований
4. Создать работу командой `/create [тема]`
5. Получить документ с применением загруженных требований

### Пример JSON требований
```json
{
  "Page": {
    "TopMargin": "1134",
    "BottomMargin": "1134",
    "LeftMargin": "1701",
    "RightMargin": "850",
    "PageSize": "A4",
    "Orientation": "portrait"
  },
  "Font": {
    "Name": "Times New Roman",
    "Size": "28",
    "Color": "000000"
  },
  "Paragraph": {
    "Alignment": "both",
    "LineSpacing": "360",
    "LineSpacingRule": "auto",
    "FirstLineIndent": "720",
    "SpaceBefore": "0",
    "SpaceAfter": "0"
  },
  "Heading": {
    "FontSize": "28",
    "Bold": "true",
    "Alignment": "left",
    "SpaceBefore": "300",
    "SpaceAfter": "150"
  },
  "Title": {
    "FontSize": "32",
    "Bold": "true",
    "Alignment": "center",
    "SpaceAfter": "200"
  }
}
```

## Технические детали

### Единицы измерения
- **twips**: 1/1440 дюйма (1 см ≈ 567 twips)
- **half-points**: размер шрифта × 2 (14pt = 28)
- **Межстрочный интервал**: 1.0 = 240, 1.5 = 360, 2.0 = 480

### Значения по умолчанию
- Отступы: верх/низ 2см, левый 3см, правый 1.5см
- Шрифт: Times New Roman, 14pt
- Выравнивание: по ширине
- Интервал: 1.5
- Отступ первой строки: 1.25см

### Поддерживаемые форматы файлов
- `.docx` - Microsoft Word (OpenXML)
- `.doc` - Microsoft Word (старый формат)
- `.pdf` - PDF документы
- `.txt` - Текстовые файлы
- `.rtf` - Rich Text Format

## Интеграция с Gemini
Используется модель `gemini-2.0-flash-exp` с:
- Multimodal capabilities (текст + файл)
- Temperature: 0.1 (для стабильности)
- Response format: JSON

## Обработка ошибок
- Ошибки анализа файлов логируются
- При ошибке возвращаются требования по умолчанию
- Пользователь получает понятное сообщение об ошибке
- Неверные значения в JSON заменяются на "default"

## Файлы изменений
- `Models/DocumentRequirements.cs` - новый файл
- `Services/SessionService.cs` - новый файл
- `Services/RequirementsAnalyzerService.cs` - новый файл
- `Services/WordDocumentBuilder.cs` - модифицирован
- `Services/DocumentBuilder.cs` - обновлен интерфейс
- `Handlers/UpdateHandler..cs` - добавлена обработка файлов
- `Program.cs` - зарегистрированы новые сервисы
