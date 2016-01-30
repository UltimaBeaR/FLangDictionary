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
        // Список слов, которые не выделенны, но составляют фразу вместе с выделенными словами, должны подсвечиваться
        List<Logic.TextInLanguage.SyntaxLayout.Word> m_unselectedPhraseWords;
        // Список неправильных слов - когда выделены слова из различных фраз - тут будут эти слова, за исключением слов у которых нет ни одной фразы
        List<Logic.TextInLanguage.SyntaxLayout.Word> m_wrongWords;
        // Список слов, для которых существует перевод (фразы не считаются)
        List<Logic.TextInLanguage.SyntaxLayout.Word> m_translatedWords;

        // Слово в статье, над которым был произведен MouseDown. Нужен для правильной обработки клика по MouseUp
        Logic.TextInLanguage.SyntaxLayout.Word m_articleMouseLeftButtonDownWord;
        // Последняя позиция мыши при движении над параграфом. Нужно для безглючной работы
        Point m_paragraphMouseMoveLastPos;

        private Color m_textColorBackRight = Color.FromRgb(192, 245, 192);
        private Color m_textColorBackRightHover = Color.FromRgb(137, 237, 137);
        private Color m_textColorBackWrong = Color.FromRgb(247, 185, 174);
        private Color m_textColorBackWrongHover = Color.FromRgb(240, 134, 115);
        private Color m_textColorUnselectedPhrase = Color.FromRgb(69, 185, 214);
        private Color m_textColorBackUnselectedPhraseHover = Color.FromRgb(196, 233, 242);
        private Color m_textColorBackSentence = Colors.LightYellow;
        private Color m_textColorBackHover = Colors.Bisque;
        private Color m_textColorTranslated = Colors.DarkBlue;

        // Обновляет состояние выделений (форматом) текста в статье
        private void UpdateArticleVisualSelection(Logic.TextInLanguage.SyntaxLayout.Word mouseOverWord)
        {
            // Вычисляем сколько будет заданно различных выделений. Это нужно для того чтобы задать точный capacity, но если не совпадет - пофиг
            int selectionCount = m_translatedWords.Count + m_selectedWords.Count + m_unselectedPhraseWords.Count + m_wrongWords.Count + (mouseOverWord == null ? 0 : 2);

            List<FlowDocumentFormatter.Selection> selections = new List<FlowDocumentFormatter.Selection>(selectionCount);

            // Подсвечиваем переведенные слова
            for (int i = 0; i < m_translatedWords.Count; i++)
            {
                selections.Add(new FlowDocumentFormatter.Selection()
                {
                    fontColor = m_textColorTranslated,
                    fontFamily = this.FontFamily,
                    fontSize = articleFontSize,
                    range = new FlowDocumentFormatter.Selection.Range()
                    {
                        firstIndex = m_translatedWords[i].FirstIndex,
                        lastIndex = m_translatedWords[i].LastIndex
                    },
                    backgroundColor = Colors.White,
                    priority = 0
                });
            }

            // Подсвечиваем выделенные слова
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
                    backgroundColor = m_textColorBackRight,
                    priority = 2
                });
            }

            // Подсвечиваем слова из фразы, которые не присутсвуют в выделении
            for (int i = 0; i < m_unselectedPhraseWords.Count; i++)
            {
                selections.Add(new FlowDocumentFormatter.Selection()
                {
                    fontColor = m_textColorUnselectedPhrase,
                    fontFamily = this.FontFamily,
                    fontSize = articleFontSize,
                    range = new FlowDocumentFormatter.Selection.Range()
                    {
                        firstIndex = m_unselectedPhraseWords[i].FirstIndex,
                        lastIndex = m_unselectedPhraseWords[i].LastIndex
                    },
                    backgroundColor = Colors.White,
                    priority = 2
                });
            }

            // Подсвечиваем "неправильные" слова
            for (int i = 0; i < m_wrongWords.Count; i++)
            {
                selections.Add(new FlowDocumentFormatter.Selection()
                {
                    fontColor = Colors.Black,
                    fontFamily = this.FontFamily,
                    fontSize = articleFontSize,
                    range = new FlowDocumentFormatter.Selection.Range()
                    {
                        firstIndex = m_wrongWords[i].FirstIndex,
                        lastIndex = m_wrongWords[i].LastIndex
                    },
                    backgroundColor = m_textColorBackWrong,
                    priority = 3
                });
            }

            // Если курсор находится над каким либо словом
            if (mouseOverWord != null)
            {
                // Подсвечиваем слово, надо которым сейчас курсор

                Color hoverWordColor = m_textColorBackHover;
                if (m_wrongWords.Contains(mouseOverWord))
                    hoverWordColor = m_textColorBackWrongHover;
                else if (m_unselectedPhraseWords.Contains(mouseOverWord))
                    hoverWordColor = m_textColorBackUnselectedPhraseHover;
                else if (m_selectedWords.Contains(mouseOverWord))
                    hoverWordColor = m_textColorBackRightHover;

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
                    backgroundColor = hoverWordColor,
                    priority = 4
                });

                // Подсвечиваем предложение

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
                    backgroundColor = m_textColorBackSentence,
                    priority = 1
                });
            }

            FlowDocumentFormatter.SetTextVisualSelections(m_articleParagraph, selections);
        }

        // Обновляет текущее визуальное представление в соответсвии с информацией из модели(бд)
        private void UpdateVisuals()
        {
            // ToDo: тест. временно берем тупо первый язык в списке. надо брать из выпадающего списка (если языков вообще нет то эта функция не должна была вызываться)
            string selectedTranslationLanguageCode = Global.CurrentWorkspace.TranslationLanguages[0].Code;

            m_articleParagraph.Inlines.Clear();
            m_articleParagraph.Inlines.Add(Global.CurrentWorkspace.CurrentArticle.OriginalText.Text);

            Global.CurrentWorkspace.CurrentArticle.GetWordsWithOwnTranslationList(selectedTranslationLanguageCode, m_translatedWords);

            UpdateArticleVisualSelection(null);
        }

        public TranslateViewPage()
        {
            InitializeComponent();

            m_selectedWords = new List<Logic.TextInLanguage.SyntaxLayout.Word>();
            m_unselectedPhraseWords = new List<Logic.TextInLanguage.SyntaxLayout.Word>();
            m_wrongWords = new List<Logic.TextInLanguage.SyntaxLayout.Word>();
            m_translatedWords = new List<Logic.TextInLanguage.SyntaxLayout.Word>();

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
            OnSelectedWordsChange();

            //textBoxOriginal.Text = string.Empty;
            //StarDictFlowDocumentBuilder.Build(string.Empty, dictionaryFlowDocument, DictionaryTermReferenceMouseUp);
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

            OnSelectedWordsChange();

            /*if (m_selectedWords.Count > 0)
            {

                string phraseAsString = string.Empty;
                foreach (var wordInSelection in m_selectedWords)
                    phraseAsString += wordInSelection.ToString() + (ReferenceEquals(wordInSelection, m_selectedWords[m_selectedWords.Count - 1]) ? string.Empty : " ");

                textBoxOriginal.Text = phraseAsString;


                Logic.TranslationUnit selectedTranslationUnit;
                Logic.TranslationUnit phraseTranslationUnit;


                bool canEditSelection;
                Logic.TextInLanguage.SyntaxLayout.Word[] wrongWords;

                // ToDo: тест. временно берем тупо первый язык в списке. надо брать из выпадающего списка (если языков вообще нет то эта функция не должна была вызваться)
                Global.CurrentWorkspace.CurrentArticle.GetSelectedWordsInfo(Global.CurrentWorkspace.TranslationLanguages[0].Code, m_selectedWords.ToArray(),
                    out canEditSelection, out selectedTranslationUnit, out phraseTranslationUnit, out wrongWords);




                string selectedStr = selectedTranslationUnit == null ? "null" : selectedTranslationUnit.OriginalPhrase.Length.ToString();
                string phraseStr = phraseTranslationUnit == null ? "null" : phraseTranslationUnit.OriginalPhrase.Length.ToString();

                MessageBox.Show($"{selectedStr}\n{phraseStr}");






                if (selectedTranslationUnit != null)
                {
                    textBoxTranslated.Text = selectedTranslationUnit.translatedPhrase;
                    textBoxOriginalInfinitive.Text = selectedTranslationUnit.infinitiveTranslation.originalPhrase;
                    textBoxTranslatedInfinitive.Text = selectedTranslationUnit.infinitiveTranslation.translatedPhrase;
                }
                else
                {
                    textBoxTranslated.Text = textBoxOriginalInfinitive.Text = textBoxTranslatedInfinitive.Text = null;
                }

                if (phraseTranslationUnit != null)
                {
                    MessageBox.Show(phraseTranslationUnit.translatedPhrase);
                }

                if (m_dict != null)
                {
                    string translation = m_dict.LookupWord(phraseAsString);
                    StarDictFlowDocumentBuilder.Build(translation, dictionaryFlowDocument, DictionaryTermReferenceMouseUp);
                }
            }
            else
                ClearSelectedWords();*/
        }

        // Устанавливает состояние для панели редактирования translation unit для выделенного слова/фразы
        public void SetTranslationUnitAreaState(bool enabled, Logic.TranslationUnit translationUnit = null)
        {
            textBoxOriginalInfinitive.IsEnabled = textBoxTranslatedInfinitive.IsEnabled = textBoxOriginal.IsEnabled = textBoxTranslated.IsEnabled = enabled;

            if (enabled)
            {
                if (translationUnit != null)
                {
                    textBoxOriginalInfinitive.Text = translationUnit.infinitiveTranslation.originalPhrase;
                    textBoxTranslatedInfinitive.Text = translationUnit.infinitiveTranslation.translatedPhrase;
                    textBoxTranslated.Text = translationUnit.translatedPhrase;
                }
                else
                    textBoxOriginalInfinitive.Text = textBoxTranslatedInfinitive.Text = textBoxTranslated.Text = string.Empty;

                StringBuilder selectedWordsAsString = new StringBuilder();
                foreach (var wordInSelection in m_selectedWords)
                {
                    selectedWordsAsString.Append(wordInSelection.ToString());
                    selectedWordsAsString.Append(ReferenceEquals(wordInSelection, m_selectedWords[m_selectedWords.Count - 1]) ? string.Empty : " ");
                }

                textBoxOriginal.Text = selectedWordsAsString.ToString();
            }
            else
                textBoxOriginalInfinitive.Text = textBoxTranslatedInfinitive.Text = textBoxOriginal.Text = textBoxTranslated.Text = string.Empty;
        }

        // В m_unselectedPhraseWords прописывает заданные слова, но отфильтровывает в них текущие m_selectedWords
        private void SetUnselectedPhraseWords(Logic.TextInLanguage.SyntaxLayout.Word[] phraseWords)
        {
            m_unselectedPhraseWords.Clear();

            if (phraseWords != null && phraseWords.Length > 0)
            {
                foreach (var word in phraseWords)
                {
                    if (!m_selectedWords.Contains(word))
                        m_unselectedPhraseWords.Add(word);
                }
            }
        }

        private void OnSelectedWordsChange()
        {
            // ToDo: тест. временно берем тупо первый язык в списке. надо брать из выпадающего списка (если языков вообще нет то эта функция не должна была вызываться)
            string selectedTranslationLanguageCode = Global.CurrentWorkspace.TranslationLanguages[0].Code;

            if (m_selectedWords.Count == 0)
            {
                SetTranslationUnitAreaState(false);
                SetUnselectedPhraseWords(null);
                m_wrongWords.Clear();

            }
            else if (m_selectedWords.Count == 1)
            {
                SetTranslationUnitAreaState(true, Global.CurrentWorkspace.CurrentArticle.GetTranslation(selectedTranslationLanguageCode, m_selectedWords[0]));
                m_wrongWords.Clear();

                var phraseTranslationUnit = Global.CurrentWorkspace.CurrentArticle.GetPhraseTranslation(selectedTranslationLanguageCode, m_selectedWords[0]);
                SetUnselectedPhraseWords(phraseTranslationUnit == null ? null : phraseTranslationUnit.OriginalPhrase);
            }
            else
            {
                bool selectionHasSinglePhrase = true;

                // Получаем фразу-перевод для каждого из выделенных слов
                Logic.TranslationUnit[] phrasesForEachWordInSelection = new Logic.TranslationUnit[m_selectedWords.Count];
                for (int i = 0; i < m_selectedWords.Count; i++)
                {
                    phrasesForEachWordInSelection[i] = Global.CurrentWorkspace.CurrentArticle.GetPhraseTranslation(selectedTranslationLanguageCode, m_selectedWords[i]);

                    if (selectionHasSinglePhrase && i != 0 && phrasesForEachWordInSelection[i] != phrasesForEachWordInSelection[i - 1])
                        selectionHasSinglePhrase = false;
                }

                m_wrongWords.Clear();

                if (selectionHasSinglePhrase)
                {
                    var phraseTranslationUnit = phrasesForEachWordInSelection[0];

                    if (phraseTranslationUnit == null || phraseTranslationUnit.OriginalPhrase.Length == m_selectedWords.Count)
                    {
                        SetTranslationUnitAreaState(true, phraseTranslationUnit);
                        SetUnselectedPhraseWords(null);
                    }
                    else
                    {
                        SetTranslationUnitAreaState(false);
                        SetUnselectedPhraseWords(phraseTranslationUnit.OriginalPhrase);
                    }
                }
                else
                {
                    SetTranslationUnitAreaState(false);
                    SetUnselectedPhraseWords(null);

                    for (int i = 0; i < m_selectedWords.Count; i++)
                    {
                        if (phrasesForEachWordInSelection[i] != null)
                            m_wrongWords.Add(m_selectedWords[i]);
                    }
                }
            }
        }

        private void DictionaryTermReferenceMouseUp(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Reference");
        }
    }
}
