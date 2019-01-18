using System;
using System.IO;

namespace GZipLibrary
{
    /// <summary>
    /// кусочки файла
    /// если файл помещается в оперативку - пишем прямо тут, иначе - в файлы
    /// </summary>
    public class FilePiece
    {
        public long Id {  get; }
        private readonly byte[] m_Content;

        public byte[] GetContent()
        {
            var app = AppPropertiesSingle.GetInstance();
            return app.IsBigFile ? File.ReadAllBytes(FilePath) : m_Content;
        }

        public string FilePath { get; private set; }

        public FilePiece(long mId, byte[] mContent)
        {
            Id = mId;
            m_Content = mContent;
        }

        public FilePiece(string filePath)
        {
            FilePath = filePath;
        }
    }
}
