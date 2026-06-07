using System;
using System.Diagnostics;
using AudioCompressor.Models;

namespace AudioCompressor.Services
{
    public class DpcmCompressionService : IAudioCompressionService
    {
        public byte[] Compress(byte[] input, CompressionSettings settings)
        {
            return Compress(input, settings, null, null);
        }

        public byte[] Compress(byte[] input, CompressionSettings settings,
                               Action<CompressionProgress> report, Func<bool> isCancelled)
        {
            if (input == null || input.Length == 0)
                return new byte[0];

            int levels = settings.QuantizationLevels;
            int step = Math.Max(1, 256 / levels);

            byte[] output = new byte[(input.Length + 1) / 2];

            int prev = 0;
            int outIndex = 0;

            var sw = Stopwatch.StartNew();
            int reportInterval = Math.Max(2, (input.Length / 100) & ~1); // keep even

            for (int i = 0; i < input.Length; i += 2)
            {
                int packedByte = 0;

                for (int j = 0; j < 2; j++)
                {
                    if (i + j >= input.Length) break;

                    int current = input[i + j];
                    int diff = current - prev;


                    int q = diff / step;


                    q = Math.Max(-8, Math.Min(7, q));

                    int encoded = q & 0x0F;

                    if (j == 0)
                        packedByte |= (encoded << 4);
                    else
                        packedByte |= encoded;

                    prev = current;
                }

                output[outIndex++] = (byte)packedByte;

                // ---- real-time monitoring + cooperative cancellation ----
                if (i % reportInterval == 0)
                {
                    if (isCancelled != null && isCancelled())
                        return null;

                    Report(report, sw, Math.Min(i + 2, input.Length), outIndex, input.Length);
                }
            }

            Report(report, sw, input.Length, output.Length, input.Length);
            return output;
        }

        public byte[] Decompress(byte[] input, CompressionSettings settings)
        {
            if (input == null || input.Length == 0)
                return new byte[0];

            int levels = settings.QuantizationLevels;
            int step = Math.Max(1, 256 / levels);


            byte[] output = new byte[input.Length * 2];

            int prev = 0;
            int outIndex = 0;

            for (int i = 0; i < input.Length; i++)
            {
                int packedByte = input[i];


                int encoded1 = (packedByte >> 4) & 0x0F;

                int q1 = encoded1 >= 8 ? encoded1 - 16 : encoded1;


                int diff1 = q1 * step;

                int current1 = prev + diff1;

                current1 = Math.Max(0, Math.Min(255, current1));
                output[outIndex++] = (byte)current1;
                prev = current1;


                int encoded2 = packedByte & 0x0F;

                int q2 = encoded2 >= 8 ? encoded2 - 16 : encoded2;


                int diff2 = q2 * step;

                int current2 = prev + diff2;


                current2 = Math.Max(0, Math.Min(255, current2));
                output[outIndex++] = (byte)current2;
                prev = current2;
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
