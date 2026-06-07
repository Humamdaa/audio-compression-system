using System;
using System.Diagnostics;
using AudioCompressor.Models;

namespace AudioCompressor.Services
{
    public class DeltaCompressionService : IAudioCompressionService
    {
        // Simple one-shot overload — delegates to the reporting version.
        public byte[] Compress(byte[] input, CompressionSettings settings)
        {
            return Compress(input, settings, null, null);
        }

        public byte[] Compress(byte[] input, CompressionSettings settings,
                               Action<CompressionProgress> report, Func<bool> isCancelled)
        {
            if (input == null || input.Length == 0)
                return new byte[0];

            int step = 1;
            int prev = 128;

            int outputSize = (input.Length + 7) / 8;
            byte[] output = new byte[outputSize];

            int outIndex = 0;
            int bitPos = 7;
            byte currentByte = 0;

            var sw = Stopwatch.StartNew();
            int reportInterval = Math.Max(1, input.Length / 100);

            for (int i = 0; i < input.Length; i++)
            {
                int current = input[i];

                int bit;

                if (current >= prev)
                {
                    bit = 1;
                    prev += step;
                }
                else
                {
                    bit = 0;
                    prev -= step;
                }


                prev = Math.Max(0, Math.Min(255, prev));


                currentByte |= (byte)(bit << bitPos);
                bitPos--;

                if (bitPos < 0)
                {
                    output[outIndex++] = currentByte;
                    currentByte = 0;
                    bitPos = 7;
                }

                // ---- real-time monitoring + cooperative cancellation ----
                if (i % reportInterval == 0)
                {
                    if (isCancelled != null && isCancelled())
                        return null;

                    Report(report, sw, i + 1, outIndex, input.Length);
                }
            }


            if (bitPos != 7)
            {
                output[outIndex] = currentByte;
            }

            Report(report, sw, input.Length, output.Length, input.Length);
            return output;
        }

        public byte[] Decompress(byte[] input, CompressionSettings settings)
        {
            if (input == null || input.Length == 0)
                return new byte[0];

            int step = 1;
            int value = 128;

            byte[] output = new byte[input.Length * 8];

            int outIndex = 0;

            foreach (byte b in input)
            {
                for (int bitPos = 7; bitPos >= 0; bitPos--)
                {
                    int bit = (b >> bitPos) & 1;

                    if (bit == 1)
                        value += step;
                    else
                        value -= step;

                    value = Math.Max(0, Math.Min(255, value));

                    output[outIndex++] = (byte)value;
                }
            }

            return output;
        }

        private static void Report(Action<CompressionProgress> report, Stopwatch sw,
                                   long inputProcessed, long outputSoFar, long total)
        {
            if (report == null) return;

            double elapsed = sw.Elapsed.TotalSeconds;
            report(new CompressionProgress
            {
                Percent = total == 0 ? 100 : (int)(inputProcessed * 100 / total),
                InputProcessed = inputProcessed,
                OutputSize = outputSoFar,
                ElapsedSeconds = elapsed,
                Ratio = outputSoFar <= 0 ? 0 : (double)inputProcessed / outputSoFar,
                SpeedMBPerSec = elapsed <= 0 ? 0 : inputProcessed / elapsed / (1024.0 * 1024.0)
            });
        }
    }
}
