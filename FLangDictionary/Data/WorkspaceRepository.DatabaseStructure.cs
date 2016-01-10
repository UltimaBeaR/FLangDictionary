namespace FLangDictionary.Data
{
    partial class WorkspaceRepository
    {
        // Этот класс нужен только для группирования констант, нужных для использования в SQL-запросах
        // Тут описывается внутренняя структура БД и метод создания таблиц для новой БД на основе этой структуры
        // Внутри идет список таблиц и в одноименных подклассах - список полей в этих таблицах
        private static class Tables
        {
            // Таблица, хранящая свойства всей рабочей области. То есть некие глобальные переменные
            // По сути это словарь ключ-значение, где ключ - идентификатор, целое число, а значение - строка текста
            public const string workspaceProperties = "[WorkspaceProperties]";
            public static class WorkspaceProperties
            {
                public const string key = "[key]";
                public const string value = "[value]";
            }

            // Таблица переводов слов/фраз в инфинитие.
            // На каждое слово может быть несколько записей с разным переводом (для разных смыслов слова/фразы)
            // Этот словарь является глобальным, то есть не привязанный к конкретной статье.
            // Так как слова находятся в инфинитиве и не связаны со статьей, по этому словарю можно делать всяческие статистические выборки,
            // например можно узнать частоту употребления того или иного слова
            public const string translationsInInfinitive = "[TranslationsInInfinitive]";
            public static class TranslationsInInfinitive
            {
                // Текст оригинальной фразы в инфинитиве
                public const string originalPhrase = "[originalPhrase]";
                // Язык перевода
                public const string translationLanguageCode = "[translationLanguageCode]";
                // Переведенный текст фразы в инфинитиве
                public const string translatedPhrase = "[translatedPhrase]";
            }

            // Таблица со статьями
            // Описывает разные статьи на разных языках. По сути это основной элемент в этой программе.
            // От статьи уже идет ссылки на еденицы перевода, художественные переводы и т.д.
            public const string articles = "[Articles]";
            public static class Articles
            {
                // Имя статьи
                public const string name = "[name]";
                // Индекс последовательности статьи. Это нужно для того, чтобы была возможность поменять порядок следования уже существующих статей
                public const string sequence = "[sequence]";
                // Текст статьи
                public const string text = "[text]";
                // Признак законченности статьи (в режиме когда статья закончена, ее текст больше нельзя менять)
                public const string finished = "[finished]";
                // Путь (относительно каталога с юзерскими данными) к файлу с аудио
                public const string audioFileName = "[audioFileName]";
            }

            // Таблица с художественными переводами статей.
            // Тут хранятся вручную сделанные переводы для конкретной статьи.
            // Слова и фразы в оригинальной статье и статье-переводе могут быть связаны.
            // Эти связи хранятся в таблице [ArtisticalTranslationUnits]
            public const string artisticalTranslations = "[ArtisticalTranslations]";
            public static class ArtisticalTranslations
            {
                // Ссылка на [Id] статьи, к которой идет этот перевод
                public const string articleId = "[articleId]";
                // Язык перевода
                public const string languageCode = "[languageCode]";
                // Текст перевода
                public const string text = "[text]";
                // Признак законченности перевода (в режиме когда перевод закончен, его текст больше нельзя менять)
                public const string finished = "[finished]";
            }

            // Таблица с еденицами перевода - то есть конкретные слова или фразы в конкретной статье
            // Каждая еденица перевода это перевод фразы в инфинитиве плюс перевод фразы в том виде в котором она была в тексте
            // И тот и другой перевод идет в контексте того места где эта фраза употребляется (так как она принадлежит определенной статье)
            // По сути ключ этой таблицы это articleId + originalPhraseIndexes + translationLanguageCode
            public const string translationUnits = "[TranslationUnits]";
            public static class TranslationUnits
            {
                // Ссылка на [Id] статьи
                public const string articleId = "[articleId]";
                // Индексы слов, из которых состоит фраза. Это индексы начал слов в строке текста статьи.
                // Задаются в виде строки через пробел. Например так "0 15 23 40"
                public const string originalPhraseIndexes = "[originalPhraseIndexes]";
                // Язык перевода
                public const string translationLanguageCode = "[translationLanguageCode]";
                // Переведенная фраза (не инфинитив, то есть так как она идет в тексте)
                public const string translatedPhrase = "[translatedPhrase]";
                // Ссылка на [Id] в таблице переводов в инфинитиве
                public const string infinitiveTranslationId = "[infinitiveTranslationId]";
            }

            // Таблица со связями между статьей и художественными переводами этой статьи
            // В связях указывается фраза из оригиналььной статьи и фраза из статьи-перевода
            // Это нужно, чтобы, например при наведении на слово в оригинальной статье автоматически подсвечивалось
            // слово в статье-переводе, которое соответсвует оригинальному слову
            public const string artisticalTranslationUnits = "[ArtisticalTranslationUnits]";
            public static class ArtisticalTranslationUnits
            {
                // Ссылка на [Id] статьи - художественного перевода. Ссылка на оригинал статьи находится в записи художественного перевода
                public const string artisticalTranslationId = "[artisticalTranslationId]";
                // Индексы слов фразы из оригинальной статьи
                public const string originalPhraseIndexes = "[originalPhraseIndexes]";
                // Индексы слов этой же фразы, но уже из художественно переведенной статьи
                public const string translatedPhraseIndexes = "[translatedPhraseIndexes]";
            }

            // Таблица с разметками границ слов оригинальной статьи внутри аудио-файла этой статьи.
            // То есть тут определяются временные метки внутри аудио-файла для каждого отдельного слова из оригинальной статьи
            // Это нужно для того, чтобы можно было проиграть отдельное случайно выбранное слово, зациклить его и т.д.
            // Также это используется для подсветки текущего звучащего слова по мере проигрывания аудио-записи
            public const string audioWordsLayout = "[AudioWordsLayout]";
            public static class AudioWordsLayout
            {
                // Ссылка на [Id] статьи
                public const string articleId = "[articleId]";
                // Индекс слова (индекс первой буквы слова в тексте статьи)
                public const string wordIndex = "[wordIndex]";
                // Время начала этого слова в аудио-файле
                public const string audioTimeStart = "[audioTimeStart]";
                // Время завершения этого слова в аудио-файле
                public const string audioTimeEnd = "[audioTimeEnd]";
            }
        }

        // Помощник для работы с типом bool внутри БД, который там представляется как tinyint
        private static class TinyIntAsBool
        {
            // Строковые значения, для использования в запросах
            public const string trueStr = "1";
            public const string falseStr = "0";

            // Преобразование из bool в str, для использования в теле запроса
            public static string StrFromBool(bool val) =>
                val ? trueStr : falseStr;

            // Преобразование из short в bool, используется при чтении результата запроса как short
            public static bool BoolFromShort(short val) =>
                val != 0;
        }

        // Содержим значения ключей для таблицы WorkspaceProperties
        private static class WorkspacePropertyKeys
        {
            // Язык оригинального языка статей в этой рабочей области
            public const int languageCode = 0;
        }

        // Создает все необходимые таблицы в базе данных
        private void CreateTables()
        {
            // Создаем всю внутреннюю инфраструктуру в БД

            ExecuteSQLQuery(
                $"CREATE TABLE {Tables.workspaceProperties} " +
                $"(" +
                $"{Tables.WorkspaceProperties.key} integer PRIMARY KEY, " +
                $"{Tables.WorkspaceProperties.value} nvarchar" +
                $");");

            ExecuteSQLQuery(
                $"CREATE TABLE {Tables.translationsInInfinitive} " +
                $"(" +
                $"[Id] integer PRIMARY KEY AUTOINCREMENT, " +
                $"{Tables.TranslationsInInfinitive.originalPhrase} nchar(256) NOT NULL, " + 
                $"{Tables.TranslationsInInfinitive.translationLanguageCode} nchar(20) NOT NULL, " + 
                $"{Tables.TranslationsInInfinitive.translatedPhrase} nchar(256) NOT NULL" +
                $");");

            ExecuteSQLQuery(
                $"CREATE TABLE {Tables.articles} " +
                $"(" +
                $"[Id] integer PRIMARY KEY AUTOINCREMENT, " +
                $"{Tables.Articles.name} nchar(60) NOT NULL, " +
                $"{Tables.Articles.sequence} integer NOT NULL, " +
                $"{Tables.Articles.text} nvarchar NOT NULL, " +
                $"{Tables.Articles.finished} tinyint NOT NULL, " +
                $"{Tables.Articles.audioFileName} nchar(256)" +
                $");");

            ExecuteSQLQuery(
                $"CREATE TABLE {Tables.artisticalTranslations} " +
                $"(" +
                $"[Id] integer PRIMARY KEY AUTOINCREMENT, " +
                $"{Tables.ArtisticalTranslations.articleId} integer NOT NULL, " +
                $"{Tables.ArtisticalTranslations.languageCode} nchar(20) NOT NULL, " +
                $"{Tables.ArtisticalTranslations.text} nvarchar NOT NULL, " +
                $"{Tables.ArtisticalTranslations.finished} tinyint NOT NULL" +
                $");");

            ExecuteSQLQuery(
                $"CREATE TABLE {Tables.translationUnits} " +
                $"(" +
                $"[Id] integer PRIMARY KEY AUTOINCREMENT, " +
                $"{Tables.TranslationUnits.articleId} integer NOT NULL, " +
                $"{Tables.TranslationUnits.originalPhraseIndexes} nvarchar NOT NULL, " +
                $"{Tables.TranslationUnits.translationLanguageCode} nchar(20) NOT NULL, " +
                $"{Tables.TranslationUnits.translatedPhrase} nvarchar NOT NULL, " +
                $"{Tables.TranslationUnits.infinitiveTranslationId} integer" +
                $");");

            ExecuteSQLQuery(
                $"CREATE TABLE {Tables.artisticalTranslationUnits} " +
                $"(" +
                $"[Id] integer PRIMARY KEY AUTOINCREMENT, " +
                $"{Tables.ArtisticalTranslationUnits.artisticalTranslationId} integer NOT NULL, " +
                $"{Tables.ArtisticalTranslationUnits.originalPhraseIndexes} nvarchar NOT NULL, " +
                $"{Tables.ArtisticalTranslationUnits.translatedPhraseIndexes} nvarchar NOT NULL" +
                $");");

            ExecuteSQLQuery(
                $"CREATE TABLE {Tables.audioWordsLayout} " +
                $"(" +
                $"[Id] integer PRIMARY KEY AUTOINCREMENT, " +
                $"{Tables.AudioWordsLayout.articleId} integer NOT NULL, " +
                $"{Tables.AudioWordsLayout.wordIndex} integer NOT NULL, " +
                $"{Tables.AudioWordsLayout.audioTimeStart} int8 NOT NULL, " +
                $"{Tables.AudioWordsLayout.audioTimeEnd} int8 NOT NULL" +
                $");");
        }

        private void SetInitialData(string languageCode)
        {
            // Записываем язык рабочей области
            ExecuteSQLQuery($"INSERT INTO {Tables.workspaceProperties} VALUES ({WorkspacePropertyKeys.languageCode}, '{languageCode}')");

            // ТЕСТЫ

            /*ExecuteSQLQuery($"INSERT INTO {articles} VALUES (NULL, 'art1', 2, 'russian', '', NULL)");
            ExecuteSQLQuery($"INSERT INTO {articles} VALUES (NULL, 'art2', 0, 'russian', '', NULL)");
            ExecuteSQLQuery($"INSERT INTO {articles} VALUES (NULL, 'art3', 1, 'russian', '', NULL)");*/
        }
    }
}
