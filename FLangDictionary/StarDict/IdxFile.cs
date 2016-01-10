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
        private class IdxFile
        {
            /** constant of 0x000000FF. */
            private const uint byteFirst = 0x000000FF;

            /** constant of 0x0000FF00. */
            private const uint secondByte = 0x0000FF00;

            /** constant of 0x00FF0000. */
            private const uint thirdByte = 0x00FF0000;

            /** constant of 0xFF000000. */
            private const uint fourthByte = 0xFF000000;

            /** constant of 0xFFFFFFFFL. */
            private const long fixthByte = 0xFFFFFFFFL;

            /** constant of number 3. */
            private const int noThree = 3;

            /** constant of number 8. */
            private const int noEight = 8;

            /** constant of number 9. */
            private const int noNine = 9;

            /** constant of number 16. */
            private const int noSixteen = 16;

            /** constant of number 24. */
            private const int noTwentyFour = 24;

            /** constant of number 4. */
            private const int aByte = 4;

            /** path to the ".idx" file. */
            private string m_fileName;

            /** decide if the properties are loaded. */
            private bool m_isLoaded = false;

            /** number of word. */
            private long m_wordCount;

            /** File size. */
            private long m_idxFileSize;

            /** store the list of entries. */
            private List<WordEntry> m_entryList;

            /**
             * constructor.
             * @param fileName path to .idx file.
             * @param wordCount number of word.
             * @param fileSize the file size.
             */
            public IdxFile(string fileName, long wordCount, long fileSize)
            {
                m_wordCount = wordCount;
                m_idxFileSize = fileSize;
                m_fileName = fileName;
                Load();
            }

            /**
             * accessor of longIdxFileSize.
             * @return longIdxFileSize
             */
            public long IdxFileSize
            {
                get
                {
                    return m_idxFileSize;
                }
            }

            /**
             * accessor of boolIsLoaded.
             * @return boolIsLoaded
             */
            public bool IsLoaded
            {
                get
                {
                    return m_isLoaded;
                }
            }

            /**
             * accessor of longWordCount.
             * @return longWordCount
             */
            public long WordCount
            {
                get
                {
                    return m_wordCount;
                }
            }

            /**
             * accessor of strFileName.
             * @return strFileName
             */
            public string FileName
            {
                get
                {
                    return m_fileName;
                }
            }

            /**
             * accessor of entryList.
             * @return entryList
             */
            public List<WordEntry> GetEntryList()
            {
                return m_entryList;
            }

            /**
             * load properties.
             */
            public void Load()
            {
                if (m_isLoaded || !File.Exists(m_fileName))
                    return;

                byte[] bt;
                using (FileStream fileSteam = new FileStream(m_fileName, FileMode.Open))
                {
                    using (BinaryReader binaryReader = new BinaryReader(fileSteam))
                    {
                        bt = binaryReader.ReadBytes((int)m_idxFileSize);
                    }
                }

                m_entryList = new List<WordEntry>();
                int startPos; // start position of entry
                int endPos = 0; // end position of entry
                WordEntry tempEntry;

                for (long i = 0; i < m_wordCount; i++)
                {
                    tempEntry = new WordEntry();
                    // read the word
                    startPos = endPos;
                    while (bt[endPos] != '\0')
                    {
                        endPos++;
                    }

                    tempEntry.word = Encoding.UTF8.GetString(bt, startPos, endPos - startPos);
                    tempEntry.lwrWord = tempEntry.word.ToLower();
                    // read the offset of the meaning (in .dict file)
                    ++endPos;
                    tempEntry.offset = ReadAnInt32(bt, endPos);
                    // read the size of the meaning (in .dict file)
                    endPos += aByte;
                    tempEntry.size = ReadAnInt32(bt, endPos);
                    endPos += aByte;
                    m_entryList.Add(tempEntry);
                }
                m_isLoaded = true;
            }

            /**
             * reload .idx file.
             */
            public void Reload()
            {
                m_isLoaded = false;
                Load();
            }

            /**
             * convert 4 char array to an integer.
             * @param str array of byte that is read from .idx file.
             * @param beginPos the position of a word.
             * @return a long.
             */
            private long ReadAnInt32(byte[] str, int beginPos)
            {
                uint firstByte = (byteFirst & str[beginPos]);
                uint secondByte = (byteFirst & str[beginPos + 1]);
                uint thirdByte = (byteFirst & str[beginPos + 2]);
                uint fourthByte = (byteFirst & str[beginPos + noThree]);

                return ((firstByte << noTwentyFour | secondByte << noSixteen | thirdByte << noEight | fourthByte)) & fixthByte;
            }

            /**
             * convert an integer to a char array.
             * @param val an integer
             * @return a char array
             */
            private byte[] ConvertAnInt32(int val)
            {
                byte[] str = new byte[aByte];
                str[0] = (byte)((val & fourthByte) >> noTwentyFour);
                str[1] = (byte)((val & thirdByte) >> noSixteen);
                str[2] = (byte)((val & secondByte) >> noEight);
                str[noThree] = (byte)((val & byteFirst));
                return str;
            }

            /**
             * return the index of a word in entry list.
             * @param word the chosen word
             * @return index of this word
             */
            public long FindIndexForWord(string word)
            {
                if (!m_isLoaded)
                {
                    return m_wordCount;
                }
                long first = 0;
                long last = (int)m_wordCount - 1;
                long mid;
                string lwrWord = word.ToLower();
                // use binary search
                do
                {
                    mid = (first + last) / 2;
                    int cmp = lwrWord.CompareTo((m_entryList[(int)mid]).lwrWord);
                    if (cmp == 0)
                    {
                        return mid; // return index if found
                    }
                    if (cmp > 0)
                    {
                        first = mid + 1;
                    }
                    else {
                        last = mid - 1;
                    }
                } while (first <= last);
                // if not found
                /*
                 * if (first < longWordCount) { while (first < longWordCount) { if (((WordEntry) entryList.get( (int)
                 * first)).getStrLwrWord().compareTo(lwrWord) > 0) { break; } else { first++; } } }
                 */
                first = -1;
                return first;
            }
        }
    }
}
