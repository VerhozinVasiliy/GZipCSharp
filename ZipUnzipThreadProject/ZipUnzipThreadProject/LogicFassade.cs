
namespace ZipUnzipThreadProject
{
    public class LogicFassade
    {
        private readonly ICutting CutFile;
        private readonly IArchiveProcessing ArchiveProcessing;
        private readonly QueueOfParts m_InQueue = new QueueOfParts();
        private readonly QueueOfParts m_OutQueue = new QueueOfParts();

        public LogicFassade(ICutting cutFile, IArchiveProcessing archiveProcessing)
        {
            CutFile = cutFile;
            ArchiveProcessing = archiveProcessing;
        }

        public void CutInPieces()
        {
            var properties = AppPropertiesSingle.GetInstance();
            CutFile.Cut(properties.InFilePath, m_InQueue);
        }

        public void ArchiveProcess()
        {
            ArchiveProcessing.ProcessArchive(m_InQueue.PieceList, m_OutQueue);
        }

        public void BringUpFile()
        {
            var properties = AppPropertiesSingle.GetInstance();
            var bringTogether = new BringTogether(m_OutQueue.PieceList, properties.OutFilePath);
            bringTogether.Collect();
        }
    }
}
