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
    public partial class WorkspaceIsNotLoadedViewPage : Page
    {
        public WorkspaceIsNotLoadedViewPage()
        {
            InitializeComponent();
        }

        private void BrowseWorkspacesButton_Click(object sender, RoutedEventArgs e)
        {
            BrowseWorkspacesWindow browseWorkspacesWindow = new BrowseWorkspacesWindow();
            browseWorkspacesWindow.Owner = Window.GetWindow(this);
            browseWorkspacesWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            browseWorkspacesWindow.ShowDialog();
        }
    }
}
