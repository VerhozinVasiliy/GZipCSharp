using System.Collections.Generic;
using System.Threading;

namespace GZipLibrary
{
    public enum CommamdsEnum
    {
        Help,
        Compress,
        Decompress,
        Exit
    }

    /// <summary>
    /// синглтон с основными параметрами архиватора
    /// </summary>
    public class AppPropertiesSingle
    {
        private static AppPropertiesSingle m_Instance;
        private static readonly object m_LockObject = new object();

        private AppPropertiesSingle()
        {
            ParamsDictionary = new Dictionary<string, CommamdsEnum>
            {
                { "help", CommamdsEnum.Help },
                { "compress", CommamdsEnum.Compress },
                { "decompress", CommamdsEnum.Decompress },
                { "exit", CommamdsEnum.Exit }
            };
        }

        public static AppPropertiesSingle GetInstance()
        {
            if (m_Instance != null)
            {
                return m_Instance;
            }

            Monitor.Enter(m_LockObject);
            if (m_Instance == null)
            {
                var temp = new AppPropertiesSingle();
                Interlocked.CompareExchange(ref m_Instance, temp, null);
            }
            Monitor.Exit(m_LockObject);

            return m_Instance;
        }

        public string InFilePath { get; private set; }
        public void SetInFilePath(string val)
        {
            InFilePath = val;
        }
        public string OutFilePath { get; private set; }
        public void SetOutFilePath(string val)
        {
            OutFilePath = val;
        }

        public int m_BufferSize { get; private set; }
        public void SetBufferSize(int bs)
        {
            m_BufferSize = bs;
        }

        public Dictionary<string, CommamdsEnum> ParamsDictionary { get; }

        public int ProcessorCount { get; private set; }
        public void SetProcessorCount(int val)
        {
            ProcessorCount = val;
        }

        public string TempPath { get; private set; }
        public void SetTempPath(string val)
        {
            TempPath = val;
        }

        public bool IsBigFile { get; private set; }
        public void SetBigFile()
        {
            IsBigFile = true;
        }
    }
}
