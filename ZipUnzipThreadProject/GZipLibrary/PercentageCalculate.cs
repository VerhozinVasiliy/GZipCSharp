using System.Linq;

namespace GZipLibrary
{
    /// <summary>
    /// подсчет среднего процента выполнения по всем потокам
    /// </summary>
    public static class PercentageCalculate
    {
        public static long GetPercentAverage(ThreadSafeList<long> percentList)
        {
            if (percentList == null || !percentList.Any())
            {
                return 0;
            }

            return //percentList.Min();
                (percentList.Max() + percentList.Min()) / 2;
                //(long) percentList.Average();
        }
    }
}
