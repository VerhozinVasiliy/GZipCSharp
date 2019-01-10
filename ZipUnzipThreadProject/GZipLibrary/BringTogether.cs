using System;
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
        private readonly List<object> m_ConcurrentThreadList = new List<object>();
        private List<List<FilePiece>> m_BytesListThread = new List<List<FilePiece>>();
        private string m_PathToSave;
        private long m_FreeMemoryForThread;

        public event NotifyProgressHandler NotifyProgress;
        private readonly ThreadSafeList<long> m_ProgressList = new ThreadSafeList<long>();

        public void Collect(List<FilePiece> mQueue, string mPathToSave)
        {
            m_PathToSave = mPathToSave;
            File.Delete(m_PathToSave);
            // разделим список байтов на части, чтобы распараллелить процесс разрезания на кусочки
            var prop = AppPropertiesSingle.GetInstance();
            var elementsEachThread = mQueue.Count / prop.ProcessorCount;

            NotifyProgress?.Invoke("Разделяю список по потокам...");

            m_BytesListThread = mQueue
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / elementsEachThread)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();

            NotifyProgress?.Invoke("Читаю свободную оперативку...");
            //узнаем сколько байт у нас есть всего для каждого потока
            m_FreeMemoryForThread = FreeRamMemory.GetFreeRamMemoryMb()*1024*1024/m_BytesListThread.Count;

            // параллелим процесс
            int pathsCount = m_BytesListThread.Count;
            var threadList = new List<Thread>();
            for (int i = 0; i < pathsCount; i++)
            {
                m_ConcurrentThreadList.Add(false);
                m_ProgressList.Add(1);
                threadList.Add(new Thread(CollectingProcess));
                threadList[i].IsBackground = true;
                threadList[i].Start(i);
                Thread.Sleep(100);
                NotifyProgress?.Invoke("Запускаю параллельные потоки..." + (i+1)*100/pathsCount);
            }
            int counter = 1;
            foreach (var thread in threadList)
            {
                thread.Join();
                NotifyProgress?.Invoke("Обработка в потоках..." + ((counter+1)*100/threadList.Count));
                counter++;
            }
        }

        private void CollectingProcess(object o)
        {
            int index = (int)o;
            Monitor.Enter(m_ConcurrentThreadList[index]);

            var inList = m_BytesListThread[index];

            if (index > 0)
            {
                Monitor.Enter(m_ConcurrentThreadList[index - 1]);
                Monitor.Exit(m_ConcurrentThreadList[index - 1]);
            }

            long sum = 0;
            var list = new List<byte[]>();
            var counter = 0;
            long oldPs = 0;
            foreach (var fp in inList)
            {
                counter++;
                long ps = counter * 100 / inList.Count;
                if (ps!=oldPs)
                {
                    oldPs = ps;
                    m_ProgressList[index] = ps;
                }
                sum += fp.BlockSize;
                if (sum < m_FreeMemoryForThread)
                {
                    list.Add(fp.GetContent());
                    if (fp != inList[inList.Count-1])
                    {
                        continue;
                    }
                }
                var allBytes = new byte[list.Select(s=>s.Length).Sum()];
                int bytesIndex = 0;
                foreach (var bytes in list)
                {
                    bytes.CopyTo(allBytes, bytesIndex);
                    bytesIndex += bytes.Length;
                }
                using (var outFile = new FileStream(m_PathToSave, FileMode.Append, FileAccess.Write))
                {
                    outFile.Write(allBytes, 0, allBytes.Length);
                }
                list = new List<byte[]>{fp.GetContent()};
                sum = fp.BlockSize;
                GC.Collect();
            }

            GC.Collect();
            Monitor.Exit(m_ConcurrentThreadList[index]);
        }
    }
}
