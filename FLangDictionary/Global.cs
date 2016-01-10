using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;

namespace FLangDictionary
{
    static class Global
    {
        private static string m_uiLanguage;

        public static void JitCompileCurrentAssembly()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var asmb = Assembly.GetExecutingAssembly();
                if (!asmb.GlobalAssemblyCache)
                {
                    foreach (Type type in asmb.GetTypes())
                    {
                        if (!type.IsInterface && !type.IsGenericTypeDefinition)
                        {
                            foreach (MethodInfo method in type.GetMethods(
                                BindingFlags.DeclaredOnly |
                                BindingFlags.NonPublic |
                                BindingFlags.Public |
                                BindingFlags.Instance |
                                BindingFlags.Static))
                            {
                                if (!method.IsAbstract && !method.IsGenericMethodDefinition && !method.ContainsGenericParameters)
                                    RuntimeHelpers.PrepareMethod(method.MethodHandle);
                            }
                        }
                    }
                }

                //MessageBox.Show("finished");
            });
        }

        public const string defaultCultureName = "en";

        // Возвращает папку, в которую можно сохранять данные (настройки, базу, аудиофайлы)
        public static string DataDirectory
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FLangDictionary");
            }
        }

        public static string WorkspacesDirectory
        {
            get
            {
                return Path.Combine(DataDirectory, "Workspaces");
            }
        }

        public static event EventHandler CurrentWorkspaceChanged;
        public static event EventHandler UILanguageChanged;

        private static Data.Workspace m_currentWorkspace;

        public static Data.Workspace CurrentWorkspace
        {
            get
            {
                return m_currentWorkspace;
            }

            set
            {
                if (m_currentWorkspace != value)
                {
                    m_currentWorkspace = value;
                    if (CurrentWorkspaceChanged != null)
                        CurrentWorkspaceChanged(m_currentWorkspace, EventArgs.Empty);
                }
            }
        }

        public static Logic.Languages Languages { get; private set; } = new Logic.Languages();

        public static Data.Preferences Preferences { get; private set; }

        public static void InitializePreferences()
        {
            Preferences = new Data.Preferences(Path.Combine(DataDirectory, "Preferences.xml"));
        }

        public static void SetUILanguageFromPreferences()
        {
            ChangeUILanguage(Preferences.UILanguage);
        }

        public static string UILanguage
        {
            get
            {
                return m_uiLanguage;
            }
            set
            {
                if (value == null || value == m_uiLanguage)
                    return;

                ChangeUILanguage(value);
            }
        }

        // Меняет текущий язык интерфейса
        private static void ChangeUILanguage(string language)
        {
            m_uiLanguage = language;

            Uri uriToLoad = DoesUISupportLanguage(m_uiLanguage) ?
                new Uri($"Resources/Lang.{m_uiLanguage}.xaml", UriKind.Relative) :
                new Uri("Resources/Lang.xaml", UriKind.Relative);

            // Создаём ResourceDictionary для нового языка
            ResourceDictionary dict = new ResourceDictionary();
            dict.Source = uriToLoad;

            // Находим старую ResourceDictionary и удаляем его и добавляем новую ResourceDictionary
            ResourceDictionary oldDict = (from d in Application.Current.Resources.MergedDictionaries
                                          where d.Source != null && d.Source.OriginalString.StartsWith("Resources/Lang.")
                                          select d).First();

            if (oldDict != null)
            {
                int ind = Application.Current.Resources.MergedDictionaries.IndexOf(oldDict);
                Application.Current.Resources.MergedDictionaries.Remove(oldDict);
                Application.Current.Resources.MergedDictionaries.Insert(ind, dict);
            }
            else
                Application.Current.Resources.MergedDictionaries.Add(dict);

            // Вызываем событие смены языка
            if (UILanguageChanged != null)
                UILanguageChanged(m_uiLanguage, EventArgs.Empty);
        }

        // Поддерживает ли заданная культура собственный вариант UI
        public static bool DoesUISupportLanguage(string language)
        {
            return CanLoadResource(new Uri($"Resources/Lang.{language}.xaml", UriKind.Relative));
        }

        private static bool CanLoadResource(Uri uri)
        {
            try
            {
                Application.GetResourceStream(uri);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }
    }
}
