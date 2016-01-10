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
    /// Логика взаимодействия для ManageArticlesWindow.xaml
    /// </summary>
    public partial class ManageArticlesWindow : Window
    {
        public ManageArticlesWindow()
        {
            InitializeComponent();
        }

        // Обновляет содержимое списка статей, в соответствии с с доступными статьями в рабочей области
        private void UpdateArticlesList(string articleNameToSelect = null)
        {
            articlesList.Items.Clear();

            foreach (string articleName in Global.CurrentWorkspace.ArticleNames)
            {
                articlesList.Items.Add(articleName);

                if (articleName == articleNameToSelect)
                    articlesList.SelectedIndex = articlesList.Items.Count - 1;
            }

            if (articleNameToSelect == null)
                articlesList.SelectedIndex = articlesList.Items.Count - 1;
        }

        // Возвращает выбранное пользователем имя рабочей области из списка рабочих областей
        // Если ничего не выбрано - вернет null
        private string ChosenArticleName
        {
            get
            {
                return articlesList.SelectedItem as string;
            }
        }

        private void RenameArticle(string articleName)
        {
            // Создаем диалог, вызываем его и возвращаем результат
            InputBoxWindow newEntityWindow =
                new InputBoxWindow(
                    articleName,
                    this.Lang("RenameArticleDialog.Title"),
                    this.Lang("RenameArticleDialog.Label"),
                    this.Lang("RenameButtonCaption"),
                    this.Lang("CancelButtonCaption"),
                    (input) =>
                    {
                        // Проверяем, то что вводит юзер на валидность и что такой рабочей области еще не создано

                        if (input != articleName)
                        {
                            if (input == string.Empty)
                                return this.Lang("Error.Article.IllegalName");
                            if (Global.CurrentWorkspace.ArticleNames.Contains(input))
                                return this.Lang("Error.Article.AlreadyExists");
                        }

                        return null;
                    }
                );
            newEntityWindow.Owner = this;
            newEntityWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (newEntityWindow.ShowDialog().Value && articleName != newEntityWindow.Input)
            {
                // ToDo: переименовать статью на newEntityWindow.Input
                UpdateArticlesList();
            }
        }

        // Открывает выбранную пользователем статью
        private void OpenChosenArticle()
        {
            if (ChosenArticleName != null)
            {
                // ToDo: открыть ChosenArticleName

                MessageBox.Show($"Открыли статью {ChosenArticleName}");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateArticlesList();
        }

        private void articlesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OpenChosenArticle();
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenChosenArticle();
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            // ToDo: UP

            UpdateArticlesList(ChosenArticleName);
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            // ToDo: Down

            UpdateArticlesList(ChosenArticleName);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string articleNameToCreate = UICommon.ShowDialog_CreateNewArticle(this);
            if (articleNameToCreate != null)
            {
                // ToDo: создать статью articleNameToCreate
                UpdateArticlesList(articleNameToCreate);
            }
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenArticleName != null)
            {
                RenameArticle(ChosenArticleName);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenArticleName != null)
            {
                // ToDo: сделать OKCancel диалог (надо делать вручную окно, наподобие InputBoxWindow)

                // ToDo: выгружаем текущую статью и удаляем эту статью из рабочей области

                UpdateArticlesList();
            }
        }
    }
}