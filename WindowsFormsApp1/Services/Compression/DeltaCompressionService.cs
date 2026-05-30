using System;
using AudioCompressor.Models;

namespace AudioCompressor.Services
{
    public class DeltaCompressionService : IAudioCompressionService
    {
        public byte[] Compress(byte[] input, CompressionSettings settings)
        {
            if (input == null || input.Length == 0)
                return new byte[0];

            byte[] output = new byte[input.Length];

            int step = 1;

            int prev = 0;

            for (int i = 0; i < input.Length; i++)
            {
                int current = input[i];

                if (current >= prev)
                {
                    output[i] = 1;
                    prev += step;
                }
                else
                {
                    output[i] = 0;
                    prev -= step;
                }
            }

            return output;
        }

        public byte[] Decompress(byte[] input, CompressionSettings settings)
        {
            if (input == null || input.Length == 0)
                return new byte[0];

            byte[] output = new byte[input.Length];

            int step = 1;

            int value = 128;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == 1)
                    value += step;
                else
                    value -= step;

                value = Math.Max(0, Math.Min(255, value));

                output[i] = (byte)value;
            }

            return output;
        }
    }
}