using System.IO;
using System.Text;

namespace ZipUnzipThreadProject
{
    /// <summary>
    /// разрезать файлик на кусочки
    /// </summary>
    public class CutUpTheFile
    {

        private readonly string m_FilePath;

        private readonly IAddablePieces m_Queeue;

        public CutUpTheFile(string mFilePath, IAddablePieces mQueeue)
        {
            m_FilePath = mFilePath;
            m_Queeue = mQueeue;
        }

        public void Cut()
        {
            using (var reader = new BinaryReader(new FileStream(m_FilePath, FileMode.Open, FileAccess.Read), Encoding.UTF8))
            {
                int counter = 0;
                var BUFFER_SIZE = AppPropertiesSingle.GetInstance().m_BufferSize;
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    reader.BaseStream.Seek(counter * BUFFER_SIZE, SeekOrigin.Begin);
                    var bufferSize = BUFFER_SIZE;
                    if (reader.BaseStream.Length - reader.BaseStream.Position <= BUFFER_SIZE)
                    {
                        bufferSize = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
                    }
                    var arBytes = reader.ReadBytes(bufferSize);
                    m_Queeue.AddPiece(new FilePiece(counter, arBytes));
                    counter++;
                }
            }
        }
    }
}
