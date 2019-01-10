using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace GZipLibrary
{
    public delegate void NotifyProgressHandler(string message);

    public interface ICutting
    {
        void Cut(string mFilePath, List<FilePiece> mQueeue);
        event NotifyProgressHandler NotifyProgress;
    }

    /// <summary>
    /// разрежем файлик для разархивации
    /// </summary>
    public class CutInPiecesCompressed : ICutting
    {
        private List<List<long>> m_AnchorListThread;
        private readonly List<List<FilePiece>> m_ThreadPieceList = new List<List<FilePiece>>();
        private string m_FilePath;

        public event NotifyProgressHandler NotifyProgress;
        private readonly ThreadSafeList<long> m_ProgressList = new ThreadSafeList<long>();

        public void Cut(string mFilePath, List<FilePiece> mQueeue)
        {
            m_FilePath = mFilePath;
            // найдем все якоря, чтобы потом считывать инфу в потоках
            var anchorList = new List<long>();
            using (var reader = new FileStream(mFilePath, FileMode.Open, FileAccess.Read))
            {
                while (reader.Position < reader.Length)
                {
                    long anchor = reader.Position;
                    var buffer = new byte[8];
                    reader.Read(buffer, 0, 8);
                    var compressedBlockLength = BitConverter.ToInt32(buffer, 4);
                    reader.Position += compressedBlockLength - 8;
                    anchorList.Add(anchor);
                }
            }

            var prop = AppPropertiesSingle.GetInstance();
            var elementsEachThread = anchorList.Count / prop.ProcessorCount;

            // распределим на разные списки для распараллеливания
            m_AnchorListThread = anchorList
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / elementsEachThread)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();

            // параллелим процесс
            foreach (var unused in m_AnchorListThread)
            {
                m_ThreadPieceList.Add(new List<FilePiece>());
                m_ProgressList.Add(1);
            }
            int pathsCount = m_AnchorListThread.Count;
            var threadList = new List<Thread>();
            for (int i = 0; i < pathsCount; i++)
            {
                threadList.Add(new Thread(CutPath));
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

            //складываем все кусочки в кучу
            foreach (var pieceListFromThread in m_ThreadPieceList)
            {
                foreach (var filePiece in pieceListFromThread)
                {
                    mQueeue.Add(new FilePiece(pieceNumber, filePiece.GetContent(), filePiece.BlockSize, filePiece.OutSize));
                    pieceNumber++;
                }
            }
        }

        private void CutPath(object o)
        {
            int index = (int)o;
            var outList = m_ThreadPieceList[index];
            var inList = m_AnchorListThread[index];

            using (var reader = new FileStream(m_FilePath, FileMode.Open, FileAccess.Read))
            {
                long counter = 0;
                long oldPs = 0;
                foreach (var anchor in inList)
                {
                    reader.Seek(anchor, SeekOrigin.Begin);
                    var buffer = new byte[8];
                    reader.Read(buffer, 0, 8);
                    var compressedBlockLength = BitConverter.ToInt32(buffer, 4);
                    var comressedBytes = new byte[compressedBlockLength + 1];
                    buffer.CopyTo(comressedBytes, 0);
                    reader.Read(comressedBytes, 8, compressedBlockLength - 8);
                    var blockSize = BitConverter.ToInt32(comressedBytes, compressedBlockLength - 4);
                    outList.Add(new FilePiece(counter, comressedBytes, comressedBytes.Length, blockSize));
                    counter++;
                    long ps = counter * 100 / inList.Count;
                    if (ps != oldPs)
                    {
                        m_ProgressList[index] = ps;
                    }
                }
            }
        }
    }

    /// <summary>
    /// разрежем файлик для архивации
    /// </summary>
    public class CutInPiecesNormal : ICutting
    {
        private readonly List<List<FilePiece>> m_ThreadPieceList = new List<List<FilePiece>>();

        private readonly ThreadSafeList<long> m_ProgressList = new ThreadSafeList<long>();

        private long m_StreamLength;
        private string m_FilePath;

        public void Cut(string mFilePath, List<FilePiece> mQueeue)
        {
            m_FilePath = mFilePath;
            // разрежем файл на части, чтобы распараллелить процесс разрезания на кусочки
            var app = AppPropertiesSingle.GetInstance();
            var prCount = app.ProcessorCount;

            var info = new FileInfo(mFilePath);
            var fileLength = info.Length;
            m_StreamLength = int.Parse((fileLength / prCount).ToString()) + 100;

            // параллелим процесс
            int pathsCount = prCount;
            for (int i = 0; i < prCount; i++)
            {
                m_ThreadPieceList.Add(new List<FilePiece>());
                m_ProgressList.Add(1);
            }
            var threadList = new List<Thread>();
            for (int i = 0; i < pathsCount; i++)
            {
                threadList.Add(new Thread(CutPath));   
                threadList[i].IsBackground = true;
                threadList[i].Start(i);
            }
            while (threadList.Any(w=>w.IsAlive))
            {
                NotifyProgress?.Invoke(PercentageCalculate.GetPercentAverage(m_ProgressList).ToString());
                Thread.Sleep(100);
            }
            foreach (var thread in threadList)
            {
                thread.Join();
            }

            var pieceNumber = 0;

            //складываем все кусочки в кучу
            foreach (var pieceListFromThread in m_ThreadPieceList)
            {
                foreach (var filePiece in pieceListFromThread)
                {
                    mQueeue.Add(new FilePiece(pieceNumber, filePiece.GetContent(), filePiece.BlockSize, filePiece.OutSize));
                    pieceNumber++;
                }
            }
        }

        public event NotifyProgressHandler NotifyProgress;

        private void CutPath(object o)
        {
            int index = (int)o;
            var threadPieceList = m_ThreadPieceList[index];
            long cursorPos = m_StreamLength * index;
            long endPos = cursorPos + m_StreamLength;
            using (var reader = new BinaryReader(new FileStream(m_FilePath, FileMode.Open, FileAccess.Read)))
            {
                int counter = 0;
                var BUFFER_SIZE = AppPropertiesSingle.GetInstance().m_BufferSize;
                long oldPs = 0;
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    long offset = cursorPos + counter * BUFFER_SIZE;
                    var ps = (offset - cursorPos) * 100 / (endPos - cursorPos);
                    if (ps != oldPs)
                    {
                        m_ProgressList[index] = ps;
                    }
                    if (offset > endPos)
                    {
                        break;
                    }
                    reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                    var bufferSize = BUFFER_SIZE;
                    var nextoffset = cursorPos + (counter + 1) * BUFFER_SIZE;
                    if (nextoffset > endPos)
                    {
                        bufferSize = (int)(endPos - offset);
                    }
                    var arBytes = reader.ReadBytes(bufferSize);
                    threadPieceList.Add(new FilePiece(counter, arBytes, arBytes.Length, 0));
                    counter++;
                }
            }
        }
    }

    public class CutInPiecesNormalOneThread : ICutting
    {
        public void Cut(string mFilePath, List<FilePiece> mQueeue)
        {
            using (var reader = new BinaryReader(new FileStream(mFilePath, FileMode.Open, FileAccess.Read)))
            {
                int counter = 0;
                var BUFFER_SIZE = AppPropertiesSingle.GetInstance().m_BufferSize;
                long oldPs = 0;
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    reader.BaseStream.Seek(counter * BUFFER_SIZE, SeekOrigin.Begin);
                    var bufferSize = BUFFER_SIZE;
                    if (reader.BaseStream.Length - reader.BaseStream.Position <= BUFFER_SIZE)
                    {
                        bufferSize = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
                    }
                    var arBytes = reader.ReadBytes(bufferSize);
                    mQueeue.Add(new FilePiece(counter, arBytes, arBytes.Length, 0));
                    counter++;
                    long ps = reader.BaseStream.Position * 100 / reader.BaseStream.Length;
                    if (ps != oldPs)
                    {
                        NotifyProgress?.Invoke(ps.ToString());
                    }
                    oldPs = ps;
                }
            }
        }

        public event NotifyProgressHandler NotifyProgress;
    }

    public class CutInPiecesCompressedOneThread : ICutting
    {
        public void Cut(string mFilePath, List<FilePiece> mQueeue)
        {
            using (var reader = new FileStream(mFilePath, FileMode.Open, FileAccess.Read))
            {
                var counter = 0;
                long oldPs = 0;
                while (reader.Position < reader.Length)
                {
                    var buffer = new byte[8];
                    //читаем заголовок файла
                    reader.Read(buffer, 0, 8);
                    //выбираем из прочитанного размер блока
                    var compressedBlockLength = BitConverter.ToInt32(buffer, 4);
                    //Console.WriteLine(compressedBlockLength);
                    var comressedBytes = new byte[compressedBlockLength + 1];
                    buffer.CopyTo(comressedBytes, 0);
                    reader.Read(comressedBytes, 8, compressedBlockLength - 8);
                    var blockSize = BitConverter.ToInt32(comressedBytes, compressedBlockLength - 4);
                    mQueeue.Add(new FilePiece(counter, comressedBytes, comressedBytes.Length, blockSize));
                    counter++;
                    long ps = reader.Position * 100 / reader.Length;
                    if (ps != oldPs)
                    {
                        oldPs = ps;
                        NotifyProgress?.Invoke(ps.ToString());
                    }
                }
            }
        }

        public event NotifyProgressHandler NotifyProgress;
    }
}
