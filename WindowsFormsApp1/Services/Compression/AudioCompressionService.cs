using System;
using AudioCompressor.Models;

namespace AudioCompressor.Services
{
    /// <summary>
    /// Snapshot of compression progress, reported live during execution.
    /// Consumed by the UI to drive the progress bar and the performance charts
    /// (Requirement 7: real-time monitoring).
    /// </summary>
    public sealed class CompressionProgress
    {
        public int Percent;             // 0..100
        public long InputProcessed;     // bytes of input consumed so far
        public long OutputSize;         // bytes of output produced so far
        public double ElapsedSeconds;   // wall-clock time since compression started
        public double Ratio;            // input/output so far ( >1 means shrinking )
        public double SpeedMBPerSec;    // processing throughput
    }

    public interface IAudioCompressionService
    {
        /// <summary>Simple one-shot compression (no progress reporting).</summary>
        byte[] Compress(byte[] input, CompressionSettings settings);

        /// <summary>
        /// Compression with live progress reporting and cooperative cancellation.
        /// Returns <c>null</c> if <paramref name="isCancelled"/> becomes true.
        /// </summary>
        /// <param name="report">Called periodically with a progress snapshot. May be null.</param>
        /// <param name="isCancelled">Polled periodically; return true to abort. May be null.</param>
        byte[] Compress(byte[] input, CompressionSettings settings,
                        Action<CompressionProgress> report, Func<bool> isCancelled);

        byte[] Decompress(byte[] input, CompressionSettings settings);
    }
}
