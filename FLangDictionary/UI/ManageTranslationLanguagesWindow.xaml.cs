using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace FLangDictionary.UI
{
    /// <summary>
    /// Логика взаимодействия для BrowseWorkspacesWindow.xaml
    /// </summary>
    public partial class ManageTranslationLanguagesWindow : Window
    {
        public ManageTranslationLanguagesWindow()
        {
            InitializeComponent();
        }

        private void UpdateLanguagesList(string languageCodeToSelect = null)
        {
            languagesList.Items.Clear();

            foreach (var language in Global.CurrentWorkspace.TranslationLanguages)
            {
                ListBoxItem itemToBeAdded = new ListBoxItem();
                itemToBeAdded.Content = language.DisplayName;
                itemToBeAdded.Tag = language;

                languagesList.Items.Add(itemToBeAdded);

                if (language.Code == languageCodeToSelect)
                    languagesList.SelectedIndex = languagesList.Items.Count - 1;
            }

            if (languageCodeToSelect == null)
                languagesList.SelectedIndex = languagesList.Items.Count - 1;
        }

        // Возвращает выбранный пользователем язык
        // Если ничего не выбрано - вернет null
        private Logic.Languages.Language ChosenLanguage
        {
            get
            {
                if (languagesList.SelectedItem == null)
                    return null;
                return (languagesList.SelectedItem as ListBoxItem).Tag as Logic.Languages.Language;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLanguagesList();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            List<Logic.Languages.Language> languagesToChooseFrom = new List<Logic.Languages.Language>(Global.Languages.InAlphabetOrder);
            languagesToChooseFrom.RemoveAll((lang) => { return Global.CurrentWorkspace.TranslationLanguages.Contains(lang); });

            string[] languageNamesToChooseFrom = new string[languagesToChooseFrom.Count];
            for (int i = 0; i < languagesToChooseFrom.Count; i++)
                languageNamesToChooseFrom[i] = languagesToChooseFrom[i].DisplayName;

            InputComboBoxWindow inputComboBoxWindow = new InputComboBoxWindow(languageNamesToChooseFrom, 0,
                this.Lang("AddLanguageDialog.Title"), this.Lang("AddLanguageDialog.Label"),
                this.Lang("YesButtonCaption"), this.Lang("NoButtonCaption"));
            inputComboBoxWindow.Owner = this;
            inputComboBoxWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (inputComboBoxWindow.ShowDialog().Value)
            {
                Global.CurrentWorkspace.AddTranslationLanguage(languagesToChooseFrom[inputComboBoxWindow.InputComboBoxIndex].Code);
                UpdateLanguagesList(languagesToChooseFrom[inputComboBoxWindow.InputComboBoxIndex].Code);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenLanguage != null)
            {
                if (UICommon.ShowDialog_TwoButton(this, this.Lang("DeleteLanguageDialog.Title"), this.Lang("DeleteLanguageDialog.Message"),
                    this.Lang("YesButtonCaption"), this.Lang("NoButtonCaption")))
                {
                    Global.CurrentWorkspace.DeleteTranslationLanguage(ChosenLanguage.Code);
                    UpdateLanguagesList();
                }
            }
        }
    }
}
