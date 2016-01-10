using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLangDictionary.Logic
{
    // Статья на иностранном языке. Может иметь один или несколько художественных переводов
    public partial class Article
    {
        // Оригинальный текст статьи (на оригинальном иностранном языке)
        public TextInLanguage OriginalText { get; private set; }

        public Article(TextInLanguage originalText)
        {
            Debug.Assert(originalText != null);
            OriginalText = originalText;

            m_artisticalTranslations = new Dictionary<string, TextInLanguage>();
        }

        // Добавить художественный перевод
        public void AddArtisticalTranslation(TextInLanguage artisticalTranslation)
        {
            Debug.Assert(artisticalTranslation != null);
            m_artisticalTranslations.Add(artisticalTranslation.Language, artisticalTranslation);
        }

        // Словарь художественных переводов этой статьи. Ключ - язык текста-перевода
        Dictionary<string, TextInLanguage> m_artisticalTranslations;

        // Список едениц перевода для этой статьи
        List<TranslationUnit> m_translationUnits;
    }

    public partial class Article
    {
        // Текст статьи
        public partial class TextInLanguage
        {
            public TextInLanguage(string language, string text)
            {
                Debug.Assert(language != null && language != string.Empty);
                Debug.Assert(text != null);

                Language = language;
                Text = text;

                // Строим синтаксическую разметку для данного текста
                BuildSyntaxLayout();
            }

            // язык, на котором написана статья
            public string Language { get; private set; }
            // текст статьи
            public string Text { get; private set; }

            public SyntaxLayout Layout { get { return m_syntaxLayout; } }

            // Получает строку текста из слов в составе синтаксической разметки
            public string GetTextFromSyntaxLayout()
            {
                return m_syntaxLayout.ToString();
            }

            // Синтаксическая разметка данного текста. С ее помощью можно определить, например, зная определенную позицию символа в тексте, 
            // к какому предложению и слову относится эта позиция, либо, например, что эта позиция вообще не является словом
            SyntaxLayout m_syntaxLayout;

            // Строит синтаксическую разметку по данному тексту и помещает ее в m_syntaxLayout
            void BuildSyntaxLayout()
            {
                // Так как в разных языках разметка может формироваться по разному (например в испанском языке есть вопросительный знак непосредственно перед предложением,
                // и он означает начало предложения, в других же языках такого нет), то будем запрашивать построитель разметок для языка этого текста, который построит
                // разметку с учетом особенностей языка этой разметки

                // Запросим нужный построитель разметки по известному языку текста
                SyntaxLayoutBuilder syntaxLayoutBuilder = SyntaxLayoutBuilder.GetBuilder(Language);
                // Строим разметку
                m_syntaxLayout = syntaxLayoutBuilder.Build(Text);
            }
        }
    }
}
