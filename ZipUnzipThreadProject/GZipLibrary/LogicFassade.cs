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
        private readonly IArchiveProcessing ArchiveProcessing;
        private readonly ICollecting Collecting;

        private List<FilePiece> m_InQueue = new List<FilePiece>();
        private List<FilePiece> m_OutQueue = new List<FilePiece>();

        public LogicFassade(ICutting cutFile, IArchiveProcessing archiveProcessing, ICollecting collecting)
        {
            CutFile = cutFile;
            ArchiveProcessing = archiveProcessing;
            Collecting = collecting;
        }

        public void CutInPieces()
        {
            var properties = AppPropertiesSingle.GetInstance();
            CutFile.Cut(properties.InFilePath, m_InQueue);
            GC.Collect();
        }

        public void ArchiveProcess()
        {
            ArchiveProcessing.ProcessArchive(m_InQueue, m_OutQueue);
            m_InQueue = new List<FilePiece>();
            GC.Collect();
        }

        public void BringUpFile()
        {
            var properties = AppPropertiesSingle.GetInstance();
            Collecting.Collect(m_OutQueue, properties.OutFilePath);
            m_OutQueue = new List<FilePiece>();
            GC.Collect();
        }
    }
}
