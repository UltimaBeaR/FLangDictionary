using FLangDictionary.Logic;
using System.Collections.Generic;
using System.Linq;

namespace FLangDictionary.Data
{
    // Статья на иностранном языке. Может иметь один или несколько художественных переводов
    class Article
    {
        Workspace m_workspace;
        WorkspaceRepository m_repository;

        // Название статьи
        public string Name { get; private set; }

        // Оригинальный текст статьи (на оригинальном иностранном языке)
        public TextInLanguage OriginalText { get; private set; }

        // Список едениц перевода для этой статьи
        private List<TranslationUnit> m_translationUnits;

        // Текущий (открытый) художественный перевод
        // Может быть null. Но если идет попытка открыть текст перевода на существующий язык для перевода то тут никогда не будет null
        public TextInLanguage CurrentArtisticalTranslation { get; private set; }

        public Article(Workspace workspace, WorkspaceRepository repository, string articleName)
        {
            m_workspace = workspace;
            m_repository = repository;

            Name = articleName;

            // Запрашиваем текст статьи у БД
            WorkspaceRepository.FinishedText originalFinishedText = m_repository.GetArticleText(Name);

            // Создаем оригинальный текст статьи
            OriginalText = new TextInLanguage(workspace.Language.Code, originalFinishedText.text, originalFinishedText.finished);

            CurrentArtisticalTranslation = null;

            // ToDo: Пока так, потом поменять
            // ToDo: понять че там с еденицами перевода - m_translationUnits. Поидее их тоже надо запросить у базы и они актуальны только если оригинальный текст завершен
            m_translationUnits = null;
        }

        public void ChangeArticleText(string newText)
        {
            if (OriginalText.Finished)
                throw new System.Exception("Cannot change article text while being in finished mode");

            OriginalText.Text = newText;

            m_repository.SetArticleText(Name, OriginalText.Text);
        }

        public void ChangeArticleFinishedState(bool finished)
        {
            OriginalText.Finished = finished;

            m_repository.SetArticleFinishedState(Name, OriginalText.Finished);
        }

        // Открывает художественный перевод по заданному языку. Если язык не присутсвует в списке языков для перевода, либо передан null, то 
        // CurrentArtisticalTranslation будет установлен в null, в другом случае в CurrentArtisticalTranslation будет открыт перевод либо пустой вариант перевода (добавление нового перевода)
        public void OpenArtisticalTranslation(string languageCode)
        {
            // Если заданный язык null, либо не является языком для перевода
            if (languageCode == null || m_workspace.TranslationLanguages.First(lang => lang.Code == languageCode) == null)
            {
                CurrentArtisticalTranslation = null;
                return;
            }

            WorkspaceRepository.FinishedText finishedText = m_repository.GetArtisticalTranslaionText(Name, languageCode);

            // В случае, если перевода для этого языка еще нет, считаем как будто он есть, но с пустым текстом
            if (finishedText.text == null)
            {
                finishedText.text = string.Empty;
                finishedText.finished = false;
            }

            // Создаем текущий текст перевода
            CurrentArtisticalTranslation = new TextInLanguage(languageCode, finishedText.text, finishedText.finished);
        }
    }
}
