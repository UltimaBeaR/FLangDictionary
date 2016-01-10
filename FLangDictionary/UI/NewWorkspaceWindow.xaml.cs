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
using System.Windows.Shapes;

namespace FLangDictionary.UI
{
    /// <summary>
    /// Логика взаимодействия для NewEntityWindow.xaml
    /// </summary>
    public partial class NewWorkspaceWindow : Window
    {
        public string Input { get { return inputTextBox.Text; } }

        public string LanguageCode { get { return (languageList.SelectedItem as ComboBoxItem).Tag as string; } }

        public NewWorkspaceWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Сначала нужно получить имя имя доступной новой рабочей области по умолчанию

            const string defaultFreeWorkspaceNameBase = "Workspace";
            int defaultFreeWorkspaceNameNumber = 1;

            string defaultFreeWorkspaceName;
            do
                defaultFreeWorkspaceName = $"{defaultFreeWorkspaceNameBase}{defaultFreeWorkspaceNameNumber++}";
            while (Data.Workspace.Exists(defaultFreeWorkspaceName));

            inputTextBox.Text = defaultFreeWorkspaceName;

            // Теперь нужно заполнить список языками

            foreach (var lang in Global.Languages.InAlphabetOrder)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = $"{lang.EnglishName} - {lang.Name}";
                item.Tag = lang.Code;
                languageList.Items.Add(item);
            }

            languageList.SelectedIndex = 0;

            Validate();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void inputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Validate();
        }

        private void Validate()
        {
            // Проверяем, то что вводит юзер на валидность и что такой рабочей области еще не создано

            string error;
            if (!Data.Workspace.IsValidName(Input))
                error = this.Lang("Error.IllegalItemName");
            else if (Data.Workspace.Exists(Input))
                error = this.Lang("Error.SuchItemAlreadyExists");
            else
                error = null;

            if (error == null)
                errorLabel.Visibility = Visibility.Collapsed;
            else
            {
                errorLabel.Visibility = Visibility.Visible;
                errorLabel.Content = error;
            }

            okButton.IsEnabled = error == null;
        }
    }
}
