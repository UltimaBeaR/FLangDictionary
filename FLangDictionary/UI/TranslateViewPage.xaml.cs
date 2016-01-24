using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

// ToDo: фигня какаято - если двигать мышь и пытаться выделить слово то не всегда получается. Если же над словом не двигая мышь нажать кнопку потом передвинуть и отпустить
// над ней же то выделяется слово. Но если само нажатие шло во время движения - выделения нет. При этом пробовал во время движения нажать, потом остановить движение потом опять
// двигать и отжимать клавишу - не выделяет. Однако если во время движения нажать, потом остановить и отпустить - выделяет. То есть видимо выделяет только если нажатие или отжатие было не во время движения.
// Хотя там даже не движение а если движение быстрое довольно тогда только невыделяет. если медленное то все ок

namespace FLangDictionary.UI
{
    /// <summary>
    /// Логика взаимодействия для TranslateViewPage.xaml
    /// </summary>
    public partial class TranslateViewPage : Page
    {
        private const double articleFontSize = 20;

        // Параграф в FlowLayout-е статьи, в котором хранится основной текст статьи
        Paragraph m_articleParagraph;
        // Словарь для просмотра перевода слов и словосочетаний. Если null, значит еще не загружен
        StarDict.StarDict m_dict;
        // Список выбранных слов через клик по слову либо клик по слову с зажатием CTRL для мультивыбора
        // В этом списке слова будут идти не в порядке выбора а в порядке следования в тексте статьи
        List<Logic.TextInLanguage.SyntaxLayout.Word> m_selectedWords;
        // Слово в статье, над которым был произведен MouseDown. Нужен для правильной обработки клика по MouseUp
        Logic.TextInLanguage.SyntaxLayout.Word m_articleMouseLeftButtonDownWord;
        // Последняя позиция мыши при движении над параграфом. Нужно для безглючной работы
        Point m_paragraphMouseMoveLastPos;

        // Обновляет состояние выделений (форматом) текста в статье
        private void UpdateArticleVisualSelection(Logic.TextInLanguage.SyntaxLayout.Word mouseOverWord)
        {
            int selectionCount = m_selectedWords.Count + (mouseOverWord == null ? 0 : 2);

            if (selectionCount == 0)
            {
                FlowDocumentFormatter.SetTextVisualSelections(m_articleParagraph);
                return;
            }

            List<FlowDocumentFormatter.Selection> selections = new List<FlowDocumentFormatter.Selection>(selectionCount);

            for (int i = 0; i < m_selectedWords.Count; i++)
            {
                selections.Add(new FlowDocumentFormatter.Selection()
                {
                    fontColor = Colors.Black,
                    fontFamily = this.FontFamily,
                    fontSize = articleFontSize,
                    range = new FlowDocumentFormatter.Selection.Range()
                    {
                        firstIndex = m_selectedWords[i].FirstIndex,
                        lastIndex = m_selectedWords[i].LastIndex
                    },
                    backgroundColor = Colors.LightGreen,
                    priority = 1
                });
            }

            if (mouseOverWord != null)
            {
                bool mouseOverWordSelected = m_selectedWords.Contains(mouseOverWord);

                selections.Add(new FlowDocumentFormatter.Selection()
                {
                    fontColor = Colors.Black,
                    fontFamily = this.FontFamily,
                    fontSize = articleFontSize,
                    range = new FlowDocumentFormatter.Selection.Range()
                    {
                        firstIndex = mouseOverWord.FirstIndex,
                        lastIndex = mouseOverWord.LastIndex
                    },
                    backgroundColor = mouseOverWordSelected ? Colors.LightBlue : Colors.Bisque,
                    priority = 2
                });

                selections.Add(new FlowDocumentFormatter.Selection()
                {
                    fontColor = Colors.Black,
                    fontFamily = this.FontFamily,
                    fontSize = articleFontSize,
                    range = new FlowDocumentFormatter.Selection.Range()
                    {
                        firstIndex = mouseOverWord.Sentence.FirstIndex,
                        lastIndex = mouseOverWord.Sentence.LastIndex
                    },
                    backgroundColor = Colors.LightYellow,
                    priority = 0
                });
            }

            FlowDocumentFormatter.SetTextVisualSelections(m_articleParagraph, selections);
        }

