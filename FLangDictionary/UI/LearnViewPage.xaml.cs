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
    /// Логика взаимодействия для LearnViewPage.xaml
    /// </summary>
    public partial class LearnViewPage : Page
    {
        public LearnViewPage()
        {
            InitializeComponent();
        }

        // Тестовая шляпа
        private void UpdateLabel()
        {
            label.Content = $"Learn page. Article = {Global.CurrentWorkspace.CurrentArticle.Name}";
        }

        private void CurrentArticleChangedHandler(object sender, EventArgs e)
        {
            if (Global.CurrentWorkspace.CurrentArticle != null)
                UpdateLabel();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Global.CurrentArticleChanged += CurrentArticleChangedHandler;

            UpdateLabel();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            Global.CurrentArticleChanged -= CurrentArticleChangedHandler;
        }
    }
}
