using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FLangDictionary.Logic;

namespace FLangDictionary.Data
{
    // Рабочая область. Это для этой программы, как проект для вижуал студии.
    // С рабочей областью ассоциирована папка в директории с рабочими областями, а внутри папки - файл с базой данных и дополнительные файлы,
    // например аудио-записи, которые связанны со статьями
    // То есть по сути это программное представление папки рабочей области
    class Workspace
    {
        // Репозиторий данных для этой рабочей области (менеджер, через который идет работа с БД)
        private WorkspaceRepository m_repository;

        // Имя рабочей области. Совпадает с именем папки в директории рабочих областей
        private string m_name;

        // Кэш языка этой рабочей области
        private Logic.Languages.Language m_language;

        // Имена статей, которые есть в этой рабочей области.
        // Хранятся тут для быстрого доступа к списку статей (для выбора статьи), без залезания в БД
        private string[] m_articleNames;

        // Языки для перевода. Кэш. Обновляется и используется подобно именам статей.
        private Logic.Languages.Language[] m_translationLanguages;

        private Article m_currentArticle;

        public string Name { get { return m_name; } }
        public Logic.Languages.Language Language { get { return m_language; } }
        public string[] ArticleNames { get { return m_articleNames; } }
        public Logic.Languages.Language[] TranslationLanguages { get { return m_translationLanguages; } }

        // Текущая статья. Если ни одной статьи у этой рабочей области нет, то тут будет null
        public Article CurrentArticle { get { return m_currentArticle; } }

        // Происходит в момент открытия статьи, но перед открытыием. То есть в этот момент текущей статьей будет еще старая статья
        public event EventHandler CurrentArticleOpening;
        // Происходит при открытии очередной статьи (которая записывается после открытия в CurrentArticle)
        // Не вызывается при создании рабочей области. При создании CurrentArticle всегда = null
        // Вызывается даже в случае если переоткрывается та же самая статья
        public event EventHandler CurrentArticleOpened;

        private Workspace(string workspaceName, bool createNew = false, string languageCode = null)
        {
            // Код языка должен быть задан, если создаем новую рабочую область
            Debug.Assert(!createNew || languageCode != null);

            m_name = workspaceName;

            string workspacePath = GetWorkspaceDirectory(m_name);

            string repositoryFileName = Path.Combine(workspacePath, "data.db");

            if (createNew)
                m_repository = WorkspaceRepository.CreateNew(repositoryFileName, languageCode);
            else
            {
                m_repository = WorkspaceRepository.OpenExisting(repositoryFileName);

                // Если файла репозитория не существует
                if (m_repository == null)
                    throw new Exception("database is not found");
            }

            // Апдейтим кэшируемые данные
            InitialUpdate();
        }

        // Добавляет новую статью
        public void AddNewArticle(string articleName)
        {
            // Заданная статья не должна существовать
            Debug.Assert(articleName != null && !m_articleNames.Contains(articleName));

            // Добавляем статью
            m_repository.AddArticle(articleName);

            // Обновим список статей, так как добавилась новая статья
            UpdateArticleNames();
        }

        public void DeleteArticle(string articleName)
        {
            // Заданная статья должна существовать
            Debug.Assert(m_articleNames.Contains(articleName));

            bool deletingCurrentArticle = CurrentArticle.Name == articleName;

            m_repository.DeleteArticle(articleName);

            // Обновим список статей, так как удалилась статья
            UpdateArticleNames();

            if (deletingCurrentArticle)
            {
                if (ArticleNames.Length == 0)
                    OpenArticle(null);
                else
                    OpenArticle(ArticleNames[0]);
            }
        }

        public void RenameArticle(string articleName, string newArticleName)
        {
            m_repository.RenameArticle(articleName, newArticleName);

            UpdateArticleNames();

            // Если сейчас открыта переименовываемая статья - переоткроем ее (с новым именем), чтобы заного считались все новые данные в статье и вызвалось событие
            // обновляющее UI
            if (CurrentArticle != null && articleName == CurrentArticle.Name)
                OpenArticle(newArticleName);
        }

        // Перемещает статью вверх или вниз по списку статей
        public void MoveArticle(string articleName, bool up)
        {
            m_repository.MoveArticle(articleName, up);
            UpdateArticleNames();
        }

