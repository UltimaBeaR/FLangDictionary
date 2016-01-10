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
        }

        private enum ViewMode
        {
            None,
            Edit,
            Translate,
            Learn
        }

        private void SetViewMode(ViewMode viewMode)
        {
            switch (viewMode)
            {
                case ViewMode.Translate: contentViewFrame.Source = new Uri("TranslateViewPage.xaml", UriKind.Relative); break;
                case ViewMode.Edit: contentViewFrame.Source = new Uri("EditViewPage.xaml", UriKind.Relative); break;
                case ViewMode.Learn: contentViewFrame.Source = new Uri("LearnViewPage.xaml", UriKind.Relative); break;
                default: contentViewFrame.Source = null; break;
            }
        }

        // Обновляет заголовок окна в соответсвии с текущим языком и открытым проектом
        private void UpdateTitle()
        {
            string windowTitle = this.Lang("MainWindowTitle");
            if (Global.CurrentWorkspace != null)
                windowTitle = $"{Global.CurrentWorkspace.Name}({Global.CurrentWorkspace.Language.Name}) - {windowTitle}";

            Title = windowTitle;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Подписываемся на глобальные события

            Global.CurrentWorkspaceChanged += CurrentWorkspaceChangedHandler;
            Global.UILanguageChanged += UILanguageChangedHandler;            
        }

        // Вызвается при изменении языка UI
        private void UILanguageChangedHandler(object sender, EventArgs e)
        {
            // При смене языка обновляем заголвок - он не обновится автоматически,
            // так как кроме фразы на текущем языке содержит еще и название открытой рабочей области
            UpdateTitle();
        }

        // Вызывается при событии смены текущей рабочей области
        private void CurrentWorkspaceChangedHandler(object sender, EventArgs e)
        {
            // Обновляем заголовок - так как в нм отображается текущий открытый проект
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
        }

        private void MenuItem_Article_Manage_Click(object sender, RoutedEventArgs e)
        {
        }

        private void MenuItem_Article_ManageLanguages_Click(object sender, RoutedEventArgs e)
        {
        }

        private void MenuItem_View_Edit_Click(object sender, RoutedEventArgs e)
        {
            SetViewMode(ViewMode.None);
        }

        private void MenuItem_View_Translate_Click(object sender, RoutedEventArgs e)
        {
            SetViewMode(ViewMode.Translate);
        }

        private void MenuItem_View_Learn_Click(object sender, RoutedEventArgs e)
        {
            SetViewMode(ViewMode.None);
        }

        private void MenuItem_Help_About_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
