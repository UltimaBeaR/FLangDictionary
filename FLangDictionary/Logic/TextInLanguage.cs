using System.Diagnostics;

namespace FLangDictionary.Logic
{
    // Текст статьи. Содержит Язык, текст и синтаксическую разметку текста, в случае если редактирование текста завершено
    public partial class TextInLanguage
    {
        private string m_text;

        public TextInLanguage(string languageCode, string text, bool finished)
        {
            Debug.Assert(languageCode != null && languageCode != string.Empty);
            Debug.Assert(text != null);

            LanguageCode = languageCode;
            m_text = text;
            BuildSyntaxLayout(finished);
        }

        // язык, на котором написана статья
        public string LanguageCode { get; private set; }

        // Завершено ли редактирование текста вручную пользователем
        public bool Finished
        {
            get
            {
                // Определяем Finished на основе того, есть ли синтаксическая разметка - она может существовать только в случае завершенного текста
                return m_syntaxLayout != null;
            }

            set
            {
                // Если произошли изменения finished
                if (Finished != value)
                    BuildSyntaxLayout(value);
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
                // Нельзя менять текст, если он завершен
                Debug.Assert(!Finished);

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
        void BuildSyntaxLayout(bool finished)
        {
            if (finished)
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
