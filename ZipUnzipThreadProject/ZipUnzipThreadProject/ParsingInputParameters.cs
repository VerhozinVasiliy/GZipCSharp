using System.Collections.Generic;
using GZipLibrary;

namespace ZipUnzipThreadProject
{
    /// <summary>
    /// распарсим входные параметры
    /// </summary>
    public class ParsingInputParameters
    {
        private readonly string[] m_Args;
        private readonly Dictionary<string, CommamdsEnum> m_ParamsDict;
        public CommamdsEnum ChosenCommand { get; private set; } = CommamdsEnum.Help;

        public string InFilePath { get; private set; }
        public string OutFilePath { get; private set; }

        public ParsingInputParameters(string[] mArgs, Dictionary<string, CommamdsEnum> mParamsDict)
        {
            m_Args = mArgs;
            m_ParamsDict = mParamsDict;
        }

        public bool Parse()
        {
            if (m_Args == null || m_Args.Length <= 0)
            {
                return false;
            }

            var command = m_Args[0];

            if (!m_ParamsDict.ContainsKey(command))
            {
                return false;
            }

            ChosenCommand = m_ParamsDict[command];

            if (m_Args.Length < 2)
            {
                return true;
            }

            InFilePath = m_Args[1].Replace("\"", "");

            if (m_Args.Length < 3)
            {
                return false;
            }

            OutFilePath = m_Args[2].Replace("\"", "");

            return true;
        }
    }
}
