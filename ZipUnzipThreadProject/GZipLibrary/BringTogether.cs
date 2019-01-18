using System.Collections.Generic;
using System.IO;
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
        private string m_PathToSave;

        public event NotifyProgressHandler NotifyProgress;

        public void Collect(List<FilePiece> mQueue, string mPathToSave)
        {
            m_PathToSave = mPathToSave;
            
            NotifyProgress?.Invoke("Читаю свободную оперативку...");
            long freeMemory = FreeRamMemory.GetFreeRamMemoryMb()*1024*1024;

            // соберем все файлы вместе
            NotifyProgress?.Invoke("Соберем все из потоков вместе...0");
            long BUFFER_SIZE = freeMemory / 4;
            if (BUFFER_SIZE > int.MaxValue)
            {
                BUFFER_SIZE = int.MaxValue-1;
            }

            using (var outFile = new FileStream(m_PathToSave, FileMode.Append, FileAccess.Write))
            {
                int fileCount = 0;
                foreach (var threadPath in mQueue)
                {
                    fileCount++;
                    NotifyProgress?.Invoke("Соберем все из потоков вместе..." + fileCount * 100 / mQueue.Count);
                    long counter = 0;
                    using (var reader = new BinaryReader(new FileStream(threadPath.FilePath, FileMode.Open, FileAccess.Read)))
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
                    File.Delete(threadPath.FilePath);
                }
            }
        }
    }
}
