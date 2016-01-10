using System.IO;
using System.Windows;

namespace FLangDictionary.UI
{
    /// <summary>
    /// Логика взаимодействия для BrowseWorkspacesWindow.xaml
    /// </summary>
    public partial class BrowseWorkspacesWindow : Window
    {
        public BrowseWorkspacesWindow()
        {
            InitializeComponent();
        }

        // Обновляет содержимое списка рабочих областей в соответсвии с состоянием файловой системы
        private void UpdateWorkspacesList(string workspaceNameToSelect = null)
        {
            workspacesList.Items.Clear();

            foreach (string workspacePath in Directory.EnumerateDirectories(Global.WorkspacesDirectory))
            {
                string workspaceName = Path.GetFileName(workspacePath.TrimEnd(Path.DirectorySeparatorChar).TrimEnd(Path.AltDirectorySeparatorChar));

                workspacesList.Items.Add(workspaceName);

                if (workspaceName == workspaceNameToSelect)
                    workspacesList.SelectedIndex = workspacesList.Items.Count - 1;
            }

            if (workspaceNameToSelect == null)
                workspacesList.SelectedIndex = workspacesList.Items.Count - 1;
        }

        // Возвращает выбранное пользователем имя рабочей области из списка рабочих областей
        // Если ничего не выбрано - вернет null
        private string ChosenWorkspaceName
        {
            get
            {
                return workspacesList.SelectedItem as string;
            }
        }

        private void RenameWorkspace(string workspaceName)
        {
            // Создаем диалог, вызываем его и возвращаем результат
            InputBoxWindow newEntityWindow =
                new InputBoxWindow(
                    this.Lang("RenameWorkspaceDialog.Title"),
                    this.Lang("RenameWorkspaceDialog.Label"),
                    this.Lang("RenameWorkspaceDialog.OkCaption"),
                    this.Lang("RenameWorkspaceDialog.CancelCaption"),
                    workspaceName,
                    (input) =>
                    {
                        // Проверяем, то что вводит юзер на валидность и что такой рабочей области еще не создано

                        if (input != workspaceName)
                        {
                            if (!Data.Workspace.IsValidName(input))
                                return this.Lang("Error.IllegalItemName");
                            if (Data.Workspace.Exists(input))
                                return this.Lang("Error.SuchItemAlreadyExists");
                        }

                        return null;
                    }
                );
            newEntityWindow.Owner = this;
            newEntityWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (newEntityWindow.ShowDialog().Value && workspaceName != newEntityWindow.Input)
            {
                Directory.Move(Data.Workspace.GetWorkspaceDirectory(workspaceName), Data.Workspace.GetWorkspaceDirectory(newEntityWindow.Input));
                UpdateWorkspacesList(newEntityWindow.Input);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateWorkspacesList();
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenWorkspaceName != null)
            {
                try
                {
                    Global.CurrentWorkspace = Data.Workspace.OpenExisting(ChosenWorkspaceName);
                    Close();
                }
                catch
                {
                    MessageBox.Show(this.Lang("Error.CannotOpenWorkspace"), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            NewWorkspaceWindow newWorkspaceWindow = new NewWorkspaceWindow();
            newWorkspaceWindow.Owner = this;
            newWorkspaceWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (newWorkspaceWindow.ShowDialog().Value)
            {
                Data.Workspace.CreateNew(newWorkspaceWindow.Input, newWorkspaceWindow.LanguageCode);
                UpdateWorkspacesList(newWorkspaceWindow.Input);
            }
        }

        private void RenameButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenWorkspaceName != null)
            {
                RenameWorkspace(ChosenWorkspaceName);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenWorkspaceName != null)
            {
                // ToDo: сделать OKCancel диалог (надо делать вручную окно, наподобие InputBoxWindow)

                // Выгружаем текущий открытый workspace перед удалением, в случае если мы его удаляем
                if (Global.CurrentWorkspace != null && Global.CurrentWorkspace.Name == ChosenWorkspaceName)
                    Global.CurrentWorkspace = null;

                Directory.Delete(Data.Workspace.GetWorkspaceDirectory(ChosenWorkspaceName), true);

                UpdateWorkspacesList();
            }
        }
    }
}
