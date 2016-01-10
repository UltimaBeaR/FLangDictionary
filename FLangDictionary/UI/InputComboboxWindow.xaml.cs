using System.Windows;
using System.Windows.Controls;

namespace FLangDictionary.UI
{
    /// <summary>
    /// Универсальное диалоговое окно, для запроса текстовой строки из списка текстовых строк через Combobox
    /// При создании в конструкторе указываются названия заголовков и кнопок
    /// В итоге через свойство InputComboBoxIndex можно забрать из диалога выбранный индекс из комбобокса
    /// (этот индекс будет соответствовать индексу в переданном в конструкторе массиве значений для комбобокса)
    /// </summary>
    public partial class InputComboBoxWindow : Window
    {
        public int InputComboBoxIndex { get { return inputComboBox.SelectedIndex; } }

        public InputComboBoxWindow(string[] inputComboBoxItems, int initialInputComboBoxIndex = 0, string title = "Input box", string label = "Input", string okCaption = "Ok", string cancelCaption = "Cancel")
        {
            InitializeComponent();

            Title = title;
            this.label.Content = label;
            okButton.Content = okCaption;
            cancelButton.Content = cancelCaption;

            if (inputComboBoxItems != null)
            {
                foreach (var item in inputComboBoxItems)
                    inputComboBox.Items.Add(item);

                if (initialInputComboBoxIndex >= 0 && initialInputComboBoxIndex < inputComboBoxItems.Length)
                    inputComboBox.SelectedIndex = initialInputComboBoxIndex;
            }
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
    }
}