using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public string Name { get { return m_name; } }
        public Logic.Languages.Language Language { get { return m_language; } }
        public string[] ArticleNames { get { return m_articleNames; } }

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

        // Открывает заданную по имени статью, то есть делает ее текущей (при этом происходит полная ее загрузка из бд, парсинг на синтаксическую разметку и так далее)
        // Если при этом какая-то статья была открыта, она закроется
        public void OpenArticle(string articleName)
        {
            // Заданная статья должна существовать
            Debug.Assert(articleName != null && m_articleNames.Contains(articleName));
        }

        // Обновление кэша, которое происходит сразу после создания объекта этого класса
        private void InitialUpdate()
        {
            // Обновим язык этой рабочей области
            string languageCode = m_repository.GetWorkspaceLanguageCode();
            m_language = Global.Languages.GetByCode(languageCode);

            // Обновим имена статей
            UpdateArticleNames();
        }

        // Обновляет кэш список имен статей на основании данных из БД
        private void UpdateArticleNames()
        {
            // Получаем и кэшируем список статей из репозитория
            m_articleNames = m_repository.GetArticleNames();
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
