using System;
using System.IO;
using System.Text;

namespace ZipUnzipThreadProject
{
    public interface ICutting
    {
        void Cut(string mFilePath, IAddablePieces mQueeue);
    }

    public class CutInPiecesNormal : ICutting
    {
        public void Cut(string mFilePath, IAddablePieces mQueeue)
        {
            using (var reader = new BinaryReader(new FileStream(mFilePath, FileMode.Open, FileAccess.Read), Encoding.UTF8))
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
                    mQueeue.AddPiece(new FilePiece(counter, arBytes, 0));
                    counter++;
                }
            }
        }
    }

    public class CutInPiecesCompressed : ICutting
    {
        public void Cut(string mFilePath, IAddablePieces mQueeue)
        {
            using (var reader = new FileStream(mFilePath, FileMode.Open, FileAccess.Read))
            {
                var counter = 0;
                while (reader.Position < reader.Length)
                {
                    var buffer = new byte[8];
                    //читаем заголовок файла
                    reader.Read(buffer, 0, 8);
                    //выбираем из прочитанного размер блока
                    var compressedBlockLength = BitConverter.ToInt32(buffer, 4);
                    //Console.WriteLine(compressedBlockLength);
                    var comressedBytes = new byte[compressedBlockLength+1];
                    reader.Read(comressedBytes, 8, compressedBlockLength - 8);

                    var blockSize = BitConverter.ToInt32(comressedBytes, compressedBlockLength - 4);


                    mQueeue.AddPiece(new FilePiece(counter, comressedBytes, blockSize));
                    counter++;
                }
            }
        }
    }
}
