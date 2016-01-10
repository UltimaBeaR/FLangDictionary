using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FLangDictionary.Data
{
    // Хранит глобальные настройки программы, которые хранятся в xml файле в папке пользователя
    public class Preferences
    {
        [Serializable]
        // Класс объекта, который сохраняется в файлик .xml
        public class SerializableData : ICloneable
        {
            // Язык пользовательского интерфейса
            public string uiLanguage;

            // Создает экземпляр с значениями по умолчанию. Такие значения будут при первом запуске приложения
            public static SerializableData CreateDefault()
            {
                SerializableData data = new SerializableData();

                // Ставим в null, для того, чтобы при валидации произошло авто-определение на основе текущей культуры
                data.uiLanguage = null;

                data.Validate();

                return data;
            }

            // Производит проверку на наличие криво заданных пользователем вручную значений и исправляет их, если
            // такие значения могут привести к сбоям. Этот метод вызывается после загрузки из .xml файла
            public bool Validate()
            {
                bool changed = false;

                if (uiLanguage != null && uiLanguage != Global.defaultCultureName)
                {
                    // В случае если язык какой-то кривой записан, либо он не поддерживается в UI, 
                    // обнулим его, тем самым дальше произойдет авто-определение языка

                    if (!Global.DoesUISupportLanguage(uiLanguage))
                        uiLanguage = null;
                }

                // Если язык UI не задан - определяем его автоматически на основании текущей культуры
                if (uiLanguage == null)
                {
                    if (Global.DoesUISupportLanguage(Thread.CurrentThread.CurrentUICulture.Name))
                        uiLanguage = Thread.CurrentThread.CurrentUICulture.Name;
                    else if (Global.DoesUISupportLanguage(Thread.CurrentThread.CurrentUICulture.Parent.Name))
                        uiLanguage = Thread.CurrentThread.CurrentUICulture.Parent.Name;
                    else
                        uiLanguage = Global.defaultCultureName;

                    changed = true;
                }

                return changed;
            }

            // Клонирование этого объекта. Нужно для создания бэкапа. По мере изменения внутренностей этого класса нужно менять и внутренности этого метода
            public object Clone()
            {
                SerializableData clone = MemberwiseClone() as SerializableData;
                return clone;
            }
        }

        // Данные, которые можно сохранить в файлик с настройками
        private SerializableData m_data;
        // Имя файла настроек
        private string m_fileName;
        // Бэкап данных, если нужно восстановление
        private SerializableData m_dataBackup;

        public string XmlFileName { get { return m_fileName; } }

        public string UILanguage { get { return m_data.uiLanguage; } set { m_data.uiLanguage = value; } }

        public Preferences(string fileName)
        {
            m_fileName = fileName;
            
            if (File.Exists(m_fileName))
            {
                // Если файл с настройками существует - прочитаем его в m_data

                XmlSerializer serializer = new XmlSerializer(typeof(SerializableData));

                using (Stream fileStream = new FileStream(m_fileName, FileMode.Open))
                {
                    m_data = serializer.Deserialize(fileStream) as SerializableData;
                }

                // Убираем с прочитанных данных косяки, которые мог внести туда пользователь вручную
                // В случае если были косяки, пересохраняем исправленные данные обратно в файл
                if (m_data.Validate())
                    Save(false);
            }
            else
            {
                // Если файла с настройками не существует, создадим его и запишем в него найстройки по умолчанию (которые задаются в конструкторе SerializableData)
                m_data = SerializableData.CreateDefault();
                Save(false);
            }
        }

        // Сохраняет текущие данные в файл
        public void Save(bool doValidate = true)
        {
            if (doValidate)
                m_data.Validate();

            XmlSerializer serializer = new XmlSerializer(typeof(SerializableData));
            using (Stream fileStream = new FileStream(m_fileName, FileMode.Create))
            {
                serializer.Serialize(fileStream, m_data);
            }
        }

        // Делает бэкап настроек
        // После этого действия можно менять настройки и в случае необходимости восстановить их через RestoreBackup()
        public void MakeBackup()
        {
            m_dataBackup = m_data.Clone() as SerializableData;
        }

        // Восстанавливает бэкап настроек, заблаговременно сделанный через MakeBackup()
        public void RestoreBackup()
        {
            if (m_dataBackup != null)
            {
                m_data = m_dataBackup;
                m_dataBackup = null;
            }
        }
    }
}