        // Обновляет текущее визуальное представление в соответсвии с информацией из модели(бд)
        private void UpdateVisuals()
        {
            m_articleParagraph.Inlines.Clear();
            m_articleParagraph.Inlines.Add(Global.CurrentWorkspace.CurrentArticle.OriginalText.Text);
        }

        public TranslateViewPage()
        {
            InitializeComponent();

            m_selectedWords = new List<Logic.TextInLanguage.SyntaxLayout.Word>();

            m_articleParagraph = articleRichTextBox.Document.Blocks.FirstBlock as Paragraph;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            articleMainParagraph.FontSize = articleFontSize;
            UpdateVisuals();
        }

        private void test_Click(object sender, RoutedEventArgs e)
        {
            Logic.TranslationUnit translationUnit = new Logic.TranslationUnit(m_selectedWords.ToArray());

            translationUnit.translatedPhrase = textBoxTranslated.Text == string.Empty ? null : textBoxTranslated.Text;

            if (textBoxOriginalInfinitive.Text == string.Empty || textBoxTranslatedInfinitive.Text == string.Empty)
            {
                translationUnit.infinitiveTranslation.originalPhrase = null;
                translationUnit.infinitiveTranslation.translatedPhrase = null;
            }
            else
            {
                translationUnit.infinitiveTranslation.originalPhrase = textBoxOriginalInfinitive.Text;
                translationUnit.infinitiveTranslation.translatedPhrase = textBoxTranslatedInfinitive.Text;
            }

            Global.CurrentWorkspace.CurrentArticle.ModifyTranslationUnit(translationUnit, Global.CurrentWorkspace.TranslationLanguages[0].Code);

            // Test_OpenStarDict();
        }

        private void Test_OpenStarDict()
        {
            var openFileDialog = new System.Windows.Forms.FolderBrowserDialog();
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                m_dict = new StarDict.StarDict(openFileDialog.SelectedPath);
        }

        int GetTextIndexInParagraphFromPointer(TextPointer pointer)
        {
            if (pointer != null)
            {
                // Такой вот извращенный способ узнать индекс символа в строке
                TextRange textRange = new TextRange(pointer.Paragraph.ContentStart, pointer);
                return textRange.Text.Length;
            }

            return -1;
        }

        string GetWordFromPointer(TextPointer pointer)
        {
            string word;
            int firstIndex;
            int lastIndex;
            bool inWord = GetWordFromPointer(pointer, out word, out firstIndex, out lastIndex);

            if (inWord)
                return word;
            else
                return null;
        }

        void GetWordFromPointer(TextPointer pointer, out Logic.TextInLanguage.SyntaxLayout.Word word)
        {
            if (pointer != null)
            {
                int letterIdx = GetTextIndexInParagraphFromPointer(pointer);

                Logic.TextInLanguage.SyntaxLayout.WordInSentenceIndex index;
                bool inWord = Global.CurrentWorkspace.CurrentArticle.OriginalText.Layout.GetWordInSentenceIndexByTextLetterIndex(letterIdx, out index);
                var wordData = Global.CurrentWorkspace.CurrentArticle.OriginalText.Layout.GetWordByIndex(index);

                if (inWord)
                    word = wordData;
                else
                    word = null;

                return;
            }

            word = null;
        }

