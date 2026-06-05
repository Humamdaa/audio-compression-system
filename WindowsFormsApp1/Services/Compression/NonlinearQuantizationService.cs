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

            int levels = settings.QuantizationLevels; 


            if (levels <= 16)
            {
                byte[] output = new byte[(input.Length + 1) / 2];
                int outIndex = 0;

                for (int i = 0; i < input.Length; i += 2)
                {

                    double norm1 = input[i] / 255.0;
                    double nonLin1 = Math.Pow(norm1, 0.5);
                    int q1 = (int)(nonLin1 * (levels - 1));
                    q1 = Math.Max(0, Math.Min(15, q1)); 


                    int q2 = 0;
                    if (i + 1 < input.Length)
                    {
                        double norm2 = input[i + 1] / 255.0;
                        double nonLin2 = Math.Pow(norm2, 0.5);
                        int q2Raw = (int)(nonLin2 * (levels - 1));
                        q2 = Math.Max(0, Math.Min(15, q2Raw));
                    }

                    byte packedByte = (byte)((q1 << 4) | (q2 & 0x0F));
                    output[outIndex++] = packedByte;
                }
                return output;
            }
            else
            {

                byte[] output = new byte[input.Length];
                for (int i = 0; i < input.Length; i++)
                {
                    double normalized = input[i] / 255.0;
                    double nonlinear = Math.Pow(normalized, 0.5);
                    output[i] = (byte)(nonlinear * (levels - 1));
                }
                return output;
            }
        }

        public byte[] Decompress(byte[] input, CompressionSettings settings)
        {
            if (input == null || input.Length == 0)
                return new byte[0];

            int levels = settings.QuantizationLevels;

            if (levels <= 16)
            {

                byte[] output = new byte[input.Length * 2];
                int outIndex = 0;

                for (int i = 0; i < input.Length; i++)
                {
                    byte packedByte = input[i];

  
                    int q1 = (packedByte >> 4) & 0x0F;
                    double norm1 = (double)q1 / (levels - 1);
                    double rest1 = Math.Pow(norm1, 2.0);
                    int v1 = (int)(rest1 * 255);
                    if (outIndex < output.Length)
                        output[outIndex++] = (byte)Math.Max(0, Math.Min(255, v1));


                    int q2 = packedByte & 0x0F;
                    double norm2 = (double)q2 / (levels - 1);
                    double rest2 = Math.Pow(norm2, 2.0);
                    int v2 = (int)(rest2 * 255);
                    if (outIndex < output.Length)
                        output[outIndex++] = (byte)Math.Max(0, Math.Min(255, v2));
                }
                return output;
            }
            else
            {
                byte[] output = new byte[input.Length];
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
}