using System;
using AudioCompressor.Models;

namespace AudioCompressor.Services
{
    public class DpcmCompressionService : IAudioCompressionService
    {
        public byte[] Compress(byte[] input, CompressionSettings settings)
        {
            if (input == null || input.Length == 0)
                return new byte[0];

            byte[] output = new byte[input.Length];

            int prev = 0;

            for (int i = 0; i < input.Length; i++)
            {
                int current = input[i];

                int diff = current - prev;

                output[i] = (byte)(diff + 128); // shift to avoid negatives

                prev = current;
            }

            return output;
        }

        public byte[] Decompress(byte[] input, CompressionSettings settings)
        {
            if (input == null || input.Length == 0)
                return new byte[0];

            byte[] output = new byte[input.Length];

            int prev = 0;

            for (int i = 0; i < input.Length; i++)
            {
                int diff = input[i] - 128;

                int value = prev + diff;

                value = Math.Max(0, Math.Min(255, value));

                output[i] = (byte)value;

                prev = value;
            }

            return output;
        }
    }
}