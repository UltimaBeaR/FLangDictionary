using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FLangDictionary.UI
{
    /// <summary>
    /// Логика взаимодействия для LearnViewPage.xaml
    /// </summary>
    public partial class LearnViewPage : Page
    {
        private Paragraph m_originalArticleParagraph;
        private PositionFromMouseQuery m_positionFromMouseQuery;

        public LearnViewPage()
        {
            InitializeComponent();

            m_originalArticleParagraph = originalArticleScrollViewer.Document.Blocks.FirstBlock as Paragraph;
    }

        // Обновляет текущее визуальное представление в соответсвии с информацией из модели(бд)
        private void UpdateVisuals()
        {
            m_originalArticleParagraph.Inlines.Clear();
            m_originalArticleParagraph.Inlines.Add(Global.CurrentWorkspace.CurrentArticle.OriginalText.Text);

            m_positionFromMouseQuery = new PositionFromMouseQuery(originalArticleScrollViewer, m_originalArticleParagraph);
        }

        private void CurrentArticleOpenedHandler(object sender, EventArgs e)
        {
            if (Global.CurrentWorkspace.CurrentArticle != null)
            {

            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Global.CurrentArticleOpened += CurrentArticleOpenedHandler;

            UpdateVisuals();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Global.CurrentArticleOpened -= CurrentArticleOpenedHandler;
        }

        private void Paragraph_MouseMove(object sender, MouseEventArgs e)
        {
            Logic.TextInLanguage.SyntaxLayout.Word word;
            UICommon.GetWordFromPointer(m_positionFromMouseQuery.GetPositionFromPoint(Mouse.GetPosition(originalArticleScrollViewer)),
                Global.CurrentWorkspace.CurrentArticle.OriginalText, out word);

            var wordTranslation = Global.CurrentWorkspace.CurrentArticle.GetWordTranslation(Global.CurrentWorkspace.TranslationLanguages[0].Code, word);
            var phraseTranslation = Global.CurrentWorkspace.CurrentArticle.GetPhraseTranslation(Global.CurrentWorkspace.TranslationLanguages[0].Code, word);

            if (word == null || wordTranslation == null)
                articlePopup.IsOpen = false;
            else
            {
                if (phraseTranslation != null)
                {
                    articlePopup_phrase.Visibility = Visibility.Visible;

                    articlePopup_phrase_source.Content = ((Func<string>)(() =>
                    {
                        StringBuilder strBuilder = new StringBuilder();
                        foreach (var originalPhraseWord in phraseTranslation.OriginalPhrase)
                        {
                            if (strBuilder.Length != 0)
                                strBuilder.Append(" ");

                            strBuilder.Append(originalPhraseWord.ToString());
                        }

                        return strBuilder.ToString();
                    }))();
                         
                    articlePopup_phrase_translation.Content = phraseTranslation.translatedPhrase;
                    if (phraseTranslation.infinitiveTranslation.HasValue)
                    {
                        articlePopup_phrase_infinitive.Visibility = Visibility.Visible;
                        articlePopup_phrase_infinitive.Content =
                            $"{phraseTranslation.infinitiveTranslation.originalPhrase} - {phraseTranslation.infinitiveTranslation.translatedPhrase}";
                    }
                    else
                        articlePopup_phrase_infinitive.Visibility = Visibility.Collapsed;
                }
                else
                    articlePopup_phrase.Visibility = Visibility.Collapsed;

                articlePopup_line.Visibility = (phraseTranslation != null && wordTranslation != null) ? Visibility.Visible : Visibility.Collapsed;

                if (wordTranslation != null)
                {
                    articlePopup_word.Visibility = Visibility.Visible;

                    articlePopup_word_source.Content = word.ToString();
                    articlePopup_word_translation.Content = wordTranslation.translatedPhrase;
                    if (wordTranslation.infinitiveTranslation.HasValue)
                    {
                        articlePopup_word_infinitive.Visibility = Visibility.Visible;
                        articlePopup_word_infinitive.Content =
                            $"{wordTranslation.infinitiveTranslation.originalPhrase} - {wordTranslation.infinitiveTranslation.translatedPhrase}";
                    }
                    else
                        articlePopup_word_infinitive.Visibility = Visibility.Collapsed;
                }
                else
                    articlePopup_word.Visibility = Visibility.Collapsed;

                const double mouseOffset = 10;

                Point point = Mouse.GetPosition(originalArticleScrollViewer);
                point.X += mouseOffset;
                point.Y += mouseOffset;

                articlePopup.HorizontalOffset = point.X;
                articlePopup.VerticalOffset = point.Y;
                articlePopup.IsOpen = true;
            }
        }

        private void Paragraph_MouseLeave(object sender, MouseEventArgs e)
        {
            articlePopup.IsOpen = false;
        }
    }
}
