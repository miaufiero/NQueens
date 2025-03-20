using System.Management;
using System.Runtime.InteropServices;

namespace NQueen
{
    /// <summary>
    /// Provides extension methods for various utility functions including shuffling, time formatting, 
    /// retrieving core counts, and generating processor core summary strings.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Shuffles an array in place using the Fisher-Yates algorithm.
        /// Time Complexity: O(n)
        /// Space Complexity: O(1)
        /// </summary>
        public static void Shuffle<T>(this T[] array, Random rng)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }

        /// <summary>
        /// Shuffles a list in place using the Fisher-Yates algorithm.
        /// Time Complexity: O(n)
        /// Space Complexity: O(1)
        /// </summary>
        public static void FisherYatesShuffle<T>(this IList<T> list, Random rng)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        /// <summary>
        /// Formats a TimeSpan into a human-readable string.
        /// Time Complexity: O(1)
        /// </summary>
        /// <param name="ts">The TimeSpan to format.</param>
        /// <returns>A formatted string representing the TimeSpan.</returns>
        public static string ToTimeString(this TimeSpan ts)
        {
            if (ts.TotalHours >= 1)
                return ts.ToString(@"hh\:mm\:ss\.fff");
            if (ts.TotalMinutes >= 1)
                return ts.ToString(@"mm\:ss\.fff");
            return ts.ToString(@"ss\.fff");
        }

        /// <summary>
        /// Retrieves the number of physical processor cores available on the system.
        /// Time Complexity: O(1) amortized.
        /// </summary>
        /// <returns>The number of physical cores.</returns>
        public static int GetPhysicalCoreCount()
        {
            int coreCount = Environment.ProcessorCount;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    return GetWindowsPhysicalCores();
                }
                catch
                {
                    return coreCount;
                }
            }
            return coreCount;
        }

        /// <summary>
        /// Retrieves the number of physical processor cores on Windows using WMI.
        /// Time Complexity: O(1)
        /// </summary>
        /// <returns>The number of physical cores, or falls back to the logical processor count.</returns>
        private static int GetWindowsPhysicalCores()
        {
            int coreCount = 0;
            var searcher = new ManagementObjectSearcher("SELECT NumberOfCores FROM Win32_Processor");
            foreach (var obj in searcher.Get())
            {
                coreCount += Convert.ToInt32(obj["NumberOfCores"]);
            }
            return coreCount > 0 ? coreCount : Environment.ProcessorCount;
        }

        /// <summary>
        /// Gets the number of logical processor cores available on the system.
        /// Time Complexity: O(1)
        /// </summary>
        public static int LogicalCores
        {
            get { return Environment.ProcessorCount; }
        }

        /// <summary>
        /// Generates a summary string for the processor core configuration.
        /// If physical cores equal logical cores, only the logical cores are displayed;
        /// otherwise, both physical and logical cores are shown.
        /// Time Complexity: O(1)
        /// </summary>
        /// <returns>A formatted string summarizing the core counts.</returns>
        public static string GetCoreSummary()
        {
            int physical = GetPhysicalCoreCount();
            return physical == LogicalCores
                ? $"Cores (C): {LogicalCores}\n"
                : $"Physical Cores: {physical}\nLogical Cores (C): {LogicalCores}\n";
        }
    }
}