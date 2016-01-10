using System.Windows;
using System.Windows.Controls;

namespace FLangDictionary.UI
{
    /// <summary>
    /// Универсальное диалоговое окно, для запроса текстовой строки
    /// При создании в конструкторе указываются названия заголовков и кнопок, а также делегат на функцию - обработчик изменения текстовой строки (можно оставить пустым)
    /// В заданном делегате можно вернуть текст ошибки (он будет отображен в окне), в случае если нельзя принять эту текстовую строку
    /// В итоге через свойство Input можно забрать из диалога напечатанную строку
    /// </summary>
    public partial class InputBoxWindow : Window
    {
        // Делегат проверяющий входные данные этого окна (вводимые пользователем)
        // метод должен проверить input и на основании проверки либо разрешить его, либо запретить
        // Для разрешения нужно вернуть null, иначе возвращается текст описания ошибки
        public delegate string ValidateInputCallback(string input);

        private ValidateInputCallback m_validateInputCallback;

        public string Input { get { return inputTextBox.Text; } }

        public InputBoxWindow(string initialInput = "", string title = "Input box", string label = "Input", string okCaption = "Ok", string cancelCaption = "Cancel", ValidateInputCallback validateInputCallback = null)
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
                errorMessage.Visibility = Visibility.Collapsed;
            else
            {
                errorMessage.Visibility = Visibility.Visible;
                errorMessage.Text = error;
            }

            okButton.IsEnabled = error == null;
        }
    }
}
