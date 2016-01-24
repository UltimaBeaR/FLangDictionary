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

namespace FLangDictionary.UI
{
    /// <summary>
    /// Логика взаимодействия для EditViewPage.xaml
    /// </summary>
    public partial class EditViewPage : Page
    {
        private Window m_window;

        private void UpdateVisuals()
        {
            articleLabel.Content = Global.CurrentWorkspace.CurrentArticle.Name;

            if (Global.CurrentWorkspace.CurrentArticle.OriginalText.Finished)
            {
                articleTextEdit.Visibility = Visibility.Collapsed;
                articleTextFinished.Visibility = Visibility.Visible;

                articleOpenFromFileButton.IsEnabled = false;

                articleTextEdit.Text = string.Empty;
                articleTextFinished.Document.Blocks.Clear();
                articleTextFinished.Document.Blocks.Add(new Paragraph(new Run(Global.CurrentWorkspace.CurrentArticle.OriginalText.Text)));

                finishArticleButton.Content = this.Lang("EditView.EditCaption");
            }
            else
            {
                articleTextEdit.Visibility = Visibility.Visible;
                articleTextFinished.Visibility = Visibility.Collapsed;

                articleOpenFromFileButton.IsEnabled = true;

                articleTextEdit.Text = Global.CurrentWorkspace.CurrentArticle.OriginalText.Text;
                articleTextFinished.Document.Blocks.Clear();

                finishArticleButton.Content = this.Lang("EditView.FinishCaption");
            }

            translationLanguageComboBox.Items.Clear();
            foreach (var lang in Global.CurrentWorkspace.TranslationLanguages)
            {
                ComboBoxItem comboBoxItem = new ComboBoxItem();
                comboBoxItem.Content = lang.DisplayName;
                comboBoxItem.Tag = lang.Code;
                translationLanguageComboBox.Items.Add(comboBoxItem);
            }

            //finishTranslationButton.Content = TranslationFinished ?
            //"Edit translation" : "Finish translation";
        }

        // Сохранение текущего состояния в бд
        // Используется в моменты смены вида, выхода из приложения и т.д.
        public void SaveCurrentState()
        {
            if (Global.CurrentWorkspace != null && Global.CurrentWorkspace.CurrentArticle != null)
            {
                if (!Global.CurrentWorkspace.CurrentArticle.OriginalText.Finished)
                {
                    if (Global.CurrentWorkspace.CurrentArticle.OriginalText.Text != articleTextEdit.Text)
                        Global.CurrentWorkspace.CurrentArticle.ChangeArticleText(articleTextEdit.Text);
                }
            }
        }

        private void ShutdownHandler()
        {
            m_window.Closed -= MainWindow_Closed;
            Global.BeforeCurrentWorkspaceSet -= Global_BeforeCurrentWorkspaceSet;
            Global.CurrentArticleOpening -= CurrentArticleOpeningHandler;
            Global.CurrentArticleOpened -= CurrentArticleOpenedHandler;
            Global.UILanguageChanged -= Global_UILanguageChanged;

            SaveCurrentState();
        }

        private void CurrentArticleOpeningHandler(object sender, EventArgs e)
        {
            // При открытии другой статьи нужно сначала сохранить старую
            SaveCurrentState();
        }

        private void CurrentArticleOpenedHandler(object sender, EventArgs e)
        {
            if (Global.CurrentWorkspace.CurrentArticle != null)
                UpdateVisuals();
        }

        public EditViewPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            m_window = Window.GetWindow(this);
            m_window.Closed += MainWindow_Closed;

            Global.BeforeCurrentWorkspaceSet += Global_BeforeCurrentWorkspaceSet;
            Global.CurrentArticleOpening += CurrentArticleOpeningHandler;
            Global.CurrentArticleOpened += CurrentArticleOpenedHandler;
            Global.UILanguageChanged += Global_UILanguageChanged;
            // ToDo: тут еще надо подписаться на событие изменения списка языков для перевода. и как реакция - обновлять visuals, чтобы поменять combobox с языками +
            // всякие ситуации если был текущим выбрал перевод на язык который удалили например, то надо менять выбранный перевод на первый по списку (либо никакой, если их не осталось)
            // И наоборот - если было 0 языков а добавили язык, то надо выбрать 1ый по списку (до этого выбранным будет пустой)

            if (Global.CurrentWorkspace.CurrentArticle.CurrentArtisticalTranslation == null)
            {
                if (Global.CurrentWorkspace.TranslationLanguages.Length > 0)
                    Global.CurrentWorkspace.CurrentArticle.OpenArtisticalTranslation(Global.CurrentWorkspace.TranslationLanguages[0].Code);
            }

            UpdateVisuals();
        }

        private void Global_BeforeCurrentWorkspaceSet(object sender, EventArgs e)
        {
            SaveCurrentState();
        }

        private void Global_UILanguageChanged(object sender, EventArgs e)
        {
            UpdateVisuals();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            ShutdownHandler();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            ShutdownHandler();
        }

        private void FinishArticleButton_Click(object sender, RoutedEventArgs e)
        {
            if (Global.CurrentWorkspace.CurrentArticle.OriginalText.Finished)
            {
                // Переходим из finished в редактирование

                // ToDo: тут надо пройтись по всем зависимым от статьи данным (Через функцию в статье, которая лезет в функцию в репозитории) и перечислить их в сообщении, что они будут удалены.
                // далее, если данных таких нет, то можно вообще не выдавать это сообщение.
                string itemsToBeRemoved = "[]";
                if (!UICommon.ShowDialog_TwoButton(Window.GetWindow(this), string.Empty, string.Format(this.Lang("EditView.EditButtonClickMessage"), itemsToBeRemoved), this.Lang("YesButtonCaption"), this.Lang("NoButtonCaption")))
                    return;

                Global.CurrentWorkspace.CurrentArticle.ChangeArticleFinishedState(false);
            }
            else
            {
                // Переходим из редактирования в finished

                Global.CurrentWorkspace.CurrentArticle.ChangeArticleText(articleTextEdit.Text);
                Global.CurrentWorkspace.CurrentArticle.ChangeArticleFinishedState(true);
            }

            UpdateVisuals();
        }

        private void ArticleOpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog(Window.GetWindow(this)).Value)
                articleTextEdit.Text = System.IO.File.ReadAllText(openFileDialog.FileName);
        }

        private void FinishTranslationButton_Click(object sender, RoutedEventArgs e)
        {
        }

        private void TranslationOpenFileButton_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