        // Открывает заданную по имени статью, то есть делает ее текущей (при этом происходит полная ее загрузка из бд, парсинг на синтаксическую разметку и так далее)
        // Если при этом какая-то статья была открыта, она закроется
        public void OpenArticle(string articleName)
        {
            if (CurrentArticleOpening != null)
                CurrentArticleOpening(m_currentArticle, EventArgs.Empty);

            if (articleName != null)
            {
                // Заданная статья должна существовать
                Debug.Assert(m_articleNames.Contains(articleName));

                m_currentArticle = new Article(this, m_repository, articleName);
            }
            else
                m_currentArticle = null;

            if (CurrentArticleOpened != null)
                CurrentArticleOpened(m_currentArticle, EventArgs.Empty);
        }

        public void AddTranslationLanguage(string languageCode)
        {
            Debug.Assert(languageCode != null && languageCode != string.Empty);

            if (m_currentArticle != null)
                m_currentArticle.BeforeTranslationLanguagesChanged();

            m_repository.AddTranslationLanguage(languageCode);

            UpdateTranslationLanguages();

            if (m_currentArticle != null)
                m_currentArticle.AfterTranslationLanguagesChanged();
        }

        public void DeleteTranslationLanguage(string languageCode)
        {
            Debug.Assert(languageCode != null && languageCode != string.Empty);

            if (m_currentArticle != null)
                m_currentArticle.BeforeTranslationLanguagesChanged();

            m_repository.DeleteTranslationLanguage(languageCode);

            UpdateTranslationLanguages();

            if (m_currentArticle != null)
                m_currentArticle.AfterTranslationLanguagesChanged();
        }

        // Обновление кэша, которое происходит сразу после создания объекта этого класса
        private void InitialUpdate()
        {
            // Обновим язык этой рабочей области
            string languageCode = m_repository.GetWorkspaceLanguageCode();
            m_language = Global.Languages.GetByCode(languageCode);

            // Обновим имена статей
            UpdateArticleNames();
            // И языки для перевода
            UpdateTranslationLanguages();
        }

        // Обновляет кэш список имен статей на основании данных из БД
        private void UpdateArticleNames()
        {
            // Получаем и кэшируем список статей из репозитория
            m_articleNames = m_repository.GetArticleNames();
        }

        // Обновляет кэш список языков для перевода на основании данных из БД
        private void UpdateTranslationLanguages()
        {
            // Получаем и кэшируем список статей из репозитория
            string[] languageCodes = m_repository.GetTranslationLanguageCodes();

            List<Logic.Languages.Language> languagesList = new List<Logic.Languages.Language>(languageCodes.Length);

            foreach (var languageCode in languageCodes)
                languagesList.Add(Global.Languages.GetByCode(languageCode));

            // Сортируем по английскому имени (при запросе из базы такая сортировка невозможна, так как база хранит только коды языков)
            languagesList.SortByEnglishName();

            m_translationLanguages = languagesList.ToArray();
        }

        public static Workspace CreateNew(string workspaceName, string languageCode)
        {
            string workspacePath = GetWorkspaceDirectory(workspaceName);

            if (File.Exists(workspacePath))
                File.Delete(workspacePath);
            if (Directory.Exists(workspacePath))
                Directory.Delete(workspacePath);

            Directory.CreateDirectory(workspacePath);

            return new Workspace(workspaceName, true, languageCode);
        }

        public static Workspace OpenExisting(string workspaceName)
        {
            string workspacePath = GetWorkspaceDirectory(workspaceName);

            if (!File.GetAttributes(workspacePath).HasFlag(FileAttributes.Directory))
                return null;

            return new Workspace(workspaceName);
        }

        // Проверяет имя рабочей области на правильность. При этом не проверяет на существование
        public static bool IsValidName(string workspaceName)
        {
            if (workspaceName == string.Empty || workspaceName.IndexOfAny(new char[] { '\\', '/' }) >= 0)
                return false;

            // Метод проверки возможно не самый красивый, но зато рабочий
            try
            {
                var directoryInfo = new DirectoryInfo(GetWorkspaceDirectory(workspaceName));
                return true;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is PathTooLongException || ex is NotSupportedException)
            {
                return false;
            }
        }

        // Существует ли уже рабочая область с заданным именем в директории рабочих областей
        public static bool Exists(string workspaceName)
        {
            string workspacePath = GetWorkspaceDirectory(workspaceName);
            return Directory.Exists(workspacePath) || File.Exists(workspacePath);
        }

        // Получает путь в файловой системе для рабочей области с заданным именем
        public static string GetWorkspaceDirectory(string workspaceName)
        {
            return Path.Combine(Global.WorkspacesDirectory, workspaceName);
        }
    }
}
