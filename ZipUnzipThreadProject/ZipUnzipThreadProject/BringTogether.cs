using System.Collections.Generic;
using System.IO;

namespace ZipUnzipThreadProject
{
    /// <summary>
    /// собрать воедино
    /// </summary>
    public class BringTogether
    {
        private readonly IEnumerable<FilePiece> m_Queue;

        private readonly string m_PathToSave;

        public BringTogether(IEnumerable<FilePiece> mQueue, string mPathToSave)
        {
            m_Queue = mQueue;
            m_PathToSave = mPathToSave;
        }

        public void Collect()
        {
            using (var outFile = new FileStream(m_PathToSave, FileMode.Create))
            {
                foreach (var queueOfPart in m_Queue)
                {
                    var buf = queueOfPart.Content;
                    outFile.Write(buf, 0, buf.Length);
                }
            }
        }
    }
}
