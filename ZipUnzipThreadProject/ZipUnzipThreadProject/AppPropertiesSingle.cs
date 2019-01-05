using System.Collections.Generic;
using System.Threading;

namespace ZipUnzipThreadProject
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
            m_BufferSize = 1048576;
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

        public string InFilePath { get; set; }
        public string OutFilePath { get; set; }

        public int m_BufferSize { get; }

        public Dictionary<string, CommamdsEnum> ParamsDictionary { get; }
    }
}
