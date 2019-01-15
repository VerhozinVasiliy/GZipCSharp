using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace GZipLibrary
{
    public interface ICollecting
    {
        void Collect(List<FilePiece> mQueue, string mPathToSave);
        event NotifyProgressHandler NotifyProgress;
    }

    /// <summary>
    /// собрать воедино
    /// </summary>
    public class BringTogether : ICollecting
    {
        public event NotifyProgressHandler NotifyProgress;
        public void Collect(List<FilePiece> mQueue, string mPathToSave)
        {
            long oldPs = 0;
            using (var outFile = new FileStream(mPathToSave, FileMode.Create))
            {
                int counter = 0;
                foreach (var queueOfPart in mQueue)
                {
                    var buf = queueOfPart.GetContent();
                    queueOfPart.CleanContent();
                    outFile.Write(buf, 0, buf.Length);
                    counter++;
                    long ps = counter * 100 / mQueue.Count;
                    if (ps!=oldPs)
                    {
                        oldPs = ps;
                        NotifyProgress?.Invoke(ps.ToString());
                    }
                }
            }
        }
    }

    /// <summary>
    /// версия для нескольких потоков
    /// </summary>
    public class BringTogetherMulty : ICollecting
    {
        //private readonly List<object> m_ConcurrentThreadList = new List<object>();
        private List<List<FilePiece>> m_BytesListThread = new List<List<FilePiece>>();
        private string m_PathToSave;
        //private long m_FreeMemoryForThread;

        public event NotifyProgressHandler NotifyProgress;
        private readonly ThreadSafeList<long> m_ProgressList = new ThreadSafeList<long>();

        // пути до файлов, создаваемых в потоках
        private readonly List<string> m_ListThreadPaths = new List<string>();

        public void Collect(List<FilePiece> mQueue, string mPathToSave)
        {
            m_PathToSave = mPathToSave;
            File.Delete(m_PathToSave);

            var prop = AppPropertiesSingle.GetInstance();
            var elementsEachThread = mQueue.Count / prop.ProcessorCount;

            NotifyProgress?.Invoke("Разделяю список по потокам...");

            m_BytesListThread = mQueue
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / elementsEachThread)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();

            NotifyProgress?.Invoke("Читаю свободную оперативку...");

            // параллелим процесс
            int pathsCount = m_BytesListThread.Count;
            var threadList = new List<Thread>();
            NotifyProgress?.Invoke("Запускаю параллельные потоки...");
            for (int i = 0; i < pathsCount; i++)
            {
                m_ListThreadPaths.Add("");
                m_ProgressList.Add(1);
                threadList.Add(new Thread(CollectingProcess));
                threadList[i].IsBackground = true;
                threadList[i].Start(i);
                Thread.Sleep(100);
            }
            while (threadList.Any(w => w.IsAlive))
            {
                NotifyProgress?.Invoke("Обработка в потоках..." + PercentageCalculate.GetPercentAverage(m_ProgressList));
                Thread.Sleep(100);
            }
            foreach (var thread in threadList)
            {
                thread.Join();
            }

            NotifyProgress?.Invoke("Узнаем сколько у нас свободной оперативки...");
            long freeMemory = FreeRamMemory.GetFreeRamMemoryMb()*1024*1024;

            // соберем все файлы вместе
            NotifyProgress?.Invoke("Соберем все из потоков вместе...0");
            var BUFFER_SIZE = freeMemory / 4;//(long)AppPropertiesSingle.GetInstance().m_BufferSize;

            using (var outFile = new FileStream(m_PathToSave, FileMode.Create, FileAccess.Write))
            {
                int fileCount = 0;
                foreach (var threadPath in m_ListThreadPaths)
                {
                    long counter = 0;
                    using (var reader = new BinaryReader(new FileStream(threadPath, FileMode.Open, FileAccess.Read)))
                    {
                        while (reader.BaseStream.Position < reader.BaseStream.Length)
                        {
                            reader.BaseStream.Seek(counter * BUFFER_SIZE, SeekOrigin.Begin);
                            var bufferSize = BUFFER_SIZE;
                            if (reader.BaseStream.Length - reader.BaseStream.Position <= BUFFER_SIZE)
                            {
                                bufferSize = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
                            }
                            var arBytes = reader.ReadBytes((int)bufferSize);
                            outFile.Write(arBytes, 0, arBytes.Length);
                            counter++;
                        }
                    }
                    fileCount++;
                    File.Delete(threadPath);
                    NotifyProgress?.Invoke("Соберем все из потоков вместе..." + fileCount * 100 / m_ListThreadPaths.Count);
                }

            }

        }

        private void CollectingProcess(object o)
        {
            int index = (int)o;

            var inList = m_BytesListThread[index];
            var filePath = AppPropertiesSingle.GetInstance().TempPath;
            var tempFileName = index + Path.GetRandomFileName();
            filePath = Path.Combine(filePath, tempFileName);
            m_ListThreadPaths[index] = filePath;

            using (var bw = new BinaryWriter(new FileStream(filePath, FileMode.Create)))
            {
                int oldPs = 0;
                long pieceId = 0;
                foreach (var filePiece in inList)
                {
                    bw.Write(filePiece.GetContent());
                    filePiece.CleanContent();
                    pieceId++;
                    long ps = pieceId * 100 / inList.Count;
                    if (ps != oldPs)
                    {
                        m_ProgressList[index] = ps;
                    }
                }
            }
        }
    }
}
