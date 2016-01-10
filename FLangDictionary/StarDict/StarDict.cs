using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLangDictionary.StarDict
{
    public partial class StarDict
    {
        private const string ext_dict = ".dict";
        private const string ext_index = ".idx";
        private const string ext_info = ".ifo";

        /** number of the nearest word that is displayed. */
        private const int nearest = 10;

        /** Dict directory.(path to the .dict file). */
        private string m_url;

        /** decide if object has loaded the entries. */
        private bool m_available;

        /** ifo file. */
        private IfoFile m_ifoFile;

        /** idx file. */
        private IdxFile m_idxFile;

        /** dict file. */
        private DictFile m_dictFile;

        /**
         * Constructor to load dictionary with given path.
         * @param url Path of one of stardict file or Path of folder contains stardict files
         * @deprecated using StarDict.loadDict(String url)
         */
        public StarDict(string url)
        {
            if (!File.GetAttributes(url).HasFlag(FileAttributes.Directory))
            {
                m_url = Path.Combine(Path.GetDirectoryName(url), Path.GetFileNameWithoutExtension(url));
                m_ifoFile = new IfoFile(m_url + ext_info);
                m_idxFile = new IdxFile(m_url + ext_index, m_ifoFile.WordCount, m_ifoFile.IdxFileSize);
                m_dictFile = new DictFile(m_url + ext_dict);
            }
            else {
                string[] list = Directory.GetFiles(url);

                string infoPath = null;
                string indexPath = null;
                string dictPath = null;

                // Build table to mapping the file extension and file name
                for (int i = list.Length - 1; i >= 0; i--)
                {
                    if (list[i].EndsWith(ext_info))
                        infoPath = list[i];
                    else if (list[i].EndsWith(ext_index))
                        indexPath = list[i];
                    else if (list[i].EndsWith(ext_dict))
                        dictPath = list[i];
                }

                m_ifoFile = new IfoFile(Path.Combine(url, infoPath));
                m_idxFile = new IdxFile(Path.Combine(url, indexPath), m_ifoFile.WordCount, m_ifoFile.IdxFileSize);
                m_dictFile = new DictFile(Path.Combine(url, dictPath));
            }

            if (m_ifoFile.IsLoaded && m_idxFile.IsLoaded)
                m_available = true;
        }

        /**
         * get book name of dictionary.
         * @return Book name
         */
        public string DictName
        {
            get
            {
                return m_ifoFile.Bookname.Replace("\r", "").Trim();
            }
        }

        /**
         * get book version.
         * @return version of a dictionary
         */
        public string DictVersion
        {
            get
            {
                return m_ifoFile.Version;
            }
        }

        /**
         * get amount of words in a StarDict dictionary (within 3 files).
         * @return a long totalWord.
         * @author LongNX
         */
        public int TotalWords
        {
            get
            {
                return GetWordEntry().Count;
            }
        }

        /**
         * get word content from an idx. let say the stardict-dictd-easton-2.4.2, we give this method the idx 1000 and it
         * return us the "diana".
         * @param idx index
         * @return word
         * @author LongNX
         */
        public string GetWordByIndex(int idx)
        {
            string word = GetWordEntry()[idx].lwrWord;
            return word;
        }

        /**
         * lookup a word by its index.
         * @param idx index of a word
         * @return word data
         */
        public string LookupWord(int idx)
        {
            if (idx < 0 || idx >= m_idxFile.WordCount)
            {
                return "not found";
            }
            WordEntry tempEntry = m_idxFile.GetEntryList()[idx];

            return m_dictFile.GetWordData(tempEntry.offset, tempEntry.size);
        }

        /**
         * lookup a word.
         * @param word that is looked up in database.
         * @return word data
         */
        public string LookupWord(string word)
        {
            if (!m_available)
            {
                return "the dictionary is not available";
            }
            int idx = (int)m_idxFile.FindIndexForWord(word);

            return LookupWord(idx);
        }

        /**
         * load index file and info file.
         */
        public void ReLoad()
        {
            m_available = false;
            m_ifoFile.Reload();
            m_idxFile.Reload();

            if (m_ifoFile.IsLoaded && m_idxFile.IsLoaded)
            {
                m_available = true;
            }
        }

        /**
         * get the nearest of the chosen word.
         * @param word that is looked up in database
         * @return a list of nearest word.
         */
        public List<string> GetNearestWords(string word)
        {
            if (m_available)
            {
                int idx = (int)m_idxFile.FindIndexForWord(word);
                int nMax = nearest + idx;
                if (nMax > m_idxFile.WordCount)
                {
                    nMax = (int)m_idxFile.WordCount;
                }
                List<string> wordList = new List<string>();
                for (int i = idx; i < nMax; i++)
                {
                    if (i != 0)
                    {
                        Word tempWord = new Word();
                        tempWord.word = m_idxFile.GetEntryList()[i].word;
                        tempWord.index = i;
                        wordList.Add(tempWord.word);
                    }
                }
                return wordList;
            }
            return null;
        }

        /**
         * check if a word is in dictionary.
         * @param word that is looked up in database
         * @return true if exists, false otherwise
         */
        public bool ExistWord(string word)
        {
            int wordIndex = (int)m_idxFile.FindIndexForWord(word);

            if (wordIndex >= m_idxFile.WordCount)
            {
                return false;
            }

            string lwrWord = word.ToLower();
            if (lwrWord.Equals(m_idxFile.GetEntryList()[wordIndex].lwrWord))
            {
                return true;
            }

            return false;
        }

        /**
         * get a list of word entry.
         * @return list of word entry
         */
        private List<WordEntry> GetWordEntry()
        {
            return m_idxFile.GetEntryList();
        }
    }
}
