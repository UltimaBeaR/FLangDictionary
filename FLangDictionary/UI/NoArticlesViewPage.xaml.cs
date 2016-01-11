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
    /// Логика взаимодействия для NoArticlesViewPage.xaml
    /// </summary>
    public partial class NoArticlesViewPage : Page
    {
        public NoArticlesViewPage()
        {
            InitializeComponent();
        }

        // Обновляет сообщение
        private void UpdateMessage()
        {
            messageLabel.Content = string.Format(this.Lang("NoArticlesView.Message"), Global.CurrentWorkspace.Name);
        }

        private void UILanguageChangedHandler(object sender, EventArgs e)
        {
            UpdateMessage();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Global.UILanguageChanged += UILanguageChangedHandler;

            UpdateMessage();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Global.UILanguageChanged -= UILanguageChangedHandler;
        }

        private void CreateArticleButton_Click(object sender, RoutedEventArgs e)
        {
            string articleNameToCreate = UICommon.ShowDialog_CreateNewArticle(Window.GetWindow(this));
            if (articleNameToCreate != null)
            {
                Global.CurrentWorkspace.AddNewArticle(articleNameToCreate);
                Global.CurrentWorkspace.OpenArticle(articleNameToCreate);
            }
        }
    }
}
