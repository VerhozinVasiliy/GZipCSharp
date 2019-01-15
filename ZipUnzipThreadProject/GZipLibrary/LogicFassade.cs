using System;
using System.Collections.Generic;

namespace GZipLibrary
{
    /// <summary>
    /// бизнес-логика архивации/разархивации файла
    /// </summary>
    public class LogicFassade
    {
        private readonly ICutting CutFile;
        private readonly ICollecting Collecting;

        private List<FilePiece> m_Queue = new List<FilePiece>();

        public LogicFassade(ICutting cutFile, ICollecting collecting)
        {
            CutFile = cutFile;
            Collecting = collecting;
        }

        public void CutInPieces()
        {
            var properties = AppPropertiesSingle.GetInstance();
            CutFile.Cut(properties.InFilePath, m_Queue);
            GC.Collect();
        }

        public void BringUpFile()
        {
            var properties = AppPropertiesSingle.GetInstance();
            Collecting.Collect(m_Queue, properties.OutFilePath);
            m_Queue = new List<FilePiece>();
            GC.Collect();
        }
    }
}
