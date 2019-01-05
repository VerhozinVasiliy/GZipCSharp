using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace ZipUnzipThreadProject
{
    /// <summary>
    /// очередь кусочков файла
    /// </summary>
    public class QueueOfParts : IEnumerable, IAddablePieces
    {
        //private static readonly object m_LockObj = new object();
        //private static QueueOfParts m_Instance;
        public QueueOfParts()
        {
            PieceList = new List<FilePiece>();
        }

        //public static QueueOfParts GetInstance()
        //{
        //    if (m_Instance!=null)
        //    {
        //        return m_Instance;
        //    }

        //    Monitor.Enter(m_LockObj);
        //    if (m_Instance == null)
        //    {
        //        var temp = new QueueOfParts();
        //        Interlocked.CompareExchange(ref m_Instance, temp, null);
        //    }
        //    Monitor.Exit(m_LockObj);
        //    return m_Instance;
        //}

        public List<FilePiece> PieceList { get; }

        public void AddPiece(FilePiece piece)
        {
            PieceList.Add(piece);
        }


        public IEnumerator GetEnumerator()
        {
            return PieceList.GetEnumerator();
        }
    }
}
