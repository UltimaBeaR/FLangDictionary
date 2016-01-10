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
    public partial class InputBoxWindow : Window
    {
        // Делегат проверяющий входные данные этого окна (вводимые пользователем)
        // метод должен проверить input и на основании проверки либо разрешить его, либо запретить
        // Для разрешения нужно вернуть null, иначе возвращается текст описания ошибки
        public delegate string ValidateInputCallback(string input);

        private ValidateInputCallback m_validateInputCallback;

        public string Input { get { return inputTextBox.Text; } }

        public InputBoxWindow(string title = "Input box", string label = "Input", string okCaption = "Ok", string cancelCaption = "Cancel", string initialInput = "", ValidateInputCallback validateInputCallback = null)
        {
            m_validateInputCallback = validateInputCallback;
            InitializeComponent();

            Title = title;
            this.label.Content = label;
            okButton.Content = okCaption;
            cancelButton.Content = cancelCaption;
            inputTextBox.Text = initialInput;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
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
            string error = null;
            if (m_validateInputCallback != null)
                error = m_validateInputCallback(inputTextBox.Text);

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
