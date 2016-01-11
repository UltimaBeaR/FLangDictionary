using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FLangDictionary.Logic
{
    // Построитель разметки для текста статьи на определенном языке. 
    public abstract class SyntaxLayoutBuilder
    {
        static Dictionary<string, SyntaxLayoutBuilder> m_builders;

        static SyntaxLayoutBuilder()
        {
            // Создадим словарь построителей синтаксической разметки из тех классов, которые присутсвуют в текущей сборке

            m_builders = new Dictionary<string, SyntaxLayoutBuilder>();

            // Находим все классы-наследники от SyntaxLayoutBuilder из всех запущенных сборок и создаем их экземпляры
            var buildersList =
                from type in Assembly.GetExecutingAssembly().GetTypes()
                where type.IsSubclassOf(typeof(SyntaxLayoutBuilder))
                select Activator.CreateInstance(type) as SyntaxLayoutBuilder;

            // Далее записываем полученные экземпляры построителей в словарь
            foreach (SyntaxLayoutBuilder builder in buildersList)
            {
                foreach (string language in builder.Languages)
                {
                    Debug.Assert(!m_builders.ContainsKey(language));
                    m_builders.Add(language, builder);
                }
            }
        }

        // По заданному языку выдает построитель синтаксической разметки
        public static SyntaxLayoutBuilder GetBuilder(string language)
        {
            Debug.Assert(language != null);

            SyntaxLayoutBuilder result;
            if (!m_builders.TryGetValue(language, out result))
                // Если билдера по запрашиваемому языку не нашлось - берем дефолтный билдер(универсальный)
                result = m_builders[""];
                

            Debug.Assert(result != null);
            return result;
        }

        // Языки, поддерживаемые данным построителем разметки
        // Если в списке есть "", значит это дефолтный построитель
        // На каждый язык должен быть только один построитель (включая "дефолтный" null-овый язык)
        public string[] Languages { get; private set; }

        public SyntaxLayoutBuilder(string[] languages)
        {
            Debug.Assert(languages != null && languages.Length > 0 && !languages.Contains(null));
            Languages = languages;
        }

        // Строит синтаксическую разметку
        public abstract TextInLanguage.SyntaxLayout Build(string text);

        // Универсальный метод постройки. Может работать не для всех языков, но работает для большинства, это точно
        protected TextInLanguage.SyntaxLayout BuildUniversal(string text, string wordLetters, string sentenceBeginingLetters, string sentenceEndingLetters, char exclamationEnding, char questionEnding)
        {
            TextInLanguage.SyntaxLayout result = new TextInLanguage.SyntaxLayout();

            TextInLanguage.SyntaxLayout.Sentence currentSentence = null;
            string currentWord = string.Empty;
            int currentWordStartIndex = 0;
            int lastWordLetterIndex = -1;

            for (int letterIdx = 0; letterIdx < text.Length; letterIdx++)
            {
                bool isLastLetter = letterIdx == text.Length - 1;
                char letter = text[letterIdx];

                if (currentSentence == null)
                {
                    // Ищем начало предложения. Оно должно начинаться с буквы
                    if (wordLetters.Contains(letter))
                        currentSentence = result.AddSentence(letterIdx);
                }

                if (currentSentence != null)
                {
                    // Если это буква из слова
                    if (wordLetters.Contains(letter))
                    {
                        // Если слово только началось - запишем текущий индекс как начальный для этого слова
                        if (currentWord == string.Empty)
                            currentWordStartIndex = letterIdx;

                        // Добавим в текущее слово эту букву
                        currentWord += letter;

                        lastWordLetterIndex = letterIdx;
                    }
                    else
                    {
                        // Текущий символ - не часть слова
                        // В этом случае заканчиваем текущее набранное слово, если таковое есть
                        BuildUniversal_FinishWord(currentSentence, ref currentWord, currentWordStartIndex, lastWordLetterIndex);
                    }

                    bool isLastLetterOrSentenceBegining = isLastLetter || sentenceBeginingLetters.Contains(letter);

                    // Если это конец предложения, или это последний символ во всем тексте
                    if (sentenceEndingLetters.Contains(letter) || isLastLetterOrSentenceBegining)
                    {
                        // Если было накопленное слово - закончим его
                        BuildUniversal_FinishWord(currentSentence, ref currentWord, currentWordStartIndex, lastWordLetterIndex);
                        currentSentence.LastIndex = lastWordLetterIndex;

                        var props = new TextInLanguage.SyntaxLayout.Sentence.Properties();
                        props.kind = TextInLanguage.SyntaxLayout.Sentence.Kind.Declarative;

                        if (!isLastLetterOrSentenceBegining)
                        {
                            if (letter == exclamationEnding)
                                props.kind = TextInLanguage.SyntaxLayout.Sentence.Kind.Exclamation;
                            else if (letter == questionEnding)
                                props.kind = TextInLanguage.SyntaxLayout.Sentence.Kind.Question;
                        }

                        currentSentence.Props = props;

                        // Предложение закончилось, теперь ищем следующее предложение
                        currentSentence = null;
                    }
                }
            }

            result.EraseEmptySentences();
            return result;
        }

        void BuildUniversal_FinishWord(TextInLanguage.SyntaxLayout.Sentence sentence, ref string word, int startIndex, int endIndex)
        {
            if (word != string.Empty)
            {
                var props = new TextInLanguage.SyntaxLayout.Word.Properties();

                sentence.AddWord(word, startIndex, endIndex, props);
                word = string.Empty;
            }
        }
    }

    // Построитель синтаксической разметки по умолчанию (Используется для английского, русского языков)
    public class DefaultSyntaxLayoutBuilder : SyntaxLayoutBuilder
    {
        public DefaultSyntaxLayoutBuilder()
            : base(new string[] { "", LangCodes.English, LangCodes.Russian })
        {
        }

        public override TextInLanguage.SyntaxLayout Build(string text)
        {
            const string englishAlphabet = @"abcdefghijklmnopqrstuvwxyz";
            const string englishSpecialSymbol = @"'";
            const string russianAlphabet = @"абвгдеёжзийклмнопрстуфхцчшщъыьэюя";
            const string sentenceEndings = @".;!?";

            // Получаем строку, содержащую валидные символы, которые могут встречаться в словах
            string wordLetters = englishAlphabet + englishAlphabet.ToUpper() + englishSpecialSymbol +
                russianAlphabet + russianAlphabet.ToUpper();

            return BuildUniversal(text, wordLetters, string.Empty, sentenceEndings, '!', '?');
        }
    }

    // Построитель синтаксической разметки для испанского языка
    public class SpanishSyntaxLayoutBuilder : SyntaxLayoutBuilder
    {
        public SpanishSyntaxLayoutBuilder()
            : base(new string[] { LangCodes.Spanish })
        {
        }

        public override TextInLanguage.SyntaxLayout Build(string text)
        {
            const string spanishAlphabet = @"abcdefghijklmnñopqrstuvwxyzáéíóú";
            const string sentenceBeginings = @"¡¿";
            const string sentenceEndings = @".;!?";

            // Получаем строку, содержащую валидные символы, которые могут встречаться в словах
            string wordLetters = spanishAlphabet + spanishAlphabet.ToUpper();

            return BuildUniversal(text, wordLetters, sentenceBeginings, sentenceEndings, '!', '?');
        }
    }
}