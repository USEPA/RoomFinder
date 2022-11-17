using System;
using System.Diagnostics;

namespace OutlookRoomFinder.Core.Services
{
    public class MeasurePerformance : IDisposable
    {
        public static bool IsDisposable { get; internal set; }
        private Stopwatch PerformanceTimer { get; set; }

        public MeasurePerformance()
        {
            IsDisposable = true;
            PerformanceTimer = new Stopwatch();
            PerformanceTimer.Start();
        }

        public string StopTimer(string operation)
        {
            if (!IsDisposable)
            {
                return "Not Running";
            }
            PerformanceTimer.Stop();
            return $"{(string.IsNullOrEmpty(operation) ? "not-set" : operation)} took {PerformanceTimer.ElapsedMilliseconds} ms.";
        }

        protected virtual void Dispose(bool disposeIt)
        {
            if (disposeIt && IsDisposable)
            {
                PerformanceTimer.Stop();
                PerformanceTimer = null;
            }
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }
    }
}