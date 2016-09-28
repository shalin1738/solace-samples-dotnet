using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SolaceSystems.Solclient.Examples.Messaging
{
    class HighResolutionCounter
    {
        [DllImport("KERNEL32")]
        private static extern bool QueryPerformanceCounter(
            out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        private static long m_frequency;
        private static Decimal m_toMilliseconds = new Decimal(1.0e3);
        private static Decimal m_toMicroseconds = new Decimal(1.0e6);

        static HighResolutionCounter()
        {
            if (QueryPerformanceFrequency(out m_frequency) == false)
            {
                // Frequency not supported
                throw new Win32Exception();
            }
        }

        public static Int64 GetTickCount()
        {
            Int64 ticks;
            QueryPerformanceCounter(out ticks);
            return ticks;
        }

        public static double TotalMilliseconds(Int64 ticks)
        {
            return ((((double)ticks) * (double)m_toMilliseconds) / (double)m_frequency);
        }

        public static double TotalMicroSeconds(Int64 ticks)
        {
            return ((((double)ticks) * (double)m_toMicroseconds) / (double)m_frequency);
        }
    }
}
