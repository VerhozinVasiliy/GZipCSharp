using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace ZipUnzipThreadProject
{
    /// <summary>
    /// упаковка/распаковка частей
    /// </summary>
    public abstract class ProcessPackingAbstract
    {
        private readonly IEnumerable<FilePiece> m_InQueue;
        private readonly IAddablePieces m_ProceccedQueue;

        //public volatile int m_Procents;

        protected ProcessPackingAbstract(IEnumerable<FilePiece> mInQueue, IAddablePieces mProceccedQueue)
        {
            m_InQueue = mInQueue;
            m_ProceccedQueue = mProceccedQueue;
        }

        public void ProcessPacking()
        {
            var orderedList = m_InQueue
                .OrderBy(w => w.Id)
                .ToList();
            //int i = 0;
            //int len = orderedList.Count;
            foreach (var filePiece in orderedList)
            {
                //i++;
                //var oldProc = m_Procents;
                //m_Procents = i * 100 / len;
                //if (oldProc != m_Procents)
                //{
                //    //Console.SetCursorPosition(Console.CursorLeft - oldProc.ToString().Length, Console.CursorTop);
                //    Console.WriteLine(m_Procents);
                    
                //}
                m_ProceccedQueue.AddPiece(new FilePiece(filePiece.Id, ProcessBlock(filePiece.Content)));
            }

        }

        protected abstract byte[] ProcessBlock(byte[] bytesToPack);
    }

    /// <summary>
    /// упаковка
    /// </summary>
    public class PackPieces : ProcessPackingAbstract
    {
        public PackPieces(IEnumerable<FilePiece> mInQueue, IAddablePieces mProceccedQueue) : base(mInQueue, mProceccedQueue)
        {
        }

        protected override byte[] ProcessBlock(byte[] bytesToPack)
        {
            using (var memoryStream = new MemoryStream(bytesToPack.Length))
            {
                using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    gZipStream.Write(bytesToPack, 0, bytesToPack.Length);
                }
                return memoryStream.ToArray();
            }
        }
    }

    /// <summary>
    /// распаковка
    /// </summary>
    public class UnpackPieces : ProcessPackingAbstract
    {
        public UnpackPieces(IEnumerable<FilePiece> mInQueue, IAddablePieces mProceccedQueue) : base(mInQueue, mProceccedQueue)
        {
        }

        protected override byte[] ProcessBlock(byte[] gzip)
        {
            var len = AppPropertiesSingle.GetInstance().m_BufferSize;
            using (var gZipStream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                using (var memStream = new MemoryStream())
                {
                    //todo необходимо при упаковке сохранять список размеров распакованных блоков, и тут их считывать заранее и юзать
                    // возможно будет вообще сохранение моих объектов, в которых есть битовое поле, размер и номер блока
                    //http://www.cyberforum.ru/csharp-beginners/thread1722183.html
                    //var buffertest = new byte[len];
                    //int nRead = memStream.Read(buffertest, 0, buffertest.Length);
                    var buffer = new byte[len];
                    int countBytes = gZipStream.Read(buffer, 0, len);
                    //var new_data = buffer.TakeWhile((v, index) => buffer.Skip(index).Any(w => w != 0x00)).ToArray();
                    //memStream.Write(new_data, 0, new_data.Length);
                    memStream.Write(buffer, 0, countBytes);
                    return memStream.ToArray();
                }
            }
        }
    }
}
