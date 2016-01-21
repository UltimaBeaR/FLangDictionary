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

        // Текущий (открытый) художественный перевод
        // Может быть null. Но если идет попытка открыть текст перевода на существующий язык для перевода то тут никогда не будет null
        public TextInLanguage CurrentArtisticalTranslation { get; private set; }

        // Еденицы перевода. Ключ содержит код языка, для которого идет перевод.
        // Значение содержит список едениц перевода. Порядок в этом списке не имеет значения.
        Dictionary<string, List<TranslationUnit>> m_translationUnits;

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

            UpdateTranslationUnits();
        }

        // Обновление текущих едениц перевода
        private void UpdateTranslationUnits()
        {
            // Заполняем текущие еденицы перевода - проходим по языкам, которые заданы для перевода и для каждого запрашиваем у базы
            // список едениц перевода

            m_translationUnits = new Dictionary<string, List<TranslationUnit>>();
            foreach (var translationLanguage in m_workspace.TranslationLanguages)
            {
                List<TranslationUnit> translationLanguageUnits = new List<TranslationUnit>();
                m_translationUnits.Add(translationLanguage.Code, translationLanguageUnits);

                // Запрашиваем список едениц перевода у БД и засовываем его в подготовленный для этого список
                var rawTranslationLanguageUnits = m_repository.GetTranslationUnits(Name, translationLanguage.Code);
                foreach (var rawTranslationLanguageUnit in rawTranslationLanguageUnits)
                {
                    // Перерабатываем сырую еденицу перевода (данные из БД) в нормальную и засовываем ее в список

                    TranslationUnit translationUnit = new TranslationUnit(OriginalText.GetPhraseWords(rawTranslationLanguageUnit.originalPhraseIndexes));
                    translationUnit.translatedPhrase = rawTranslationLanguageUnit.translatedPhrase;
                    translationUnit.infinitiveTranslation.originalPhrase = rawTranslationLanguageUnit.infinitiveOriginalPhrase;
                    translationUnit.infinitiveTranslation.translatedPhrase = rawTranslationLanguageUnit.infinitiveTranslatedPhrase;

                    translationLanguageUnits.Add(translationUnit);
                }
            }
        }

        public void BeforeTranslationLanguagesChanged()
        {
            // ToDo: надо сохранить все текущие изменения в еденицах перевода
        }

        public void AfterTranslationLanguagesChanged()
        {
            UpdateTranslationUnits();
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
