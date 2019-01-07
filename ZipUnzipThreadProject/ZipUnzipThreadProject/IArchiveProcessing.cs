using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace ZipUnzipThreadProject
{
    public interface IArchiveProcessing
    {
        void ProcessArchive(IEnumerable<FilePiece> mInQueue, IAddablePieces mProceccedQueue);
    }

    /// <summary>
    /// упаковка/распаковка частей
    /// </summary>
    public class ProcessPacking : IArchiveProcessing
    {
        public void ProcessArchive(IEnumerable<FilePiece> mInQueue, IAddablePieces mProceccedQueue)
        {
            var orderedList = mInQueue
                .OrderBy(w => w.Id)
                .ToList();
            //int i = 0;
            //int len = orderedList.Count;
            foreach (var filePiece in orderedList)
            {
                //i++;
                mProceccedQueue.AddPiece(new FilePiece(filePiece.Id, ProcessBlock(filePiece.Content), 0));
            }
        }

        private byte[] ProcessBlock(byte[] bytesToPack)
        {
            using (var memoryStream = new MemoryStream(bytesToPack.Length))
            {
                using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    gZipStream.Write(bytesToPack, 0, bytesToPack.Length);
                }

                var compressedData = memoryStream.ToArray();
                //добавить длину блока
                var compressedLength = compressedData.Length;
                var bitLength = BitConverter.GetBytes(compressedLength);
                bitLength.CopyTo(compressedData, 4);
                return compressedData;
            }
        }
    }

    public class ProcessUnPacking : IArchiveProcessing
    {
        public void ProcessArchive(IEnumerable<FilePiece> mInQueue, IAddablePieces mProceccedQueue)
        {
            var orderedList = mInQueue
                .OrderBy(w => w.Id)
                .ToList();
            //int i = 0;
            //int len = orderedList.Count;
            foreach (var filePiece in orderedList)
            {
                //i++;
                mProceccedQueue.AddPiece(new FilePiece(filePiece.Id, ProcessBlock(filePiece), 0));
            }
        }

        private byte[] ProcessBlock(FilePiece filePiece)
        {
            var len = AppPropertiesSingle.GetInstance().m_BufferSize;
            using (var gZipStream = new GZipStream(new MemoryStream(filePiece.Content), CompressionMode.Decompress))
            {
                using (var memStream = new MemoryStream())
                {
                    var buffer = new byte[filePiece.OutSize];
                    gZipStream.Read(buffer, 0, buffer.Length);
                    memStream.Write(buffer, 0, buffer.Length);
                    return memStream.ToArray();
                }
            }
        }
    }
}
