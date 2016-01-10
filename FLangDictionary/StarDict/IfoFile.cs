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
        private class IfoFile
        {
            /** path to the ".ifo" file. */
            private string m_fileName;

            /** the type of file. */
            private string m_sameTypeSequence = "";

            /** author of dictionary. */
            private string m_author = "";

            /** dict url. */
            private string m_website = "";

            /** Description of book. */
            private string m_description = "";

            /** date. */
            private string m_date = "";

            /** version f dictionary. */
            public string Version { get; set; } = "";
            /** name of dictionary. */
            public string Bookname { get; set; } = "";
            /** decide if ".ifo" file is loaded. */
            public bool IsLoaded { get; set; } = false;
            /** number of entries stored in ".idx" file. */
            public long WordCount { get; set; } = 0;
            /** size of ".idx" file. */
            public long IdxFileSize { get; set; } = 0;

            /**
             * Constructor.
             * @param fileName file name to the path of ifo file.
             */
            public IfoFile(string fileName)
            {
                m_fileName = fileName;
                Load();
            }

            /**
             * Load .idx .
             */
            public void Load()
            {
                if (IsLoaded)
                    return;

                string strInput = File.ReadAllText(m_fileName, Encoding.UTF8);

                Version = GetStringForKey("version=", strInput); // get version

                // get number of entries
                WordCount = GetLongForKey("wordcount=", strInput);
                if (WordCount < 0)
                    return;

                // get size of ".idx" file
                IdxFileSize = GetLongForKey("idxfilesize=", strInput);
                if (IdxFileSize < 0)
                    return;

                m_sameTypeSequence = GetStringForKey("sametypesequence=", strInput);
                Bookname = GetStringForKey("bookname=", strInput);
                if (Bookname == null)
                    return;

                m_author = GetStringForKey("author=", strInput);
                m_website = GetStringForKey("website=", strInput);
                m_description = GetStringForKey("description=", strInput);
                m_date = GetStringForKey("date=", strInput);
                // make sure that ifo file is loaded successfully
                IsLoaded = WordCount > 0;
            }

            /**
             * load the properties again.
             */
            public void Reload()
            {
                IsLoaded = false;
                Load();
            }

            /**
             * find a long number follows the key in a string.
             * @param strKey the string key
             * @param str string
             * @return long
             */
            long GetLongForKey(string strKey, string str)
            {
                try
                {
                    return long.Parse(GetStringForKey(strKey, str).Trim());
                }
                catch (Exception)
                {
                    return 0;
                }
            }

            /**
             * find a string follows the key in a string.
             * @param strKey string key
             * @param str string
             * @return string
             */
            string GetStringForKey(string strKey, string str)
            {
                int keyLen = strKey.Length;

                int startPos = str.IndexOf(strKey) + keyLen;
                if (startPos < keyLen)
                {
                    return null;
                }

                str += '\0';

                int endPos = startPos - 1;

                while ((str[++endPos] != '\n') && (str[endPos] != '\0'))
                {
                }

                return str.Substring(startPos, endPos - startPos);
            }
        }
    }
}
