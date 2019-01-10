using System;
using System.Diagnostics;

namespace GZipLibrary
{
    /// <summary>
    /// прочитаем сколько памяти свободно в Мб
    /// </summary>
    public static class FreeRamMemory
    {
        public static long GetFreeRamMemoryMb()
        {
            var ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            float freeMemory;
            try
            {
                freeMemory = ramCounter.NextValue();
            }
            catch (PlatformNotSupportedException)
            {
                //Console.WriteLine("Не могу прочитать оперативку, не поддерживается платформой, буду считать что свободной чуть меньше 1Гб");
                freeMemory = 1000;
            }

            // докинем маленько, тобы винда шевелилась (20%)
            freeMemory -= freeMemory / 5;

            return (long)freeMemory;
        }
    }
}
