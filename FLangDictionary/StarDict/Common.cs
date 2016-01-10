using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLangDictionary.StarDict
{
    public partial class StarDict
    {
        private struct WordEntry
        {
            /** lower case of str_word. */
            public string lwrWord;

            /** Word. */
            public string word;

            /** position of meaning of this word in ".dict" file. */
            public long offset;

            /** length of the meaning of this word in ".dict" file. */
            public long size;
        }

        private struct Word
        {
            /** Word. */
            public string word;

            /** index. */
            public int index;
        }
    }
}
