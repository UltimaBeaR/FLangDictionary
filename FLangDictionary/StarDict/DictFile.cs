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
        private class DictFile
        {
            /**
             * String that get file name.
             */
            private string m_fileName;

            /**
             * Constructor.
             * @param fileName get fileName and assign it to strFileName.
             */
            public DictFile(string fileName)
            {
                m_fileName = fileName;
            }
            /**
             * Get Word meaning by its offset and its meaning size.
             * @param offset offset that is get in .idx file.
             * @param size size that is get in .idx file
             * @return meaning of word data
             */
            public string GetWordData(long offset, long size)
            {
                if (!File.Exists(m_fileName))
                    return "File: " + m_fileName + " does not exist";

                string strMeaning = "not found";

                using (FileStream fileSteam = new FileStream(m_fileName, FileMode.Open))
                {
                    using (BinaryReader binaryReader = new BinaryReader(fileSteam))
                    {
                        fileSteam.Seek(offset, SeekOrigin.Begin);
                        byte[] bt = binaryReader.ReadBytes((int)size);
                        strMeaning = Encoding.UTF8.GetString(bt);
                    }
                }

                return strMeaning;
            }
        }
    }
}
