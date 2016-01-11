using FLangDictionary.Logic;
using System.Collections.Generic;
using System.Diagnostics;

namespace FLangDictionary.Data
{
    // Статья на иностранном языке. Может иметь один или несколько художественных переводов
    class Article
    {
        // Оригинальный текст статьи (на оригинальном иностранном языке)
        public TextInLanguage OriginalText { get; private set; }

        public Article(Workspace workspace, WorkspaceRepository repository, string articleName)
        {
            // Запрашиваем текст статьи у БД
            WorkspaceRepository.FinishedText originalFinishedText = repository.GetArticleText(articleName);

            // Создаем оригинальный текст статьи
            OriginalText = new TextInLanguage(workspace.Language.Code, originalFinishedText.text, originalFinishedText.finished);

            m_artisticalTranslations = new Dictionary<string, TextInLanguage>();

            // ToDo: также запросить тексты переводов и заполнить список переводов.
            // Например FinishedText[] translationFinishedTexts = repository.GetArtisticalTranslationTexts(articleName);

            // ToDo: понять че там с еденицами перевода - m_translationUnits. Поидее их тоже надо запросить у базы и они актуальны только если оригинальный текст завершен
        }

        // Добавить художественный перевод
        public void AddArtisticalTranslation(TextInLanguage artisticalTranslation)
        {
            Debug.Assert(artisticalTranslation != null);
            m_artisticalTranslations.Add(artisticalTranslation.LanguageCode, artisticalTranslation);
        }

        // Словарь художественных переводов этой статьи. Ключ - язык текста-перевода
        Dictionary<string, TextInLanguage> m_artisticalTranslations;

        // Список едениц перевода для этой статьи
        List<TranslationUnit> m_translationUnits;
    }
}
