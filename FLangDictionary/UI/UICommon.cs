using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace FLangDictionary.UI
{
    class UICommon
    {
        public static bool ShowDialog_TwoButton(Window owner, string title = "Message box", string message = "Message", string positiveCaption = "Ok", string negativeCaption = "Cancel")
        {
            TwoButtonsWindow dialog = new TwoButtonsWindow(title, message, positiveCaption, negativeCaption);
            dialog.Owner = owner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            return dialog.ShowDialog().Value;
        }

        public static string ShowDialog_CreateNewArticle(Window owner)
        {
            // Сначала нужно получить имя имя доступной новой статьи по умолчанию

            const string defaultFreeWorkspaceNameBase = "Article";
            int defaultFreeWorkspaceNameNumber = 1;

            string defaultFreeWorkspaceName;
            do
                defaultFreeWorkspaceName = $"{defaultFreeWorkspaceNameBase}{defaultFreeWorkspaceNameNumber++}";
            while (Global.CurrentWorkspace.ArticleNames.Contains(defaultFreeWorkspaceName));

            InputBoxWindow dialog = new InputBoxWindow(
                defaultFreeWorkspaceName,
                owner.Lang("NewArticleDialog.Title"),
                owner.Lang("NewArticleDialog.Label"),
                owner.Lang("CreateButtonCaption"),
                owner.Lang("CancelButtonCaption"),
                (input) =>
                {
                    if (input == string.Empty)
                        return owner.Lang("Error.Article.IllegalName");

                    if (Global.CurrentWorkspace.ArticleNames.Contains(input))
                        return owner.Lang("Error.Article.AlreadyExists");

                    return null;
                });

            dialog.Owner = owner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            return dialog.ShowDialog().Value ? dialog.Input : null;
        }

        // По указателю в тексте получает индекс символа в тексте, связанного с этим указателем.
        // Причем текст считается относительно начала параграфа, в котором находится этот указатель
        public static int GetTextIndexInParagraphFromPointer(TextPointer pointer)
        {
            if (pointer != null)
            {
                // Такой вот извращенный способ узнать индекс символа в строке
                TextRange textRange = new TextRange(pointer.Paragraph.ContentStart, pointer);
                return textRange.Text.Length;
            }

            return -1;
        }

        public static string GetWordFromPointer(TextPointer pointer, Logic.TextInLanguage textInLanguage)
        {
            string word;
            int firstIndex;
            int lastIndex;
            bool inWord = GetWordFromPointer(pointer, textInLanguage, out word, out firstIndex, out lastIndex);

            if (inWord)
                return word;
            else
                return null;
        }

        public static void GetWordFromPointer(TextPointer pointer, Logic.TextInLanguage textInLanguage, out Logic.TextInLanguage.SyntaxLayout.Word word)
        {
            if (pointer != null)
            {
                int letterIdx = GetTextIndexInParagraphFromPointer(pointer);

                Logic.TextInLanguage.SyntaxLayout.WordInSentenceIndex index;
                bool inWord = textInLanguage.Layout.GetWordInSentenceIndexByTextLetterIndex(letterIdx, out index);
                var wordData = textInLanguage.Layout.GetWordByIndex(index);

                if (inWord)
                    word = wordData;
                else
                    word = null;

                return;
            }

            word = null;
        }

        public static bool GetWordFromPointer(TextPointer pointer, Logic.TextInLanguage textInLanguage, out string word, out int firstIndex, out int lastIndex)
        {
            Logic.TextInLanguage.SyntaxLayout.Word wordData;
            GetWordFromPointer(pointer, textInLanguage, out wordData);

            bool wordDataExists = wordData != null;
            if (wordDataExists)
            {
                word = wordData.ToString();
                firstIndex = wordData.FirstIndex;
                lastIndex = wordData.LastIndex;
            }
            else
            {
                word = null;
                firstIndex = -1;
                lastIndex = -1;
            }

            return wordDataExists;
        }
    }

    // Штука для получения позиции в тексте из позиции мыши. Суть сего класса - кэшировать данные о ректах букв, чтобы доступ происходил быстрее (без этого оно безбожно тормозит)
    // По хорошему еще можно сделать тут какой-то механизм разделения пространства для быстрого нахождения ректа - но пока вроде и так не особо томрозит на мелких статьях
    class PositionFromMouseQuery
    {
        struct TextPointerWithRect
        {
            public Rect rect;
            public TextPointer textPointer;
            public TextPointer nextTextPointer;
        }

        double m_horizontalOffset;
        double m_verticalOffset;
        double m_scrollViewerZoom;
        double m_scrollViewerActualWidth;
        double m_scrollViewerActualHeight;

        ScrollViewer m_scrollControl;
        FlowDocumentScrollViewer m_scrollViewer;
        Paragraph m_paragraph;
        List<TextPointerWithRect> m_items;

        public PositionFromMouseQuery(FlowDocumentScrollViewer scrollViewer, Paragraph paragraph)
        {
            m_scrollViewer = scrollViewer;

            m_paragraph = paragraph;

            // Вот таким вот хитрым способом получаем элемент ScrollViewer
            m_scrollControl = ((Func<ScrollViewer>)(() =>
            {
                DependencyObject scrollViewerDepObj = m_scrollViewer;

                do
                {
                    if (VisualTreeHelper.GetChildrenCount(scrollViewerDepObj) > 0)
                        scrollViewerDepObj = VisualTreeHelper.GetChild(scrollViewerDepObj as Visual, 0);
                    else
                        return null;
                }
                while (!(scrollViewerDepObj is ScrollViewer));

                return scrollViewerDepObj as ScrollViewer;
            }))();

            m_items = new List<TextPointerWithRect>();

            RefreshData();
        }

        // ToDo: поидее надо показывать подсказку через небольшой таймаут, в случае если это свойство true. Чтобы не вызывать перестроение сразу, на случай если юзер просто
        // проводит мышку мимо
        // Нужно ли обновление внутренних данных перед вызовом GetPositionFromPoint(). Обновление занимает некоторое время
        public bool DoesNeedRefresh
        {
            get
            {
                return m_scrollViewerActualWidth != m_scrollViewer.ActualWidth ||
                    m_scrollViewerActualHeight != m_scrollViewer.ActualHeight ||
                    m_scrollViewerZoom != m_scrollViewer.Zoom ||
                    m_horizontalOffset != m_scrollControl.HorizontalOffset ||
                    m_verticalOffset != m_scrollControl.VerticalOffset;
            }
        }

        void RefreshData()
        {
            m_horizontalOffset = m_scrollControl.HorizontalOffset;
            m_verticalOffset = m_scrollControl.VerticalOffset;
            m_scrollViewerZoom = m_scrollViewer.Zoom;
            m_scrollViewerActualWidth = m_scrollViewer.ActualWidth;
            m_scrollViewerActualHeight = m_scrollViewer.ActualHeight;

            m_items.Clear();

            foreach (var inline in m_paragraph.Inlines)
            {
                Run run = inline as Run;

                if (run != null)
                {
                    TextPointer currentTextPointer = run.ContentStart;
                    TextPointer nextTextPointer = currentTextPointer.GetNextInsertionPosition(LogicalDirection.Forward);

                    while (nextTextPointer != null)
                    {
                        Rect currentRect = currentTextPointer.GetCharacterRect(LogicalDirection.Forward);
                        Rect nextRect = nextTextPointer.GetCharacterRect(LogicalDirection.Backward);

                        m_items.Add(new TextPointerWithRect()
                        {
                            rect = new Rect(currentRect.X, currentRect.Top, nextRect.X - currentRect.X, nextRect.Bottom - currentRect.Top),
                            textPointer = currentTextPointer,
                            nextTextPointer = nextTextPointer
                        });

                        currentTextPointer = nextTextPointer;
                        nextTextPointer = nextTextPointer.GetNextInsertionPosition(LogicalDirection.Forward);
                    }
                }
            }
        }

        public TextPointer GetPositionFromPoint(Point point)
        {
            if (DoesNeedRefresh)
                RefreshData();

            for (int i = 0; i < m_items.Count; i++)
            {
                Rect rect = m_items[i].rect;
                if (point.X >= rect.Left && point.X < rect.Right &&
                    point.Y >= rect.Top && point.Y < rect.Bottom)
                {
                    return m_items[i].textPointer;
                }
            }

            return null;
        }
    }

    static class WindowExtensions
    {
        // Получает имя ресурса - строка с разными вариантами перевода, в зависимости от текущего языка.
        // Эти ресурсы лежат в Resources/Lang*.xaml
        public static string Lang(this FrameworkElement frameworkElement, string langResource)
        {
            return (string)frameworkElement.FindResource("Lang." + langResource);
        }
    }

    public static class RunExtensions
    {
        // По координате получает указатель в тексте. Если координаты берутся мышиные, мышь нужно брать относительно
        // корневого элемента управления в котором сидит FlowDocument (как пример)
        public static TextPointer GetPositionFromPoint(this Run run, Point searchForPoint)
        {
            // Работает тормозно. Возможно стоит пробежаться 1 раз при перерисовке flowdocument-а и в какое нибудь дерево или чето подобное запихнуть все эти рект-ы
            // чтобы быстро находить букву по позиции

            TextPointer currentTextPointer = run.ContentStart;
            TextPointer nextTextPointer = currentTextPointer.GetNextInsertionPosition(LogicalDirection.Forward);

            while (nextTextPointer != null)
            {
                Rect currentRect = currentTextPointer.GetCharacterRect(LogicalDirection.Forward);
                Rect nextRect = nextTextPointer.GetCharacterRect(LogicalDirection.Backward);

                if (searchForPoint.X >= currentRect.X && searchForPoint.X <= nextRect.X &&
                    searchForPoint.Y >= currentRect.Top && searchForPoint.Y <= nextRect.Bottom)
                {
                    return currentTextPointer;
                }

                currentTextPointer = nextTextPointer;
                nextTextPointer = nextTextPointer.GetNextInsertionPosition(LogicalDirection.Forward);
            }

            return null;
        }
    }

    public static class ParagraphExtensions
    {
        // По координате получает указатель в тексте. Если координаты берутся мышиные, мышь нужно брать относительно
        // корневого элемента управления в котором сидит FlowDocument (как пример)
        public static TextPointer GetPositionFromPoint(this Paragraph paragraph, Point point)
        {
            foreach (var inline in paragraph.Inlines)
            {
                Run run = inline as Run;

                if (run != null)
                {
                    TextPointer textPointer = run.GetPositionFromPoint(point);
                    if (textPointer != null)
                        return textPointer;
                }
            }

            return null;
        }
    }
}
