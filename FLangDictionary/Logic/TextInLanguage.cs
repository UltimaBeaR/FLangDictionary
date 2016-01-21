using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

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

        // Синтаксическая разметка данного текста. С ее помощью можно определить, например, зная определенную позицию символа в тексте, 
        // к какому предложению и слову относится эта позиция, либо, например, что эта позиция вообще не является словом
        SyntaxLayout m_syntaxLayout;

        // Получает строку текста из слов в составе синтаксической разметки
        public string GetTextFromSyntaxLayout()
        {
            return m_syntaxLayout.ToString();
        }

        // По строке с индексами вида " 0 12 16 80 " получает список слов 
        public SyntaxLayout.Word[] GetPhraseWords(string phraseIndexes)
        {
            Debug.Assert(phraseIndexes != null);

            if (phraseIndexes == "")
                return new SyntaxLayout.Word[0];

            string[] indexes = phraseIndexes.Split(' ');
            SyntaxLayout.Word[] res = new SyntaxLayout.Word[indexes.Length - 2];
            for (int i = 1; i < indexes.Length - 1; i++)
            {
                res[i - 1] = m_syntaxLayout.GetWordByFirstIndex(Convert.ToInt32(indexes[i]));
                Debug.Assert(res[i - 1] != null);
            }

            return res;
        }

        // По списку слов получает строку вида " 0 12 16 80 " (в таком виде слова хранятся в базе данных)
        public static string GetPhraseIndexes(SyntaxLayout.Word[] phraseWords)
        {
            Debug.Assert(phraseWords != null);

            if (phraseWords.Length == 0)
                return "";

            StringBuilder sb = new StringBuilder(" ");

            foreach (var word in phraseWords)
            {
                sb.Append(word.FirstIndex.ToString());
                sb.Append(' ');
            }

            return sb.ToString();
        }

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
