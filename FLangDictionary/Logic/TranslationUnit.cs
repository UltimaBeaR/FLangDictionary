using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLangDictionary.Logic
{
    // Еденица перевода. То есть перевод одного или нескольких слов из разметки в статье на языке оригинала
    // Еденица перевода содержит слово/выражение(с переводом) в инфинитиве, а также перевод этого слова/выражения в текущем склонении
    // Инфинитив при этом не привязан к конкретно этому месту разметки а лежит глобально. То есть 2 разных слова в одной или нескольких разных статьях
    // могут иметь один и тот же инфинитив с одним вариантом перевода
    public class TranslationUnit
    {
        // Оригинальный (на иностранном языке) вариант слова или фразы в виде списка ссылок на элементы разметки слов в статье
        // По сути, если перевести их в текст то получится склоненный вариант фразы без знаков препинания, для которого и ставится в соответсвие фраза-перевод
        List<TextInLanguage.SyntaxLayout.Word> m_originalPhrase;
        // Переведенный (на язык перевода) вариант слова или фразы в склонении слова/фразы оригинала
        string m_translatedPhrase;
        // Вариант перевода этого слова/выражения в инфинитиве
        TranslationInInfinitive m_infinitiveTranslation;
    }

    // Перевод слова/словосочетания в определенном смысле в форме инфинитива
    // По сути любой словарь иностранного языка и состоит из таких вот элементов, плюс примеров
    public struct TranslationInInfinitive
    {
        public TranslationInInfinitive(string originalPhrase, string translatedPhrase)
        {
            this.originalPhrase = originalPhrase;
            this.translatedPhrase = translatedPhrase;
        }

        // Оригинальный (на иностранном языке) вариант слова или фразы
        public string originalPhrase;
        // Переведенный (на язык перевода) вариант слова или фразы
        public string translatedPhrase;
    }

    // Еденица художественного перевода. По сути это связь слова/фразы в тексте оригинала со словом/фразой в тексте художественного перевода
    public class ArtisticalTranslationUnit
    {
        // Фраза на языке оригинала
        List<TextInLanguage.SyntaxLayout.Word> m_originalPhrase;
        // Переведенная фраза
        List<TextInLanguage.SyntaxLayout.Word> m_artisticalTranslatedPhrase;
    }
}
