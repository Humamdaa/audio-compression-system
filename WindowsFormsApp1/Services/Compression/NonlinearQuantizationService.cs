using System;
using AudioCompressor.Models;

namespace AudioCompressor.Services
{
    public class NonlinearQuantizationService : IAudioCompressionService
    {
        public byte[] Compress(byte[] input, CompressionSettings settings)
        {
            if (input == null || input.Length == 0)
                return new byte[0];

            byte[] output = new byte[input.Length];

            int levels = settings.QuantizationLevels;

            for (int i = 0; i < input.Length; i++)
            {
                double normalized = input[i] / 255.0;

                // nonlinear (log-like compression)
                double nonlinear = Math.Pow(normalized, 0.5);

                int quantized = (int)(nonlinear * (levels - 1));

                output[i] = (byte)quantized;
            }

            return output;
        }

        public byte[] Decompress(byte[] input, CompressionSettings settings)
        {
            if (input == null || input.Length == 0)
                return new byte[0];

            byte[] output = new byte[input.Length];

            int levels = settings.QuantizationLevels;

            for (int i = 0; i < input.Length; i++)
            {
                double normalized = (double)input[i] / (levels - 1);

                double restored = Math.Pow(normalized, 2.0);

                int value = (int)(restored * 255);

                output[i] = (byte)Math.Max(0, Math.Min(255, value));
            }

            return output;
        }
    }
}