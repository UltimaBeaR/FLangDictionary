﻿using Mono.Data.Sqlite;
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
            ExecuteSQLQuery(
                $"SELECT {Tables.WorkspaceProperties.value} FROM {Tables.workspaceProperties};",
                (reader) =>
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        res = reader.GetString(0);
                    }
                });

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

            ExecuteSQLQuery(
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
                });

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

        // Добавляет новую пустую статью с заданным именем
        public void AddArticle(string articleName)
        {
            int sequence = 0;
            // Сначала получим количество записей - это нужно чтобы узнать следующий sequence(порядковый номер) для этой статьи
            ExecuteSQLQuery($"SELECT Count(*) FROM {Tables.articles};", (reader) => { reader.Read(); sequence = reader.GetInt32(0) - 1; });

            // Теперь добавим пустую статью с заданным именем и полученным порядковым номером
            ExecuteSQLQuery($"INSERT INTO {Tables.articles} VALUES " +
                $"(" +
                $"NULL, " + //< Id (Автоинкремент)
                $"{SQLStringLiteral(articleName)}, " + // name - Имя статьи
                $"{sequence}, " + //< sequence - порядковый номер
                $"'', " + //< text - текст статьи
                $"{TinyIntAsBool.falseStr}, " + //< finished - флаг завершенности статьи
                $"NULL" + //< audioFileName - имя файла с аудиозаписью
                $");");
        }
    }
}
