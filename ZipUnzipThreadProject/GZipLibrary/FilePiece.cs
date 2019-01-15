using System;
using System.IO;

namespace GZipLibrary
{
    /// <summary>
    /// кусочки файла
    /// если файл помещаетсяв оперативку - пишем прямо тут, иначе - в файлы
    /// </summary>
    public class FilePiece
    {
        public long Id {  get; }
        private byte[] m_Content;

        public byte[] GetContent()
        {
            var app = AppPropertiesSingle.GetInstance();
            return app.IsBigFile ? File.ReadAllBytes(m_FilePath) : m_Content;
        }
        public int BlockSize { get; }
        public int OutSize {  get; }

        private string m_FilePath;

        public FilePiece(long mId, byte[] mContent, int blockSize, int outSize)
        {
            Id = mId;
            var app = AppPropertiesSingle.GetInstance();
            if (app.IsBigFile)
            {
                AddPieceAsFile(mContent);
            }
            else
            {
                m_Content = mContent;
            }
            BlockSize = blockSize;
            OutSize = outSize;
        }

        private void AddPieceAsFile(byte[] fileBytes)
        {
            var app = AppPropertiesSingle.GetInstance();
            var randomFileName = Path.GetRandomFileName();
            randomFileName = DateTime.Now.Millisecond + randomFileName;
            m_FilePath = Path.Combine(app.TempPath, randomFileName);
            File.WriteAllBytes(m_FilePath, fileBytes);
        }

        public void CleanContent()
        {
            var app = AppPropertiesSingle.GetInstance();
            if (app.IsBigFile)
            {
                File.Delete(m_FilePath);
            }
            else
            {
                m_Content = new byte[0];
            }
        }
    }
}
