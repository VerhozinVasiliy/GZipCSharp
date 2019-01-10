using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GZipLibrary;

namespace ZipUnzipThreadProject
{
    /// <summary>
    /// выбираем стратегию в зависимсоти от команды и параметров
    /// </summary>
    public class ChooseStrategy
    {
        private readonly CommamdsEnum m_Command;

        public ChooseStrategy(CommamdsEnum mCommand)
        {
            m_Command = mCommand;
        }

        public ICutting CutFile { get; private set; }
        public IArchiveProcessing ArchiveProcess { get; private set; }
        public ICollecting Collecting { get; private set; }

        public void Choose()
        {
            var appProp = AppPropertiesSingle.GetInstance();
            if (appProp.IsBigFile)
            {
                Collecting = new BringTogetherMulty();
            }
            else
            {
                Collecting = new BringTogether();
            }

            switch (m_Command)
            {
                case CommamdsEnum.Compress:
                    if (appProp.IsBigFile)
                    {
                        CutFile = new CutInPiecesNormal();
                    }
                    else
                    {
                        CutFile = new CutInPiecesNormalOneThread();
                    }
                    ArchiveProcess = new ProcessPacking();
                    break;
                case CommamdsEnum.Decompress:
                    if (appProp.IsBigFile)
                    {
                        CutFile = new CutInPiecesCompressed();
                    }
                    else
                    {
                        CutFile = new CutInPiecesCompressedOneThread();
                    }
                    ArchiveProcess = new ProcessUnPacking();
                    break;
            }
        }
    }
}
