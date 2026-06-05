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

            int step = 1;
            int prev = 128;

            int outputSize = (input.Length + 7) / 8;
            byte[] output = new byte[outputSize];

            int outIndex = 0;
            int bitPos = 7;
            byte currentByte = 0;

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
            }

 
            if (bitPos != 7)
            {
                output[outIndex] = currentByte;
            }

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
    }
}