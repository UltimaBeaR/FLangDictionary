using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;

namespace FLangDictionary.UI
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            m_currentViewMode = ViewMode.None;
        }

        private enum ViewMode
        {
            None,
            WorkspaceIsNotLoaded,
            NoArticles,
            Edit,
            Translate,
            Learn
        }

        private ViewMode m_currentViewMode;

        private ViewMode CurrentViewMode
        {
            get
            {
                return m_currentViewMode;
            }
            set
            {
                m_currentViewMode = value;
                switch (m_currentViewMode)
                {
                    case ViewMode.WorkspaceIsNotLoaded: contentViewFrame.Source = new Uri("WorkspaceIsNotLoadedViewPage.xaml", UriKind.Relative); break;
                    case ViewMode.NoArticles: contentViewFrame.Source = new Uri("NoArticlesViewPage.xaml", UriKind.Relative); break;
                    case ViewMode.Edit: contentViewFrame.Source = new Uri("EditViewPage.xaml", UriKind.Relative); break;
                    case ViewMode.Translate: contentViewFrame.Source = new Uri("TranslateViewPage.xaml", UriKind.Relative); break;
                    case ViewMode.Learn: contentViewFrame.Source = new Uri("LearnViewPage.xaml", UriKind.Relative); break;

                    case ViewMode.None:
                    default: contentViewFrame.Source = null; break;
                }
            }
        }

        // Обновляет заголовок окна в соответсвии с текущим языком и открытым проектом
        private void UpdateTitle()
        {
            string windowTitle = this.Lang("MainWindowTitle");
            if (Global.CurrentWorkspace != null)
                windowTitle = $"{Global.CurrentWorkspace.Name} - {Global.CurrentWorkspace.Language.Name} - {windowTitle}";

            Title = windowTitle;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Подписываемся на глобальные события

            Global.CurrentArticleOpened += CurrentArticleOpenedHandler;
            Global.AfterCurrentWorkspaceSet += CurrentWorkspaceSetHandler;
            Global.UILanguageChanged += UILanguageChangedHandler;

            // При первом показе окна переходим в вид незагруженной рабочей области
            CurrentViewMode = ViewMode.WorkspaceIsNotLoaded;           
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            // Отписываемся от всех событий

            Global.CurrentArticleOpened -= CurrentArticleOpenedHandler;
            Global.AfterCurrentWorkspaceSet -= CurrentWorkspaceSetHandler;
            Global.UILanguageChanged -= UILanguageChangedHandler;
        }

        // Вызывается при событии смены текущей рабочей области
        private void CurrentArticleOpenedHandler(object sender, EventArgs e)
        {
            // Если Статей не осталось (после удаления всех статей) - открываем экран с сообщением что нет статей
            if (Global.CurrentWorkspace.ArticleNames.Length == 0)
                CurrentViewMode = ViewMode.NoArticles;
            // Если статья меняется, в то время как был пустой режим
            // (После добавления первой статьи  сразу или после открытия рабочей области) - открываем режим чтения как дефолтный
            else if (CurrentViewMode == ViewMode.None || CurrentViewMode == ViewMode.NoArticles)
                CurrentViewMode = ViewMode.Learn;
        }

        // Вызывается при событии смены текущей рабочей области
        private void CurrentWorkspaceSetHandler(object sender, EventArgs e)
        {
            // Обновляем заголовок - так как в нм отображается текущий открытый проект
            UpdateTitle();

            if (Global.CurrentWorkspace == null)
                CurrentViewMode = ViewMode.WorkspaceIsNotLoaded;
            else
            {
                // Если открыли существующую рабочую область

                if (Global.CurrentWorkspace.ArticleNames.Length == 0)
                    CurrentViewMode = ViewMode.NoArticles;
                else
                {
                    CurrentViewMode = ViewMode.None;
                    // Открываем первую в порядке следования статью
                    Global.CurrentWorkspace.OpenArticle(Global.CurrentWorkspace.ArticleNames[0]);
                }
            }
        }

        // Вызвается при изменении языка UI
        private void UILanguageChangedHandler(object sender, EventArgs e)
        {
            // При смене языка обновляем заголвок - он не обновится автоматически,
            // так как кроме фразы на текущем языке содержит еще и название открытой рабочей области
            UpdateTitle();
        }

        private void MenuItem_File_Workspace_New_Click(object sender, RoutedEventArgs e)
        {
            NewWorkspaceWindow newWorkspaceWindow = new NewWorkspaceWindow();
            newWorkspaceWindow.Owner = this;
            newWorkspaceWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (newWorkspaceWindow.ShowDialog().Value)
            {
                Global.CurrentWorkspace = Data.Workspace.CreateNew(newWorkspaceWindow.Input, newWorkspaceWindow.LanguageCode);
            }
        }

        private void MenuItem_File_Workspace_Browse_Click(object sender, RoutedEventArgs e)
        {
            BrowseWorkspacesWindow browseWorkspacesWindow = new BrowseWorkspacesWindow();
            browseWorkspacesWindow.Owner = this;
            browseWorkspacesWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            browseWorkspacesWindow.ShowDialog();
        }

        private void MenuItem_File_Workspace_Close_Click(object sender, RoutedEventArgs e)
        {
            Global.CurrentWorkspace = null;
        }

        private void MenuItem_File_ShowInFileExplorer_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(Global.DataDirectory);
        }

        private void MenuItem_File_Preferences_Click(object sender, RoutedEventArgs e)
        {
            PreferencesWindow preferences = new PreferencesWindow();
            preferences.Owner = this;
            preferences.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            preferences.ShowDialog();
        }

        private void MenuItem_File_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void MenuItem_Article_New_Click(object sender, RoutedEventArgs e)
        {
            if (Global.CurrentWorkspace != null)
            {
                string articleNameToCreate = UICommon.ShowDialog_CreateNewArticle(this);
                if (articleNameToCreate != null)
                {
                    Global.CurrentWorkspace.AddNewArticle(articleNameToCreate);
                    Global.CurrentWorkspace.OpenArticle(articleNameToCreate);
                }
            }
        }

        private void MenuItem_Article_Manage_Click(object sender, RoutedEventArgs e)
        {
            if (Global.CurrentWorkspace != null)
            {
                ManageArticlesWindow manageArticlesWindow = new ManageArticlesWindow();
                manageArticlesWindow.Owner = this;
                manageArticlesWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                manageArticlesWindow.ShowDialog();
            }
        }

        private void MenuItem_Article_ManageLanguages_Click(object sender, RoutedEventArgs e)
        {
            if (Global.CurrentWorkspace != null)
            {
                ManageTranslationLanguagesWindow manageLanguagesWindow = new ManageTranslationLanguagesWindow();
                manageLanguagesWindow.Owner = this;
                manageLanguagesWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                manageLanguagesWindow.ShowDialog();
            }
        }

        private void MenuItem_View_Edit_Click(object sender, RoutedEventArgs e)
        {
            if (Global.CurrentWorkspace != null && Global.CurrentWorkspace.CurrentArticle != null)
            {
                CurrentViewMode = ViewMode.Edit;
            }
        }

        private void MenuItem_View_Translate_Click(object sender, RoutedEventArgs e)
        {
            if (Global.CurrentWorkspace != null && Global.CurrentWorkspace.CurrentArticle != null)
            {
                CurrentViewMode = ViewMode.Translate;
            }
        }

        private void MenuItem_View_Learn_Click(object sender, RoutedEventArgs e)
        {
            if (Global.CurrentWorkspace != null && Global.CurrentWorkspace.CurrentArticle != null)
            {
                CurrentViewMode = ViewMode.Learn;
            }
        }

        private void MenuItem_Help_About_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
