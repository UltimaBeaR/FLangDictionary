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
    // ToDo: Когда все основные функции будут завершены, можно сделать некий режим тестирования на знание слов и фраз
    // То есть из переведенных слов и фраз в статье рандомно берется фраза, и юзеру выдается предложение, в которой идет эта фраза
    // (Если предложение очень, короткое, можно соседние предложения либо части этих предложений тоже впендюрить. 
    // Это предложение проигрывается с помощью аудио-записи, если она есть. Фраза/слово подсвечивается при этом выделением.
    // И предлагается перевести эту фразу на один из языков с помощью теста, например из 4ех вариантов.

    // ToDo: Еще есть вариант реализовать что-то типа опросника по методике Effortless english-а. Надо будет для этого по статье заполнять
    // простые тупые вопросы - кучу простых тупых вопросов. Можно при этом на сами вопросы тоже озвучку прилеплять (тогда например можно использовать аудио материал из effortless english)
    // Эти тупые вопросы либо по порядку, либо рандомно спрашивать, далее юзер должен громко (или про себя) ответить, далее юзер нажимает кнопочку - сигнал что ответ дан (например пробел на клаве)
    // После этого появляется ответ на этот вопрос (опять же вместе с озвучкой). То есть эффект как от ответа на вопросы по программе effortless english, но есть время на ответ + виден текст
    // Можно сделать ленивый вариант этого дела без текста (только аудио), либо без аудио(только текст), ну либо и то и другое
    // Тогда например можно взять нарезать вопросы - ответы из effortless english и не писать к ним текст.
    // Как вариант для этого можно сделать специальный тип статьи - он будет иметь какойнибудь особенный тип (чтобы при обычной смене статей в learning-mode он не высвечивался)
    // И в него к примеру писать вопрос - ответ. Каждый вопрос и ответ на новой строке, либо отделенные спец.символом, чтобы это все распарсить.
    // Тогда ненадо будет тот же самый фукнционал реализовывать - уже будет объект статьи с распаршенными словами, назначенным переводом и аудио, и его можно просто юзать в режиме опросника
    // Эту же байду потом можно использовать и в версии для телефона-планшета, в режиме только аудио - там на ответ просто можно давать какое-то время (можно например в самом тексте с вопросами-ответами его задавать)

    // ToDo: Идея, как сделать удобно привязку аудио к тексту - Можно сделать граф звуковой волны как длинная горизонтальная линия, а прямо под ним текст статьи в одну строку, также в линию
    // (Плюс общий элемент для обоих "полос" - скроллер, для прокрутки от начала до конца (графа/статьи)).
    // Юзер должен будет проигрывая звуковой граф ставить в нем метки и эти метки подгонять к границам слов, которые визуально находятся внизу. То есть деформировать форму графа
    // Так чтобы она была под слова. Либо например, юзер клацает на слово, появляется прямоугольничек на графе, который можно растягивать (устанавливать начало и конец) и нужно
    // Этим прямоугольничком указать в начало и конец места где идет слово в графе. При этом при каждой установке такого прямоугольничка последовательно, проигрывание графа
    // Идет уже не сначала всего звукового файла а с места последнего установленного прямоугольничка, который находится ДО устанавливаемого прямоугольничка (возможно это место минус некоторый оффсет, чтобы разобрать слова)
    // Это будет и наглядно и быстро и удобно. При этом не обязательно устанавливать именно отдельные слова (возможно структуру бд придется подправить, непомню как там щас с этим) - 
    // возможно выделить несколько слов, фраз или предложений в кучу и для них уже будет большой прямоугольник и его уже размещать. Просто если например это идет разметка
    // Звука для вопросов-ответов по системе effortless english - то там вобще не нужно знать где слова связаны с аудио - там нужно только начало и конец вопроса/ответа.
    // То есть в БД дожно быть аудио связано с индексами слов в разметке - то есть отдельные прямоугольничики, каждый из которых имеет начальный и конечный индекс слова в разметке
    // и привязанные к прямоугольничку временные метки начала/конца оффсета в аудио-файле

    // ToDo: Нужно сделать поддержку выделения текста, но не выделять по одной букве а сразу по слову, тоесть юзер тащит мышку и выделяются все слова, которые под мышкой, плюс ctrl тоже должен работать.

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

        // Происходит при смене текущей статьи в текущей рабочей области
        // НЕ вызывается при изменениях текущей рабочей области
        public static event EventHandler CurrentArticleChanged;
        // Происходит при смене текущей рабочей области
        public static event EventHandler CurrentWorkspaceChanged;
        // Происходит при смене языка пользовательского интерфейса
        public static event EventHandler UILanguageChanged;

        private static Data.Workspace m_currentWorkspace;

        // Обработик события смены статьи из конкретного Workspace
        private static void CurrentWorkspace_CurrentArticleChangedHandler(object sender, EventArgs e)
        {
            // Делегируем событие на глобальный обработчик
            if (CurrentArticleChanged != null)
                CurrentArticleChanged(sender, e);
        }

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
                    // Если есть старая рабочая область, отпишемся от ее события изменения статьи
                    if (m_currentWorkspace != null)
                        m_currentWorkspace.CurrentArticleChanged -= CurrentWorkspace_CurrentArticleChangedHandler;

                    m_currentWorkspace = value;

                    // Если есть новая рабочая область, подпишемся на ее события изменения статьи
                    if (m_currentWorkspace != null)
                        m_currentWorkspace.CurrentArticleChanged += CurrentWorkspace_CurrentArticleChangedHandler;

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
