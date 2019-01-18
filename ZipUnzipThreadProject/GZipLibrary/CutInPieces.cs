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
    /// разрежем файлик для архивации
    /// </summary>
    public class CutInPiecesNormal : ICutting
    {
        private List<FilePiece> m_ThreadPieceList;

        private readonly ThreadSafeList<long> m_ProgressList = new ThreadSafeList<long>();

        private long m_StreamLength;
        private string m_FilePath;

        public void Cut(string mFilePath, List<FilePiece> mQueeue)
        {
            m_ThreadPieceList = mQueeue;
            m_FilePath = mFilePath;
            // разрежем файл на части, чтобы распараллелить процесс разрезания на кусочки
            var app = AppPropertiesSingle.GetInstance();
            var prCount = app.ProcessorCount;

            var info = new FileInfo(mFilePath);
            var fileLength = info.Length;
            m_StreamLength = long.Parse((fileLength / prCount).ToString()) + 100;

            // параллелим процесс
            int pathsCount = prCount;
            for (int i = 0; i < prCount; i++)
            {
                m_ThreadPieceList.Add(new FilePiece(""));
                m_ProgressList.Add(1);
            }
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
        }

        public event NotifyProgressHandler NotifyProgress;

        private void CutPath(object o)
        {
            // индекс потока
            int index = (int)o;

            // файл, чтобы складывать все с потока
            var filePath = AppPropertiesSingle.GetInstance().TempPath;
            var tempFileName = index + Path.GetRandomFileName();
            filePath = Path.Combine(filePath, tempFileName);
            
            // позиция курсора и конец отрезка в зависимости от идекса потока
            long cursorPos = m_StreamLength * index;
            long endPos = cursorPos + m_StreamLength;
            using (var reader = new BinaryReader(new FileStream(m_FilePath, FileMode.Open, FileAccess.Read)))
            {
                long BUFFER_SIZE = (long)AppPropertiesSingle.GetInstance().m_BufferSize;

                // для расчета прогресса
                long counter = 0;
                long oldPs = 0;

                // создаем файлик для записи
                using (var bw = new BinaryWriter(new FileStream(filePath, FileMode.Create)))
                {
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
                        var arBytes = reader.ReadBytes((int)bufferSize);

                        // заархивируем считанный кусочек
                        var compBytes = ProcessPacking.ProcessArchive(arBytes);
                        bw.Write(compBytes);
                        counter++;
                    }
                }
                m_ThreadPieceList[index] = new FilePiece(filePath);
            }
        }
    }

    /// <summary>
    /// разрежем файлик для разархивации
    /// </summary>
    public class CutInPiecesCompressed : ICutting
    {
        private List<List<long>> m_AnchorListThread;
        private List<FilePiece> m_ThreadPieceList;
        private string m_FilePath;

        public event NotifyProgressHandler NotifyProgress;
        private readonly ThreadSafeList<long> m_ProgressList = new ThreadSafeList<long>();

        public void Cut(string mFilePath, List<FilePiece> mQueeue)
        {
            m_ThreadPieceList = mQueeue;
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
                m_ThreadPieceList.Add(new FilePiece(""));
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
        }

        private void CutPath(object o)
        {
            // индекс потока
            int index = (int)o;
            var inList = m_AnchorListThread[index];

            // файл, чтобы складывать все с потока
            var filePath = AppPropertiesSingle.GetInstance().TempPath;
            var tempFileName = index + Path.GetRandomFileName();
            filePath = Path.Combine(filePath, tempFileName);
            
            using (var reader = new FileStream(m_FilePath, FileMode.Open, FileAccess.Read))
            {
                // для расчета прогресса
                long counter = 0;
                long oldPs = 0;

                // создаем файлик для записи
                using (var bw = new BinaryWriter(new FileStream(filePath, FileMode.Create)))
                {
                    foreach (var anchor in inList)
                    {
                        // считаем порцию байт (найдем размер порции и саму её по якорям)
                        reader.Seek(anchor, SeekOrigin.Begin);
                        var buffer = new byte[8];
                        reader.Read(buffer, 0, 8);
                        var compressedBlockLength = BitConverter.ToInt32(buffer, 4);
                        var comressedBytes = new byte[compressedBlockLength + 1];
                        buffer.CopyTo(comressedBytes, 0);
                        reader.Read(comressedBytes, 8, compressedBlockLength - 8);
                        var blockSize = BitConverter.ToInt32(comressedBytes, compressedBlockLength - 4);
                        
                        // разархивируем файл
                        var uncompressedBytes = ProcessUnPacking.ProcessArchive(comressedBytes, blockSize);

                        // запишем во временный файл
                        bw.Write(uncompressedBytes);

                        // расчет прогресса
                        counter++;
                        long ps = counter * 100 / inList.Count;
                        if (ps != oldPs)
                        {
                            m_ProgressList[index] = ps;
                        }
                    }
                    m_ThreadPieceList[index] = new FilePiece(filePath);
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
                    var compressedBytes = ProcessPacking.ProcessArchive(arBytes);
                    mQueeue.Add(new FilePiece(counter, compressedBytes));
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
                    var decompressedBytes = ProcessUnPacking.ProcessArchive(comressedBytes, blockSize);
                    mQueeue.Add(new FilePiece(counter, decompressedBytes));
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
