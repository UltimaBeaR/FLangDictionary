﻿using Mono.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace FLangDictionary.Data
{
    // Представляет собой репозиторий данных касаемых рабочей области - физически находится в файле рабочей области
    // Рабочая область хранит все данные касаемо переводов, статей и всего прочего (все, кроме глобальных настроек самой программы)
    partial class WorkspaceRepository
    {
        // Создает новый экземпляр репозитория и сохраняет его в виде файла с указанным именем
        public static WorkspaceRepository CreateNew(string fileName, string languageCode)
        {
            if (File.Exists(fileName))
                File.Delete(fileName);

            string directoryName = Path.GetDirectoryName(fileName);

            if (!Directory.Exists(directoryName))
                Directory.CreateDirectory(directoryName);

            SqliteConnection.CreateFile(fileName);

            return new WorkspaceRepository(fileName, true, languageCode);
        }

        // Открывает существующий экземпляр репозитория на основе указанного файла
        public static WorkspaceRepository OpenExisting(string fileName)
        {
            if (!File.Exists(fileName))
                return null;

            return new WorkspaceRepository(fileName);
        }

        // Файл рабочей области (файл базы данных sqlite)
        private string m_fileName;

        private WorkspaceRepository(string fileName, bool createNew = false, string languageCode = null)
        {
            // Код языка должен быть задан, если создаем новый репозиторий
            Debug.Assert(!createNew || languageCode != null);

            m_fileName = fileName;

            if (createNew)
            {
                CreateTables();
                SetInitialData(languageCode);
            }
        }

        // Выполняет запрос, и в случае если это был INSERT, возвратит rowid добавленной записи
        private int ExecuteSQLQuery(string query, out int lastInsertedRowId)
        {
            using (var connection = new SqliteConnection($"Data Source={m_fileName}"))
            {
                connection.Open();

                int rowCount;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    rowCount = command.ExecuteNonQuery();
                }

                // Получаем rowid последней добавленной записи. Важно чтобы он был в пределах того же соединения
                // что и INSERT запрос (который предположительно идет прямо перед этим), иначе всегда возвращается 0
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT last_insert_rowid();";
                    using (var reader = command.ExecuteReader())
                    {
                        Debug.Assert(reader.HasRows);
                        reader.Read();
                        lastInsertedRowId = reader.GetInt32(0);
                    }
                }

                connection.Close();

                return rowCount;
            }
        }

        private int ExecuteSQLQuery(string query)
        {
            using (var connection = new SqliteConnection($"Data Source={m_fileName}"))
            {
                connection.Open();

                int rowCount;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    rowCount = command.ExecuteNonQuery();
                }

                connection.Close();

                return rowCount;
            }
        }

        private delegate void ReaderCallback(SqliteDataReader reader);

        private void ExecuteSQLQuery(string query, ReaderCallback readerCallback)
        {
            using (var connection = new SqliteConnection($"Data Source={m_fileName}"))
            {
                connection.Open();

                SqliteDataReader reader;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    reader = command.ExecuteReader();
                }

                if (readerCallback != null)
                    readerCallback(reader);

                reader.Close();

                connection.Close();
            }
        }

        // По заданному ключу получает значение в виде строки из таблицы-словаря [WorkspaceProperties]
        private string GetWorkspaceProperty(int key)
        {
            string res = null;

            ExecuteSQLQuery
            (
                $"SELECT {Tables.WorkspaceProperties.value} FROM {Tables.workspaceProperties};",
                
                (reader) =>
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        res = reader.GetString(0);
                    }
                }
            );

            return res;
        }

        // Получает код языка этой рабочей области
        public string GetWorkspaceLanguageCode()
        {
            return GetWorkspaceProperty(WorkspacePropertyKeys.languageCode);
        }

        // Получает список имен статей (в порядке следования)
        public string[] GetArticleNames()
        {
            List<string> articleNames = new List<string>();

            ExecuteSQLQuery
            (
                $"SELECT {Tables.Articles.name} FROM {Tables.articles} " +
                $"ORDER BY {Tables.Articles.sequence};",
                
                (reader) =>
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            articleNames.Add(reader.GetString(0));
                        }
                    }
                }
            );

            return articleNames.ToArray();
        }

        // Из заданной строки получает литерал SQL-строки в кавычках. При этом следит за тем, чтобы
        // не было проблем с экранированием
        // Например, если была подана строка abc'de, то выдаст строку 'abc''de'
        private string SQLStringLiteral(string sourceString)
        {
            StringBuilder stringBuilder = new StringBuilder(sourceString);

            // Заменяем ковычки на двойные, чтобы не было выхода за экран
            stringBuilder.Replace("'", "''");

            // Добавляем кавычки с обих сторон
            stringBuilder.Insert(0, "'");
            stringBuilder.Append("'");

            return stringBuilder.ToString();
        }

        private int GetArticlesCount()
        {
            int articlesCount = 0;
            ExecuteSQLQuery($"SELECT Count(*) FROM {Tables.articles};", (reader) => { reader.Read(); articlesCount = reader.GetInt32(0); });
            return articlesCount;
        }

        // Добавляет новую пустую статью с заданным именем
        public void AddArticle(string articleName)
        {
            // Узнаем следующий sequence, зависящий от количества статей, так как добавляем эту статью самой последней по последовательности
            int sequence = GetArticlesCount();

            // Теперь добавим пустую статью с заданным именем и полученным порядковым номером
            ExecuteSQLQuery
            (
                $"INSERT INTO {Tables.articles} VALUES " +
                $"(" +
                $"NULL, " + //< Id (Автоинкремент)
                $"{SQLStringLiteral(articleName)}, " + // name - Имя статьи
                $"{sequence}, " + //< sequence - порядковый номер
                $"'', " + //< text - текст статьи
                $"{TinyIntAsBool.falseStr}, " + //< finished - флаг завершенности статьи
                $"NULL" + //< audioFileName - имя файла с аудиозаписью
                $");"
            );
        }

        // Получает индекс последовательности из заданной статьи
        private int GetArticleSequence(string articleName)
        {
            int sequence = 0;

            // Получаем sequence удаляемой статьи. Все оставшиеся sequence статей выше этой надо будет уменьшить на 1
            ExecuteSQLQuery
            (
                $"SELECT {Tables.Articles.sequence} FROM {Tables.articles} WHERE {Tables.Articles.name} = {SQLStringLiteral(articleName)} LIMIT 1;",
                
                (reader) =>
                {
                    if (!reader.HasRows)
                        throw new System.Exception($"{articleName} is not found in database");

                    reader.Read();

                    sequence = reader.GetInt32(0);
                }
            );

            return sequence;
        }

        public void DeleteArticle(string articleName)
        {
            // Удаляем статью, а так же все с ней связанное

            int sequence = GetArticleSequence(articleName);

            // Удаляем запись со статьей
            ExecuteSQLQuery($"DELETE FROM {Tables.articles} WHERE {Tables.Articles.name} = {SQLStringLiteral(articleName)};");

            // Уменьшаем sequence всех статей, у которых он выше удаляемой на 1, чтобы актуализировать последовательности
            ExecuteSQLQuery($"UPDATE {Tables.articles} SET {Tables.Articles.sequence} = {Tables.Articles.sequence} - 1 WHERE {Tables.Articles.sequence} > {sequence};");

            // ToDo: Со статьей может быть много всего связанно. сейчас реализовано только простое удаление, надо доделать нормальное удаление из всех связанных таблиц
        }

        public void RenameArticle(string articleName, string newArticleName)
        {
            ExecuteSQLQuery($"UPDATE {Tables.articles} SET {Tables.Articles.name} = {SQLStringLiteral(newArticleName)} WHERE {Tables.Articles.name} = {SQLStringLiteral(articleName)};");
        }

        // Меняет порядок статьи относительно других статей (двигает вверх либо вниз по списку статей)
        public void MoveArticle(string articleName, bool up)
        {
            // Для того, чтобы передвинуть статью, нужно поменять ей номер (sequence) в последовательности, при этом статья, которая сейчас
            // Стоит на месте, куда мы хотим переместиться должна переместиться на наше старое место. То есть надо двум статьсям обменяться номерами

            // Получаем текущий номер статьи, которую будем перемещать
            int sequence = GetArticleSequence(articleName);
            // Получаем номер статьи, с которой будем меняться местами
            int sequenceToBeSet = up ? sequence - 1 : sequence + 1;

            // В случае если статье больше некуда двигаться - выходим без изменения номера
            if (sequenceToBeSet < 0 || sequenceToBeSet >= GetArticlesCount())
                return;

            // Меняем 2 статьи местами
            ExecuteSQLQuery($"UPDATE {Tables.articles} SET {Tables.Articles.sequence} = {sequence} WHERE {Tables.Articles.sequence} = {sequenceToBeSet};");
            ExecuteSQLQuery($"UPDATE {Tables.articles} SET {Tables.Articles.sequence} = {sequenceToBeSet} WHERE {Tables.Articles.name} = {SQLStringLiteral(articleName)};");
        }

        // Текст и флаг его завершенности - в таком виде возвращаются данные из БД
        public struct FinishedText
        {
            // Текст
            public string text;
            // Флаг завершенности редактирования текста пользователем
            public bool finished;
        }

        // Возвращает текст и флаг завершенности этого текста для статьи с указанным именем
        public FinishedText GetArticleText(string articleName)
        {
            FinishedText res = new FinishedText();

            ExecuteSQLQuery
            (
                $"SELECT {Tables.Articles.text}, {Tables.Articles.finished} " +
                $"FROM {Tables.articles} " +
                $"WHERE {Tables.Articles.name} = {SQLStringLiteral(articleName)} " +
                $"LIMIT 1;",
                
                (reader) =>
                {
                    if (!reader.HasRows)
                        throw new System.Exception($"{articleName} is not found in database");

                    reader.Read();

                    res.text = reader.GetString(0);
                    res.finished = TinyIntAsBool.BoolFromShort(reader.GetInt16(1));
                }
            );

            return res;
        }

        public bool GetArticleFinishedState(string articleName)
        {
            Debug.Assert(articleName != null);

            bool isArticleFinished = false;

            // Получаем значение finished для статьи
            ExecuteSQLQuery
            (
                $"SELECT {Tables.Articles.finished} " +
                $"FROM {Tables.articles} " +
                $"WHERE {Tables.Articles.name} = {SQLStringLiteral(articleName)} " +
                $"LIMIT 1;",

                (reader) =>
                {
                    if (!reader.HasRows)
                        throw new System.Exception($"{articleName} is not found in database");

                    reader.Read();

                    isArticleFinished = TinyIntAsBool.BoolFromShort(reader.GetInt16(0));
                }
            );

            return isArticleFinished;
        }

        // Устанавливает текст статьи с заданным именем.
        // устанавливать текст можно только в случае если статья finished
        public void SetArticleText(string articleName, string newText)
        {
            Debug.Assert(articleName != null && newText != null);

            bool isArticleFinished = GetArticleFinishedState(articleName);

            if (isArticleFinished)
                throw new System.Exception("Cannot set new text to article while it is in finished mode");
            else
            {
                // Устанавливаем новый текст
                ExecuteSQLQuery($"UPDATE {Tables.articles} SET {Tables.Articles.text} = {SQLStringLiteral(newText)} " +
                    $"WHERE {Tables.Articles.name} = {SQLStringLiteral(articleName)};");
            }
        }

        // Устанавливает состояние завершенности для статьи. Если устанавливаем состояние false, а до этого было true,
        // То также удалятся все связанные с этой статьей сущности, требующие состояния завершенности (привязанные переводы слов и худ. переводы, тайминги аудио-разметки)
        public void SetArticleFinishedState(string articleName, bool finished)
        {
            // Если состояние не меняется - выходим
            if (finished == GetArticleFinishedState(articleName))
                return;

            // Ставим статье finished = true
            ExecuteSQLQuery($"UPDATE {Tables.articles} SET {Tables.Articles.finished} = {TinyIntAsBool.StrFromBool(finished)} " +
                $"WHERE {Tables.Articles.name} = {SQLStringLiteral(articleName)};");

            if (!finished)
            {
                // ToDo: случай, когда завершенное состояние меняем на незавершенного - нужно удалить все связи в бд 
            }
        }

        // Возвращает текст и флаг завершенности этого текста для художественного перевода с указанным языком, который принадлежит статье с указанным именем
        // В случае, если для такого языка нет художественного перевода - в поле text возвращаемой структуры будет null
        public FinishedText GetArtisticalTranslaionText(string articleName, string languageCode)
        {
            FinishedText res = new FinishedText();

            int articleId = -1;

            // запрашиваем articleId по articleName
            ExecuteSQLQuery
            (
                $"SELECT {Tables.Articles.id} " +
                $"FROM {Tables.articles} " +
                $"WHERE {Tables.Articles.name} = {SQLStringLiteral(articleName)} " +
                $"LIMIT 1;",

                (reader) =>
                {
                    if (!reader.HasRows)
                        throw new System.Exception($"{articleName} is not found in database");

                    reader.Read();

                    articleId = reader.GetInt32(0);
                }
            );

            ExecuteSQLQuery
            (
                $"SELECT {Tables.ArtisticalTranslations.text}, {Tables.ArtisticalTranslations.finished} " +
                $"FROM {Tables.artisticalTranslations} " +
                $"WHERE {Tables.ArtisticalTranslations.articleId} = {articleId} and {Tables.ArtisticalTranslations.languageCode} = {SQLStringLiteral(languageCode)} " +
                $"LIMIT 1;",

                (reader) =>
                {
                    // Если для этого языка еще нет перевода, то выходим. В этом случае будет значение по умолчанию (text = null)
                    if (!reader.HasRows)
                        return;

                    reader.Read();

                    res.text = reader.GetString(0);
                    res.finished = TinyIntAsBool.BoolFromShort(reader.GetInt16(1));
                }
            );

            return res;
        }

        // Получает список имен статей (в алфавитном порядке)
        public string[] GetTranslationLanguageCodes()
        {
            List<string> languageCodes = new List<string>();

            ExecuteSQLQuery
            (
                $"SELECT {Tables.TranslationLanguages.languageCode} FROM {Tables.translationLanguages};",
                
                (reader) =>
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            languageCodes.Add(reader.GetString(0));
                        }
                    }
                }
            );

            return languageCodes.ToArray();
        }

        // Добавляет новый язык для перевода
        public void AddTranslationLanguage(string languageCode)
        {
            ExecuteSQLQuery
            (
                $"INSERT INTO {Tables.translationLanguages} VALUES " +
                $"(" +
                $"{SQLStringLiteral(languageCode)}" +
                $");"
            );
        }

        // Удаляет язык для перевода
        public void DeleteTranslationLanguage(string languageCode)
        {
            // Удаляем запись с языком из списка языков для перевода
            ExecuteSQLQuery($"DELETE FROM {Tables.translationLanguages} WHERE {Tables.TranslationLanguages.languageCode} = {SQLStringLiteral(languageCode)};");

            // ToDo: Когда удаляем язык, нужно каскадно удалить все связанные с этим языком переводы - то есть статьи художественного перевода и переводы слов/фраз на этом языке
        }

        public struct RawTranslationUnit
        {
            public string originalPhraseIndexes;
            public string translatedPhrase;
            public string infinitiveOriginalPhrase;
            public string infinitiveTranslatedPhrase;
        }

        // Подзапрос для получения id статьи из ее имени
        // Если задаются insertionValuesBefore или insertionValuesAfter (даже если задан пробел), то подзапрос генерируется для 2ой части INSERT запроса
        private string ArticleIdByNameSubquery(string articleName, string insertionValuesBefore = null, string insertionValuesAfter = null)
        {
            bool insertionSyntax = false;

            if (insertionValuesBefore == null)
                insertionValuesBefore = string.Empty;
            else
            {
                insertionSyntax = true;
                insertionValuesBefore = insertionValuesBefore + ", ";
            }

            if (insertionValuesAfter == null)
                insertionValuesAfter = string.Empty;
            else
            {
                insertionSyntax = true;
                insertionValuesAfter = ", " + insertionValuesAfter;
            }

            string query = $"SELECT {insertionValuesBefore}{Tables.Articles.id}{insertionValuesAfter} FROM {Tables.articles} WHERE {Tables.Articles.name} = {SQLStringLiteral(articleName)}";

            return insertionSyntax ? query : $"({query})";
        }

        // Записывает перевод фразы в инфинитиве и возвращает его id, либо, в случае если точно такой же перевод уже есть, просто вернет его id
        private int GetOrAddTranslationInInfinitive(string originalPhrase, string translationLanguageCode, string translatedPhrase)
        {
            Debug.Assert(originalPhrase != null && translationLanguageCode != null && translatedPhrase != null);

            int foundId = -1;

            ExecuteSQLQuery
            (
                $"SELECT {Tables.TranslationsInInfinitive.id} FROM {Tables.translationsInInfinitive} WHERE " +
                $"{Tables.TranslationsInInfinitive.originalPhrase} = {SQLStringLiteral(originalPhrase)} and " +
                $"{Tables.TranslationsInInfinitive.translationLanguageCode} = {SQLStringLiteral(translationLanguageCode)} and " +
                $"{Tables.TranslationsInInfinitive.translatedPhrase} = {SQLStringLiteral(translatedPhrase)} LIMIT 1;",

                (reader) =>
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        foundId = reader.GetInt32(0);
                    }
                }
            );

            // Если нашли Id, значит такая запись уже есть - возвращаем Id
            if (foundId != -1)
                return foundId;

            // Если же нет такого Id, то добавляем новую запись и возвращаем полученный Id

            int lastRowId;
            ExecuteSQLQuery($"INSERT INTO {Tables.translationsInInfinitive} VALUES " +
                $"(NULL, {SQLStringLiteral(originalPhrase)}, {SQLStringLiteral(translationLanguageCode)}, {SQLStringLiteral(translatedPhrase)});", out lastRowId);

            ExecuteSQLQuery
            (
                $"SELECT {Tables.TranslationsInInfinitive.id} FROM {Tables.translationsInInfinitive} WHERE " +
                $"[rowid] = {lastRowId} LIMIT 1;",

                (reader) =>
                {
                    Debug.Assert(reader.HasRows);

                    reader.Read();
                    foundId = reader.GetInt32(0);
                }
            );

            return foundId;
        }

        // Добавляет еденицу перевода. Если слово в инфинитиве не существует в словаре, то оно также добавляется в этот словарь
        public void AddOrChangeTranslationUnit(string articleName, string translationLanguageCode, RawTranslationUnit translationUnit)
        {
            Debug.Assert(articleName != null && translationLanguageCode != null && translationUnit.originalPhraseIndexes != null && translationUnit.translatedPhrase != null);

            // Если задан перевод в инфинитиве, получаем его Id (при этом либо добавится новый перевод либо возьмется уже существующий)
            int translationInInfinitiveId = -1;
            if (translationUnit.infinitiveOriginalPhrase != null && translationUnit.infinitiveTranslatedPhrase != null)
                translationInInfinitiveId = GetOrAddTranslationInInfinitive(translationUnit.infinitiveOriginalPhrase, translationLanguageCode, translationUnit.infinitiveTranslatedPhrase);

            // Запрашиваем Id оригинальной фразы (ее индексы слов), дабы узнать есть ли уже такая еденица перевода в БД

            int translationUnitId = -1;
            ExecuteSQLQuery
            (
                $"SELECT {Tables.TranslationUnits.id} FROM {Tables.translationUnits} " +
                $"WHERE {Tables.TranslationUnits.articleId} = {ArticleIdByNameSubquery(articleName)} and " +
                $"{Tables.TranslationUnits.translationLanguageCode} = {SQLStringLiteral(translationLanguageCode)} and " +
                $"{Tables.TranslationUnits.originalPhraseIndexes} = {SQLStringLiteral(translationUnit.originalPhraseIndexes)} LIMIT 1;",

                (reader) =>
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        translationUnitId = reader.GetInt32(0);
                    }
                }
            );

            // Если такая еденица перевода уже есть - модифицируем ее, иначе добавляем новую

            string translationInInfinitiveIdAsString = translationInInfinitiveId == -1 ? "NULL" : translationInInfinitiveId.ToString();

            if (translationUnitId == -1)
            {
                // Если нужно добавлять новую еденицу перевода

                // Формируем строку со значениями
                string insertionData =
                    SQLStringLiteral(translationUnit.originalPhraseIndexes) + ", " +
                    SQLStringLiteral(translationLanguageCode) + ", " +
                    SQLStringLiteral(translationUnit.translatedPhrase) + ", " +
                    translationInInfinitiveIdAsString;

                // Добавляем еденицу перевода
                ExecuteSQLQuery($"INSERT INTO {Tables.translationUnits} {ArticleIdByNameSubquery(articleName, "NULL", insertionData)};");
            }
            else
            {
                // Если нужно изменить уже существующую еденицу перевода

                ExecuteSQLQuery
                (
                    $"UPDATE {Tables.translationUnits} SET " +
                    $"{Tables.TranslationUnits.translatedPhrase} = {SQLStringLiteral(translationUnit.translatedPhrase)}, " +
                    $"{Tables.TranslationUnits.infinitiveTranslationId} = {translationInInfinitiveIdAsString} " +
                    $"WHERE {Tables.TranslationUnits.articleId} = {ArticleIdByNameSubquery(articleName)} and " +
                    $"{Tables.TranslationUnits.translationLanguageCode} = {SQLStringLiteral(translationLanguageCode)} and " +
                    $"{Tables.TranslationUnits.originalPhraseIndexes} = {SQLStringLiteral(translationUnit.originalPhraseIndexes)};"
                );
            }
        }

        // Удаляет заданную еденицу перевода, в случае, если она существует
        public void RemoveTranslationUnitIfExists(string articleName, string translationLanguageCode, string originalPhraseIndexes)
        {
            ExecuteSQLQuery
            (
                $"DELETE FROM {Tables.translationUnits} WHERE " +
                $"{Tables.TranslationUnits.articleId} = {ArticleIdByNameSubquery(articleName)} and " +
                $"{Tables.TranslationUnits.translationLanguageCode} = {SQLStringLiteral(translationLanguageCode)} and " +
                $"{Tables.TranslationUnits.originalPhraseIndexes} = {SQLStringLiteral(originalPhraseIndexes)};"
            );
        }

        // Получает набор едениц перевода для заданного языка в заданной статье
        public RawTranslationUnit[] GetTranslationUnits(string articleName, string translationLanguageCode)
        {
            List<RawTranslationUnit> units = new List<RawTranslationUnit>();

            ExecuteSQLQuery
            (
                $"SELECT t1.{Tables.TranslationUnits.originalPhraseIndexes}, t1.{Tables.TranslationUnits.translatedPhrase}, " +
                $"t2.{Tables.TranslationsInInfinitive.originalPhrase}, t2.{Tables.TranslationsInInfinitive.translatedPhrase} " +
                $"FROM {Tables.translationUnits} t1 " +
                $"LEFT JOIN {Tables.translationsInInfinitive} t2 " +
                $"ON t1.{Tables.TranslationUnits.infinitiveTranslationId} = t2.{Tables.TranslationsInInfinitive.id} " +
                $"WHERE t1.{Tables.TranslationUnits.articleId} = {ArticleIdByNameSubquery(articleName)} and " +
                $"t1.{Tables.TranslationUnits.translationLanguageCode} = {SQLStringLiteral(translationLanguageCode)};",

                (reader) =>
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            RawTranslationUnit unit = new RawTranslationUnit();

                            unit.originalPhraseIndexes = reader.GetString(0);
                            unit.translatedPhrase = reader.GetString(1);

                            // Проверяем значения в инфинитиве на null. Потому как они могут быть не заданы и в этом случае будут null.
                            // Если они null, то и в выходных данных оставляем дефолтный null
                            if (!reader.IsDBNull(2))
                            {
                                unit.infinitiveOriginalPhrase = reader.GetString(2);
                                unit.infinitiveTranslatedPhrase = reader.GetString(3);
                            }

                            units.Add(unit);
                        }
                    }
                }
            );

            return units.ToArray();
        }
    }
}
