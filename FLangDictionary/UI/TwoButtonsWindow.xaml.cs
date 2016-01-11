using System.Windows;
using System.Windows.Controls;

namespace FLangDictionary.UI
{
    /// <summary>
    /// Универсальное диалоговое окно, для вывода сообщения с двумя кнопками, первая(positive) вернет true в диалоге, вторая(negative) false
    /// </summary>
    public partial class TwoButtonsWindow : Window
    {
        public TwoButtonsWindow(string title = "Message box", string message = "Message", string positiveCaption = "Ok", string negativeCaption = "Cancel")
        {
            InitializeComponent();

            Title = title;
            this.message.Text = message;
            positiveButton.Content = positiveCaption;
            negativeButton.Content = negativeCaption;
        }

        private void positiveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void negativeButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}