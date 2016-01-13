using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLangDictionary.Logic
{
    // Коды некоторых языков (CultureInfo.Name, Language.Code) для использования в коде
    public static class LangCodes
    {
        public static string English = "en";
        public static string Spanish = "es";
        public static string Russian = "ru";
    }

    // Список языков, которые можно выбирать для рабочих областей и переводов статей/слов
    // Используются 3 названия, это код (то что пишется в базу данных и используется внутренне), имя на английском и имя на языке самого этого языка
    public class Languages
    {
        public class Language
        {
            public Language(string code, string name, string englishName)
            {
                Code = code;
                Name = name;
                EnglishName = englishName;
            }

            // Код языка, например "en", "ru" и так далее
            public string Code { get; private set; }
            // Имя, например "English", "Русский"
            public string Name { get; private set; }
            // Имя на английском, например "English", "Russian"
            public string EnglishName { get; private set; }

            // Имя для отображения, например в списках выбора языка
            public string DisplayName { get { return $"{EnglishName} - {Name}"; } }
        }

        // Список языков в алфавитном порядке
        List<Language> m_inAlphabetOrder;
        // Словарь по кодам языка - языку, для быстрого поиска по коду
        Dictionary<string, Language> m_dictionary;

        // Получает список языков в алфавитном порядке (порядок для варианта языков в английском написании)
        public IReadOnlyList<Language> InAlphabetOrder { get { return m_inAlphabetOrder; } }

        public Languages()
        {
            m_inAlphabetOrder = new List<Language>();
            m_dictionary = new Dictionary<string, Language>();

            foreach (var culture in CultureInfo.GetCultures(CultureTypes.NeutralCultures))
            {
                if (culture.Name != CultureInfo.InvariantCulture.Name)
                {
                    Language lang = new Language(culture.Name, culture.NativeName, culture.EnglishName);

                    m_inAlphabetOrder.Add(lang);
                    m_dictionary.Add(culture.Name, lang);
                }
            }

            m_inAlphabetOrder.SortByEnglishName();
        }

        // Получает язык по его коду
        public Language GetByCode(string code)
        {
            Language res;
            m_dictionary.TryGetValue(code, out res);

            return res;
        }
    }

    public static class LanguagesListExtensions
    {
        // Сортирует список языков в алфавитном порядке, по английскому варианту имени
        public static void SortByEnglishName(this List<Languages.Language> languages)
        {
            languages.Sort((a, b) => { return a.EnglishName.CompareTo(b.EnglishName); });
        }
    }
}
