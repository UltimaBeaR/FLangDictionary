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
            m_artisticalTranslations.Add(artisticalTranslation.LanguageCode, artisticalTranslation);
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
            private string m_text;
            private bool m_finished;

            public TextInLanguage(string languageCode, string text, bool finished)
            {
                Debug.Assert(languageCode != null && languageCode != string.Empty);
                Debug.Assert(text != null);

                LanguageCode = languageCode;
                m_text = text;
                m_finished = finished;
                BuildSyntaxLayout();
            }

            // язык, на котором написана статья
            public string LanguageCode { get; private set; }

            // Завершено ли редактирование текста вручную пользователем
            public bool Finished
            {
                get
                {
                    return m_finished;
                }

                set
                {
                    bool changed = m_finished == value;
                    m_finished = value;

                    if (changed)
                        BuildSyntaxLayout();
                }
            }

            // текст статьи
            public string Text
            {
                get
                {
                    return m_text;
                }

                set
                {
                    // Нельзя менять текст, если у него завершено редактирование
                    Debug.Assert(!m_finished);

                    m_text = value;
                }
            }

            // Синтаксическая разметка. Доступна только, когда текст завершен (finished)
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
                if (m_finished)
                {
                    // Так как в разных языках разметка может формироваться по разному (например в испанском языке есть вопросительный знак непосредственно перед предложением,
                    // и он означает начало предложения, в других же языках такого нет), то будем запрашивать построитель разметок для языка этого текста, который построит
                    // разметку с учетом особенностей языка этой разметки

                    // Запросим нужный построитель разметки по известному языку текста
                    SyntaxLayoutBuilder syntaxLayoutBuilder = SyntaxLayoutBuilder.GetBuilder(LanguageCode);
                    // Строим разметку
                    m_syntaxLayout = syntaxLayoutBuilder.Build(Text);
                }
                else
                    m_syntaxLayout = null;
            }
        }
    }
}
