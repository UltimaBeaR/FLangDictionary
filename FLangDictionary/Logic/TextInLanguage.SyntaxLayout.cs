using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLangDictionary.Logic
{
    public partial class TextInLanguage
    {
        // Синтаксическая разметка текста. То есть те синтаксические элементы, которые есть
        // в этом тексте. Например предложения, внутри которых есть слова.
        // Знаки препинания при этом в разметку не входят (хотя и можно по позиции знака определить в каком предложении он находится и его позицию между слов)
        public class SyntaxLayout
        {
            // Предложение в синтаксической разметке текста статьи. Состоит из слов
            public class Sentence
            {
                // Вид предожения
                public enum Kind
                {
                    Unknown, //< Вид предложения неизвестен
                    Declarative, //< Повествовательное
                    Exclamation, //< Восклицательное
                    Question //< Вопросительное
                }

                // Свойства предложения
                public struct Properties
                {
                    public Kind kind;
                }

                public Sentence(int firstIndex)
                {
                    Debug.Assert(firstIndex >= 0);


                    m_firstIndex = firstIndex;
                    m_lastIndex = m_firstIndex;

                    m_words = new List<Word>();
                }

                public int FirstIndex { get { return m_firstIndex; } }
                public int LastIndex { get { return m_lastIndex; } set { Debug.Assert(value >= m_firstIndex); m_lastIndex = value; } }
                public Properties Props { get { return m_properties; } set { m_properties = value; } }

                public int WordCount { get { return m_words.Count; } }

                // Добавляет новое слово в предложение
                public void AddWord(string text, int firstIndex, int lastIndex, Word.Properties properties)
                {
                    m_words.Add(new Word(this, text, firstIndex, lastIndex, properties));
                }

                public Word GetWordByIndex(int wordIndex)
                {
                    if (wordIndex < 0 || wordIndex >= m_words.Count)
                        return null;

                    return m_words[wordIndex];
                }

                // Получает слово по его значению FirstIndex
                public Word GetWordByFirstIndex(int firstIndex)
                {
                    for (int wordIdx = 0; wordIdx < m_words.Count; wordIdx++)
                    {
                        if (m_words[wordIdx].FirstIndex == firstIndex)
                            return m_words[wordIdx];
                    }

                    return null;
                }

                // Получает локальный индекс слова в текущем предложении по глобальному индексу символа в полном тексте статьи
                // Возвращаемое значение работает также как в SyntaxLayout::GetWordInSentenceIndexByTextLetterIndex()
                public bool GetWordIndexByTextLetterIndex(int textLetterIndex, out int wordIndex)
                {
                    wordIndex = -1;

                    // Слов вообще нет, только в этом случае отсутсвует выходной индекс
                    if (m_words.Count == 0)
                        return false;

                    if (textLetterIndex <= m_words[0].FirstIndex)
                    {
                        wordIndex = 0;
                        return textLetterIndex == m_words[0].FirstIndex;
                    }

                    if (textLetterIndex >= m_words[m_words.Count - 1].LastIndex)
                    {
                        wordIndex = m_words.Count - 1;
                        return textLetterIndex == m_words[m_words.Count - 1].LastIndex;
                    }

                    for (int wordIdx = 0; wordIdx < m_words.Count; wordIdx++)
                    {
                        Word word = m_words[wordIdx];

                        if (textLetterIndex >= word.FirstIndex && textLetterIndex <= word.LastIndex)
                        {
                            wordIndex = wordIdx;
                            return true;
                        }
                        else if (textLetterIndex < word.FirstIndex)
                        {
                            wordIndex = Math.Max(0, wordIdx - 1);
                            return false;
                        }
                    }

                    wordIndex = m_words.Count - 1;
                    return false;
                }

                public override string ToString()
                {
                    string res = string.Empty;
                    foreach (var word in m_words)
                    {
                        if (res != string.Empty)
                            res += " ";

                        res += word.ToString();
                    }

                    switch (m_properties.kind)
                    {
                        case Kind.Declarative: res += '.'; break;
                        case Kind.Exclamation: res += '!'; break;
                        case Kind.Question: res += '?'; break;
                        default: res += "#"; break;
                    }

                    return res;
                }

                // first и last индексы - это индексы в тексте TextInLanguage, обозначающие границы этого предложения в тексте статьи
                // внутрь границ будет входить точка, знак восклицания, вопросительный знак и так далее
                int m_firstIndex;
                int m_lastIndex;

                // Слова, из которых и состоит данное предложение
                List<Word> m_words;

                // Свойства предолжения
                Properties m_properties;
            }

            // Слово в синтаксической разметке текста статьи (слова всегда находятся внутри предложений Sentence)
            public class Word
            {
                // Свойства слова
                public struct Properties
                {
                }

                public Word(Sentence sentence, string text, int firstIndex, int lastIndex, Properties properties)
                {
                    Debug.Assert(sentence != null);
                    Debug.Assert(text != null && text != string.Empty);
                    Debug.Assert(firstIndex >= 0);
                    Debug.Assert(lastIndex >= firstIndex);

                    Sentence = sentence;
                    m_text = text;
                    m_firstIndex = firstIndex;
                    m_lastIndex = lastIndex;
                    m_properties = properties;
                }

                public override string ToString()
                {
                    return m_text;
                }

                // Предложение, в которое входит это слово
                public Sentence Sentence { get; private set; }

                // Текст слова
                string m_text;

                // first и last индексы - это индексы в тексте TextInLanguage, обозначающие границы этого слова в тексте статьи
                int m_firstIndex;
                int m_lastIndex;

                // Свойства слова
                Properties m_properties;

                public int FirstIndex { get { return m_firstIndex; } }

                public int LastIndex { get { return m_lastIndex; } }
            }

            public SyntaxLayout()
            {
                m_sentences = new List<Sentence>();
            }

            // Добавляет новое предложение в разметку
            public Sentence AddSentence(int firstIndex)
            {
                Sentence result = new Sentence(firstIndex);
                m_sentences.Add(result);
                return result;
            }

            // Индекс предложения + слова в этом предложении
            public struct WordInSentenceIndex
            {
                // Индекс предложения в синтаксической разметке текста
                // -1 является невалидным значением (используется если надо показать что предложение не найдено)
                public int sentenceIndex;
                // Индекс слова в этом предложении
                // -1 является невалидным значением аналогично индексу предложения
                public int wordIndex;
            }

            // Удаляем пустые предложения, которые могли добавиться в разметку в ходе постройки
            public void EraseEmptySentences()
            {
                for (int index = m_sentences.Count - 1; index >= 0; index--)
                {
                    if (m_sentences[index].WordCount == 0)
                        m_sentences.RemoveAt(index);
                }
            }

            // Получает слово по его значению FirstIndex (null, если такого нет)
            public Word GetWordByFirstIndex(int firstIndex)
            {
                for (int sentenceIdx = 0; sentenceIdx < m_sentences.Count; sentenceIdx++)
                {
                    if (firstIndex >= m_sentences[sentenceIdx].FirstIndex && firstIndex <= m_sentences[sentenceIdx].LastIndex)
                        return m_sentences[sentenceIdx].GetWordByFirstIndex(firstIndex);
                }

                return null;
            }

            // По индексу символа из исходной строки текста получает индекс слова в предложении
            // Если вернет true, значит попали точно в слово. Если false, значит индекс символа не на слове и будет возвращен индекс БЛИЖАЙШЕГО слова
            // например если мы запрашиваем пробел после слова, то будет выдано это слово.
            public bool GetWordInSentenceIndexByTextLetterIndex(int textLetterIndex, out WordInSentenceIndex wordInSentenceIndex)
            {
                wordInSentenceIndex.sentenceIndex = -1;
                wordInSentenceIndex.wordIndex = -1;

                // Предложений вообще нет, только в этом случае отсутсвует выходные индексы
                if (m_sentences.Count == 0)
                    return false;
                    
                if (textLetterIndex <= m_sentences[0].FirstIndex)
                {
                    wordInSentenceIndex.sentenceIndex = 0;
                    wordInSentenceIndex.wordIndex = 0;
                    return textLetterIndex == m_sentences[0].FirstIndex;
                }

                if (textLetterIndex >= m_sentences[m_sentences.Count - 1].LastIndex)
                {
                    wordInSentenceIndex.sentenceIndex = m_sentences.Count - 1;
                    wordInSentenceIndex.wordIndex = m_sentences[m_sentences.Count - 1].WordCount - 1;
                    return textLetterIndex == m_sentences[m_sentences.Count - 1].LastIndex;
                }

                for (int sentenceIdx = 0; sentenceIdx < m_sentences.Count; sentenceIdx++)
                {
                    Sentence sentence = m_sentences[sentenceIdx];

                    if (textLetterIndex >= sentence.FirstIndex && textLetterIndex <= sentence.LastIndex)
                    {
                        wordInSentenceIndex.sentenceIndex = sentenceIdx;
                        return sentence.GetWordIndexByTextLetterIndex(textLetterIndex, out wordInSentenceIndex.wordIndex);
                    }
                    else if (textLetterIndex < sentence.FirstIndex)
                    {
                        wordInSentenceIndex.sentenceIndex = Math.Max(0, sentenceIdx - 1);
                        wordInSentenceIndex.wordIndex = m_sentences[wordInSentenceIndex.sentenceIndex].WordCount - 1;
                        return false;
                    }
                }

                wordInSentenceIndex.sentenceIndex = m_sentences.Count - 1;
                wordInSentenceIndex.wordIndex = m_sentences[m_sentences.Count - 1].WordCount - 1;
                return false;
            }

            public Sentence GetSentenceByIndex(int sentenceIndex)
            {
                if (sentenceIndex < 0 || sentenceIndex >= m_sentences.Count)
                    return null;

                return m_sentences[sentenceIndex];
            }

            public Word GetWordByIndex(WordInSentenceIndex wordIndex)
            {
                Sentence sentence = GetSentenceByIndex(wordIndex.sentenceIndex);
                if (sentence == null)
                    return null;

                return sentence.GetWordByIndex(wordIndex.wordIndex);
            }

            public override string ToString()
            {
                string res = string.Empty;
                foreach (var sentence in m_sentences)
                {
                    if (res != string.Empty)
                        res += " ";

                    res += sentence.ToString();
                }

                return res;
            }

            // Предложения, из которых состоит синтаксическая разметка
            List<Sentence> m_sentences;
        }
    }
}
