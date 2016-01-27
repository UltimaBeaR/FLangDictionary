using System;
using FLangDictionary.Logic;
using System.Collections.Generic;
using System.Diagnostics;
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

        // Добавляет/Меняет/Удаляет еденицу перевода. Удаление идет в случае если поле перевода = null, а также поля оригинала и перевода в инфинитиве.
        // Добавление идет, если такой еденицы еще нет, в другом случае - изменение.
        public void ModifyTranslationUnit(TranslationUnit translationUnit, string languageCode)
        {
            Debug.Assert(translationUnit != null && translationUnit.OriginalPhrase != null && m_translationUnits.ContainsKey(languageCode));

            // Перевод в инфинитиве должен быть либо не задан полностью либо полностью задан
            Debug.Assert(
                (translationUnit.infinitiveTranslation.originalPhrase == null && translationUnit.infinitiveTranslation.translatedPhrase == null) ||
                (translationUnit.infinitiveTranslation.originalPhrase != null && translationUnit.infinitiveTranslation.translatedPhrase != null));

            // Получаем список едениц перевода для данного языка (который также будет изменен, чтобы не лезть лишний раз в базу и не синхронизировать)
            List<TranslationUnit> translationLanguageUnits = m_translationUnits[languageCode];
            // Находим заданный элемент в текущем списке едениц перевода, если он там есть
            int foundIndex = translationLanguageUnits.FindIndex(trUnit => trUnit.OriginalPhrase.SequenceEqual(translationUnit.OriginalPhrase));

            // Получаем оригинальную фразу в виде строки с индексами (Это данные, которые будет сохранять БД)
            string phraseIndexes = TextInLanguage.GetPhraseIndexes(translationUnit.OriginalPhrase);

            if (translationUnit.translatedPhrase != null || translationUnit.infinitiveTranslation.originalPhrase != null)
            {
                // Какие-то поля заданы - значит добавляем либо модифицируем эту еденицу перевода

                WorkspaceRepository.RawTranslationUnit rawTranslationUnit = new WorkspaceRepository.RawTranslationUnit();
                rawTranslationUnit.originalPhraseIndexes = phraseIndexes;
                rawTranslationUnit.translatedPhrase = translationUnit.translatedPhrase == null ? string.Empty : translationUnit.translatedPhrase;
                rawTranslationUnit.infinitiveOriginalPhrase = translationUnit.infinitiveTranslation.originalPhrase;
                rawTranslationUnit.infinitiveTranslatedPhrase = translationUnit.infinitiveTranslation.translatedPhrase;

                m_repository.AddOrChangeTranslationUnit(Name, languageCode, rawTranslationUnit);

                // Обновляем локальный список
                if (foundIndex == -1)
                    translationLanguageUnits.Add(translationUnit);
                else
                    translationLanguageUnits[foundIndex] = translationUnit;
            }
            else
            {
                // Ни одно поле не задано - значит удаляем эту еденицу перевода (в случае, если такой еденицы перевода нет - то просто ничего не произойдет)

                m_repository.RemoveTranslationUnitIfExists(Name, languageCode, phraseIndexes);

                // Обновляем локальный список
                if (foundIndex != -1)
                    translationLanguageUnits.RemoveAt(foundIndex);
            }
        }

        // По заданному языку перевода и выбранному списку слов получает translation unit, соответсвующий этому выбранному списку и фразу, в состав которой входит этот список слов
        // selection и phrase могут быть = null, в случае если передается вся фраза, то фраза будет = null
        public void GetTranslationUnits(string languageCode, TextInLanguage.SyntaxLayout.Word[] selectedWords, out TranslationUnit selection, out TranslationUnit phrase)
        {
            Debug.Assert(selectedWords != null && selectedWords.Length > 0);

            // Получаем для заданного языка все еденицы перевода
            List<TranslationUnit> translationLanguageUnits;
            bool languageExists = m_translationUnits.TryGetValue(languageCode, out translationLanguageUnits);
            Debug.Assert(languageExists);

            selection = null;
            phrase = null;

            // Пробегаем все еденицы перевода для заданного языка
            foreach (TranslationUnit translationUnit in translationLanguageUnits)
            {
                if (selectedWords[0].FirstIndex >= translationUnit.OriginalPhrase[0].FirstIndex &&
                    selectedWords[selectedWords.Length - 1].LastIndex <= translationUnit.OriginalPhrase[translationUnit.OriginalPhrase.Length - 1].LastIndex)
                {
                    // Если translationUnit содержит выбранный список слов

                    int selectedWordsIdx = 0;
                    for (int translationUnitWordIdx = 0; translationUnitWordIdx < translationUnit.OriginalPhrase.Length; translationUnitWordIdx++)
                    {
                        if (selectedWords[selectedWordsIdx] == translationUnit.OriginalPhrase[translationUnitWordIdx])
                            selectedWordsIdx++;

                        if (selectedWordsIdx == selectedWords.Length)
                            break;
                    }

                    if (selectedWordsIdx == selectedWords.Length)
                    {
                        if (selectedWords.Length == translationUnit.OriginalPhrase.Length)
                            selection = translationUnit;
                        else
                            phrase = translationUnit;

                        if (selection != null && phrase != null)
                            return;
                    }
                }
            }            
        }
    }
}