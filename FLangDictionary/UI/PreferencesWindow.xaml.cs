using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace FLangDictionary.UI
{
    /// <summary>
    /// Логика взаимодействия для PreferencesWindow.xaml
    /// </summary>
    public partial class PreferencesWindow : Window
    {
        bool m_doReactOnComboboxLanguagesSelectionChanged;

        public PreferencesWindow()
        {
            InitializeComponent();
        }

        void InitComboboxUILanguage(string language)
        {
            foreach (ComboBoxItem item in comboBoxUILanguage.Items)
            {
                if (language == (item.Tag as string))
                {
                    item.IsSelected = true;
                    return;
                }
            }

            Debug.Assert(language != Global.defaultCultureName);
            InitComboboxUILanguage(Global.defaultCultureName);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Делаем бэкап настроек, чтобы в случае клаца по кнопке Reset, можно было откатиться назад
            Global.Preferences.MakeBackup();

            m_doReactOnComboboxLanguagesSelectionChanged = false;
            InitComboboxUILanguage(Global.Preferences.UILanguage);
            m_doReactOnComboboxLanguagesSelectionChanged = true;
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            Global.Preferences.Save();
        }

        private void comboBoxUILanguage_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!m_doReactOnComboboxLanguagesSelectionChanged)
                return;

            ComboBoxItem selectedItem = comboBoxUILanguage.SelectedItem as ComboBoxItem;
            string selectedLanguage = selectedItem.Tag as string;

            Global.Preferences.UILanguage = selectedLanguage;
            Global.SetUILanguageFromPreferences();
        }
    }
}
