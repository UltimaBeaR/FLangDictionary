using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Media;

namespace FLangDictionary.UI
{
    // Дополнительный функционал для форматировния FlowDocument
    static class FlowDocumentFormatter
    {
        public struct Selection
        {
            public struct Range
            {
                public int firstIndex;
                public int lastIndex;
            }

            public Range range;
            public int priority;
            public FontFamily fontFamily;
            public double fontSize;
            public Color fontColor;
            public Color backgroundColor;
        }

        // Преобразует весь текст, содержащийся в заданном параграфе в отформатированный текст, согласно переданному списку выделений
        public static void SetTextVisualSelections(Paragraph paragraph, List<Selection> selections = null)
        {
            if (paragraph == null)
                return;

            string text = (new TextRange(paragraph.ContentStart, paragraph.ContentEnd)).Text;

            paragraph.Inlines.Clear();

            if (selections == null || selections.Count == 0)
            {
                paragraph.Inlines.Add(new Run(text));
                return;
            }

            // Сортируем переданный список выделений по приоритету
            selections.Sort((a, b) => { return a.priority.CompareTo(b.priority) * (-1); });

            bool currentSelectionIsDefault;
            Selection currentSelection = new Selection();

            bool lastSelectionIsDefault = true;
            Selection lastSelection = new Selection();

            int selectionStart = 0;

            // Проходимся по каждому символу в тексте параграфа
            for (int charIdx = 0; charIdx < text.Length; charIdx++)
            {
                currentSelectionIsDefault = true;

                foreach (var selection in selections)
                {
                    if (charIdx >= selection.range.firstIndex && charIdx <= selection.range.lastIndex)
                    {
                        currentSelectionIsDefault = false;
                        currentSelection = selection;
                        break;
                    }
                }

                // Если это первый символ в тексте статьи, то добавлять новый run смысла нет
                bool needToAddNewRun = charIdx != 0 &&
                    // Нужно добавлять новый run, если:
                    (
                    // прошлый символ был дефолтным, а текущий недефолтный и наоборот (поменялась дефолтность)
                    currentSelectionIsDefault != lastSelectionIsDefault ||
                    // символ недефолтный, но при этом поменялись свойства selection-а
                    (!currentSelectionIsDefault && !currentSelection.Equals(lastSelection))
                    );

                if (needToAddNewRun)
                {
                    // Добавляем Run в параграф с заданным форматом выделения
                    AddInlineToParagraph(paragraph, text.Substring(selectionStart, ((charIdx - 1) - selectionStart) + 1), lastSelectionIsDefault, lastSelection);

                    // Ставим позицию начала следующего элемента Run в текущий символ
                    selectionStart = charIdx;
                }

                lastSelectionIsDefault = currentSelectionIsDefault;
                lastSelection = currentSelection;
            }

            // Добавляем последний параграф
            AddInlineToParagraph(paragraph, text.Substring(selectionStart, ((text.Length - 1) - selectionStart) + 1), lastSelectionIsDefault, lastSelection);
        }

        static void AddInlineToParagraph(Paragraph paragraph, string text, bool isDefault, Selection selectionFormat)
        {
            // Создаем текстовую строку
            Run textRunToBeAdded = new Run(text);

            // Если это было не дефолтное выделение, то нужно отформатировать строку в соответсвии с заданным форматом
            if (!isDefault)
            {
                if (selectionFormat.fontFamily != null)
                    textRunToBeAdded.FontFamily = selectionFormat.fontFamily;
                textRunToBeAdded.FontSize = selectionFormat.fontSize;
                textRunToBeAdded.Foreground = new SolidColorBrush(selectionFormat.fontColor);
                textRunToBeAdded.Background = new SolidColorBrush(selectionFormat.backgroundColor);
            }

            paragraph.Inlines.Add(textRunToBeAdded);
        }
    }
}