        bool GetWordFromPointer(TextPointer pointer, out string word, out int firstIndex, out int lastIndex)
        {
            Logic.TextInLanguage.SyntaxLayout.Word wordData;
            GetWordFromPointer(pointer, out wordData);

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

        private void Paragraph_MouseMove(object sender, MouseEventArgs e)
        {
            // Если не сделать этой проверки на то что мышь изменила координату, то почему-то не будут обрабатываться события нажатий на кнопки мыши, если
            // вызывается код изменения inline-ов в параграфе (SetTextVisualSelection() это делает)
            // Это событие вызывается постоянно, даже когда мышь реально не двигается, а тут идет отсечка на моменты когда мышь не двигается
            if (m_paragraphMouseMoveLastPos != Mouse.GetPosition(articleRichTextBox))
            {
                m_paragraphMouseMoveLastPos = Mouse.GetPosition(articleRichTextBox);
                TextPointer pointer = articleRichTextBox.GetPositionFromPoint(Mouse.GetPosition(articleRichTextBox), false);

                Logic.TextInLanguage.SyntaxLayout.Word mouseOverWord;
                GetWordFromPointer(pointer, out mouseOverWord);

                UpdateArticleVisualSelection(mouseOverWord);

                // ToDo: и тем не менее глюк в заголовке этого файла происходит в том случае когда заходим в это условие (когда мышка не двигается)
                // Тупо перестает приходить событие mouseup/down
            }
        }

        private void Paragraph_MouseLeave(object sender, MouseEventArgs e)
        {
            UpdateArticleVisualSelection(null);
        }

        private void articleRichTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            TextPointer pointer = articleRichTextBox.GetPositionFromPoint(Mouse.GetPosition(articleRichTextBox), false);
            Logic.TextInLanguage.SyntaxLayout.Word word;
            GetWordFromPointer(pointer, out word);

            m_articleMouseLeftButtonDownWord = word;
        }

        private void articleRichTextBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            bool ctrlIsDown = Keyboard.GetKeyStates(Key.LeftCtrl).HasFlag(KeyStates.Down) || Keyboard.GetKeyStates(Key.RightCtrl).HasFlag(KeyStates.Down);

            TextPointer pointer = articleRichTextBox.GetPositionFromPoint(Mouse.GetPosition(articleRichTextBox), false);
            Logic.TextInLanguage.SyntaxLayout.Word mouseOverWord;
            GetWordFromPointer(pointer, out mouseOverWord);

            if (m_articleMouseLeftButtonDownWord == mouseOverWord)
            {
                m_articleMouseLeftButtonDownWord = null;

                if (mouseOverWord != null)
                    SelectWord(mouseOverWord, ctrlIsDown);
                else if (!ctrlIsDown)
                    ClearSelectedWords();

                UpdateArticleVisualSelection(mouseOverWord);
            }
        }

        private void ClearSelectedWords()
        {
            m_selectedWords.Clear();
            textBoxOriginal.Text = string.Empty;
            StarDictFlowDocumentBuilder.Build(string.Empty, dictionaryFlowDocument, DictionaryTermReferenceMouseUp);
        }

        public void SelectWord(Logic.TextInLanguage.SyntaxLayout.Word word, bool multiSelect)
        {
            if (!multiSelect)
            {
                m_selectedWords.Clear();
                m_selectedWords.Add(word);
            }
            else
            {
                if (!m_selectedWords.Contains(word))
                {
                    m_selectedWords.Add(word);

                    // Упорядочим список выделенных слов по порядку следования в исходном тексте
                    m_selectedWords.Sort((a, b) => { return a.FirstIndex.CompareTo(b.FirstIndex); });
                }
                else
                    m_selectedWords.Remove(word);
            }

            if (m_selectedWords.Count > 0)
            {

                string phraseAsString = string.Empty;
                foreach (var wordInSelection in m_selectedWords)
                    phraseAsString += wordInSelection.ToString() + (ReferenceEquals(wordInSelection, m_selectedWords[m_selectedWords.Count - 1]) ? string.Empty : " ");

                textBoxOriginal.Text = phraseAsString;

                if (m_dict != null)
                {
                    string translation = m_dict.LookupWord(phraseAsString);
                    StarDictFlowDocumentBuilder.Build(translation, dictionaryFlowDocument, DictionaryTermReferenceMouseUp);
                }
            }
            else
                ClearSelectedWords();
        }

        private void DictionaryTermReferenceMouseUp(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Reference");
        }
    }
}
