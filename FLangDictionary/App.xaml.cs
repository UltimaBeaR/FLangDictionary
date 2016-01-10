using System.IO;
using System.Windows;

namespace FLangDictionary
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!Directory.Exists(Global.DataDirectory))
                Directory.CreateDirectory(Global.DataDirectory);
            if (!Directory.Exists(Global.WorkspacesDirectory))
                Directory.CreateDirectory(Global.WorkspacesDirectory);

            Global.InitializePreferences();
            Global.SetUILanguageFromPreferences();
        }
    }
}
