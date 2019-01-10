using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace GZipLibrary
{
    public interface IArchiveProcessing
    {
        void ProcessArchive(List<FilePiece> mInQueue, List<FilePiece> mProceccedQueue);
        event NotifyProgressHandler NotifyProgress;
    }

    /// <summary>
    /// упаковка/распаковка частей
    /// </summary>
    public class ProcessPacking : IArchiveProcessing
    {
        private List<List<FilePiece>> m_BytesListThread;
        private readonly List<List<FilePiece>> m_CompressedBytesListThread = new List<List<FilePiece>>();

        public event NotifyProgressHandler NotifyProgress;
        private readonly ThreadSafeList<long> m_ProgressList = new ThreadSafeList<long>();

        public void ProcessArchive(List<FilePiece> mInQueue, List<FilePiece> mProceccedQueue)
        {
            // разделим список байтов на части, чтобы распараллелить процесс разрезания на кусочки
            var prop = AppPropertiesSingle.GetInstance();
            var elementsEachThread = mInQueue.Count / prop.ProcessorCount;

            if (elementsEachThread <= 0)
            {
                m_BytesListThread = new List<List<FilePiece>> { mInQueue };
            }
            else
            {
                m_BytesListThread = mInQueue
                    .Select((x, i) => new { Index = i, Value = x })
                    .GroupBy(x => x.Index / elementsEachThread)
                    .Select(x => x.Select(v => v.Value).ToList())
                    .ToList();
            }


            // параллелим процесс
            foreach (var unused in m_BytesListThread)
            {
                m_CompressedBytesListThread.Add(new List<FilePiece>());
                m_ProgressList.Add(1);
            }
            int pathsCount = m_BytesListThread.Count;
            var threadList = new List<Thread>();
            for (int i = 0; i < pathsCount; i++)
            {
                threadList.Add(new Thread(ProcessBlocks));
                threadList[i].IsBackground = true;
                threadList[i].Start(i);
            }
            while (threadList.Any(w => w.IsAlive))
            {
                NotifyProgress?.Invoke(PercentageCalculate.GetPercentAverage(m_ProgressList).ToString());
                Thread.Sleep(100);
            }
            foreach (var thread in threadList)
            {
                thread.Join();
            }

            var pieceNumber = 0;
            long oldPs = 0;
            long countOfElements = m_CompressedBytesListThread.Select(s => s.Count).Sum();
            //складываем все кусочки в кучу
            foreach (var pieceListFromThread in m_CompressedBytesListThread)
            {
                foreach (var filePiece in pieceListFromThread)
                {
                    //mProceccedQueue.Add(new FilePiece(pieceNumber, filePiece.GetContent(), filePiece.BlockSize, 0));
                    mProceccedQueue.Add(filePiece);
                    pieceNumber++;
                    long ps = pieceNumber * 100 / countOfElements;
                    if (ps != oldPs)
                    {
                        oldPs = ps;
                        NotifyProgress?.Invoke("Собираем кусочки с потоков " + ps);
                    }
                }
            }
        }

        private void ProcessBlocks(object o)
        {
            int index = (int)o;
            var outList = m_CompressedBytesListThread[index];
            var inList = m_BytesListThread[index];
            long pieceId = 0;
            int oldPs = 0;
            foreach (var filePiece in inList)
            {
                using (var memoryStream = new MemoryStream(filePiece.BlockSize))
                {
                    using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                    {
                        gZipStream.Write(filePiece.GetContent(), 0, filePiece.BlockSize);
                    }

                    var compressedData = memoryStream.ToArray();
                    //добавить длину блока
                    var compressedLength = compressedData.Length;
                    var bitLength = BitConverter.GetBytes(compressedLength);
                    bitLength.CopyTo(compressedData, 4);
                    outList.Add(new FilePiece(pieceId, compressedData, compressedData.Length, 0));
                    pieceId++;
                    long ps = pieceId * 100 / inList.Count;
                    if (ps != oldPs)
                    {
                        m_ProgressList[index] = ps;
                    }
                }
                filePiece.CleanContent();
            }
        }
    }

    public class ProcessUnPacking : IArchiveProcessing
    {
        private List<List<FilePiece>> m_BytesListThread;
        private readonly List<List<FilePiece>> m_DeCompressedBytesListThread = new List<List<FilePiece>>();

        public event NotifyProgressHandler NotifyProgress;
        private readonly ThreadSafeList<long> m_ProgressList = new ThreadSafeList<long>();

        public void ProcessArchive(List<FilePiece> mInQueue, List<FilePiece> mProceccedQueue)
        {
            // разделим список байтов на части, чтобы распараллелить процесс разрезания на кусочки
            var prop = AppPropertiesSingle.GetInstance();
            var elementsEachThread = mInQueue.Count / prop.ProcessorCount;

            if (elementsEachThread <= 0)
            {
                m_BytesListThread = new List<List<FilePiece>> { mInQueue };
            }
            else
            {
                m_BytesListThread = mInQueue
                    .Select((x, i) => new { Index = i, Value = x })
                    .GroupBy(x => x.Index / elementsEachThread)
                    .Select(x => x.Select(v => v.Value).ToList())
                    .ToList();
            }

            // параллелим процесс
            foreach (var unused in m_BytesListThread)
            {
                m_DeCompressedBytesListThread.Add(new List<FilePiece>());
                m_ProgressList.Add(1);
            }
            int pathsCount = m_BytesListThread.Count;
            var threadList = new List<Thread>();
            for (int i = 0; i < pathsCount; i++)
            {
                threadList.Add(new Thread(ProcessBlocks));
                m_ProgressList.Add(1);
                threadList[i].IsBackground = true;
                threadList[i].Start(i);
            }
            while (threadList.Any(w => w.IsAlive))
            {
                NotifyProgress?.Invoke(PercentageCalculate.GetPercentAverage(m_ProgressList).ToString());
                Thread.Sleep(100);
            }

            foreach (var thread in threadList)
            {
                thread.Join();
            }

            var pieceNumber = 0;
            long oldPs = 0;
            long countOfElements = m_DeCompressedBytesListThread.Select(s => s.Count).Sum();
            //складываем все кусочки в кучу
            foreach (var pieceListFromThread in m_DeCompressedBytesListThread)
            {
                foreach (var filePiece in pieceListFromThread)
                {
                    //mProceccedQueue.Add(new FilePiece(pieceNumber, filePiece.GetContent(), filePiece.BlockSize, 0));
                    mProceccedQueue.Add(filePiece);
                    long ps = pieceNumber * 100 / countOfElements;
                    if (ps != oldPs)
                    {
                        oldPs = ps;
                        NotifyProgress?.Invoke("Собираем кусочки с потоков " + ps);
                    }
                    pieceNumber++;
                }
            }            
        }

        private void ProcessBlocks(object o)
        {
            int index = (int)o;
            var outList = m_DeCompressedBytesListThread[index];
            var inList = m_BytesListThread[index];
            
            long pieceId = 0;
            long oldPs = 0;
            long counter = 0;
            foreach (var filePiece in inList)
            {
                counter++;
                using (var gZipStream = new GZipStream(new MemoryStream(filePiece.GetContent()), CompressionMode.Decompress))
                {
                    using (var memStream = new MemoryStream())
                    {
                        m_ProgressList[index] = pieceId;
                        var buffer = new byte[filePiece.OutSize];
                        int size = gZipStream.Read(buffer, 0, buffer.Length);
                        memStream.Write(buffer, 0, size);
                        var ms = memStream.ToArray();
                        outList.Add(new FilePiece(pieceId, ms, ms.Length, 0));
                        pieceId++;
                    }
                }
                filePiece.CleanContent();
                long ps = counter * 100 / inList.Count;
                if (ps != oldPs)
                {
                    m_ProgressList[index] = ps;
                }
            }
            
        }
    }
}
