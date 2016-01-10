using Mono.Data.Sqlite;
using System;
using System.Windows;

// ToDo: первое и самое простое - сделать класс-форматер для описания-перевода. Он должен формировать FlowLayout (с одним или несколькими параграфами)
// и должен уметь переходить по ссылкам. То есть например ему при построении передается делегат на метод перехода по ссылке, а этот делегат будет в свою очередь
// тоже строить FlowLayout через этот же форматер и засовывать его в поле просмотра.
//----
// Далее нужно сделать инфраструктуру по переводу слов. Все переведенные слова вместе с самими текстами статей и худ. переводов к ним должны лежать в фаликах, которые можно
// взаимозаменять. То есть все данные хранятся в одной бд и связаны со всеми другими данными. То есть глобальный список слов идет для всех статей которые указаны для этой базы
// Каждый перевод слова - это скорее всего просто пара слов - слово на оригинале = слово в переводе (один смысл). То есть может быть много элементов с одинаковыми оригинальными словами
// но разными смыслами-переводами. Далее нужен элемент, который соединит разметку в статье и переведенное слово, а также даст опциональную ссылку на соответсвующее слово
// в художественном переводе (возможно для хранения этого лучше подойдет группа слов). Надо незабывать что одно слово может означать несколько слов в переводе и наоборот.
// Далее нужна группа слов - то есть перевод для целой группы слов. Это должно быть параллельно со словом-переводом. То есть если есть группа слов, то параллельно переводится
// и отдельно каждое слово из этой группы. Эти элементы не пересекаются внутренне, но при показе слова(при наведении мышкой) - может показаться вся соответсвующая слову группа,
// с переводом именно группы, далее может показаться именно это выделенное слово, и подсвечиваются границы предложения. Соответсвенно в разметке надо указывать ссылки
// как на отдельное слово, так и на группу слов, если это слово находится в группе. Или еще лучше в разметке сидит элемент переходной, который соединяет
// слово, группу слов, и ссылки на элементы разметки в художественном переводе(переводах, если языков несколько). Тут скорее всего так - если есть художественный
// перевод группы слов - берем его, если нет, берем ссылку на одно слово. Также надо понимать что ссылка на худ. перевод может идти не на одно слово а на список слов.
// Например если в инглише написано "Good luck", и это является группой, то художественный перевод для этого будет тоже пара слов - "Желаю удачи"

namespace FLangDictionary
{
    class TempTests
    {
        private TempTests m_instance;
        public TempTests Instance { get { if (m_instance == null) m_instance = new TempTests(); return m_instance; } }

        private TempTests()
        {
        }

        private void SQLTest()
        {
            var dataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "FLangDictionary");

            var dbName = "testDB.db";
            var dbFilePath = System.IO.Path.Combine(dataFolder, dbName);

            if (System.IO.File.Exists(dbFilePath))
                System.IO.File.Delete(dbFilePath);

            if (!System.IO.Directory.Exists(dataFolder))
                System.IO.Directory.CreateDirectory(dataFolder);

            SqliteConnection.CreateFile(dbFilePath);

            string output = "";

            using (var connection = new SqliteConnection("Data Source=" + dbFilePath))
            {
                connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "CREATE TABLE [Items] ([_id] int, [Symbol] ntext, [Name] ntext);";
                    var rowcount = command.ExecuteNonQuery();
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "INSERT INTO [Items] ([_id], [Symbol]) VALUES ('1', 'APPL')";
                    var rowcount = command.ExecuteNonQuery(); // rowcount will be 1
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT * FROM [Items]";
                    SqliteDataReader reader = command.ExecuteReader();
                    output += "\nReading data";
                    while (reader.Read())
                        output += $"\n\tKey={reader["_id"]}; Value={reader["Symbol"]}";
                }

                connection.Close();
            }

            MessageBox.Show(output);
        }
    }
}
