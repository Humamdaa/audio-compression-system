using System;

namespace AudioCompressor.Models
{
    public class CompressionSettings
    {
        public int SampleRate { get; set; } = 44100;

        // عدد مستويات التكميم
        public int QuantizationLevels { get; set; } = 256;

        // اختيار نوع الخوارزمية
        public CompressionAlgorithm Algorithm { get; set; }

        // نسبة الضغط (اختياري للواجهة)
        public float CompressionRatio { get; set; } = 1.0f;
    }

    public enum CompressionAlgorithm
    {
        DPCM,
        DeltaModulation,
        NonLinearQuantization
    }
}