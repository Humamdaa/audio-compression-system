using System;
using AudioCompressor.Models;
using AudioCompressor.Services;

class Program
{
    static void Main()
    {
        byte[] data = System.IO.File.ReadAllBytes(
            @"C:\Users\GharamOthman\Desktop\audio-compression-system\_report_build\sample.wav");

        Console.WriteLine("Input size: " + data.Length + " bytes");

        var algos = new IAudioCompressionService[] {
            new DpcmCompressionService(), new DeltaCompressionService(), new NonlinearQuantizationService() };
        var names = new string[] { "DPCM", "DeltaModulation", "NonLinearQuantization" };
        var en = new CompressionAlgorithm[] {
            CompressionAlgorithm.DPCM, CompressionAlgorithm.DeltaModulation, CompressionAlgorithm.NonLinearQuantization };

        for (int k = 0; k < algos.Length; k++)
        {
            var svc = algos[k];
            var s = new CompressionSettings { QuantizationLevels = 16, SampleRate = 44100, Algorithm = en[k] };

            var comp = svc.Compress(data, s);
            var dec = svc.Decompress(comp, s);
            double ratio = comp.Length > 0 ? (double)data.Length / comp.Length : 0;

            Console.WriteLine();
            Console.WriteLine("[" + names[k] + "]  compress=" + comp.Length +
                "  ratio=" + ratio.ToString("F2") + "x  decompress=" + dec.Length + "  (orig=" + data.Length + ")");

            int calls = 0, last = -1; double lr = 0, ls = 0;
            Action<CompressionProgress> rep = p => { calls++; last = p.Percent; lr = p.Ratio; ls = p.SpeedMBPerSec; };
            svc.Compress(data, s, rep, () => false);
            Console.WriteLine("   progress: calls=" + calls + " lastPct=" + last +
                " ratio=" + lr.ToString("F2") + " speed=" + ls.ToString("F1") + "MB/s");

            var c3 = svc.Compress(data, s, null, () => true);
            Console.WriteLine("   cancel -> null: " + (c3 == null));
        }

        var d1 = new DpcmCompressionService().Compress(data, new CompressionSettings { QuantizationLevels = 4 });
        var d2 = new DpcmCompressionService().Compress(data, new CompressionSettings { QuantizationLevels = 64 });
        bool diff = false; int m = Math.Min(d1.Length, d2.Length);
        for (int i = 0; i < m; i++) { if (d1[i] != d2[i]) { diff = true; break; } }
        Console.WriteLine();
        Console.WriteLine("Settings effect (DPCM levels 4 vs 64 differ): " + diff);
        Console.WriteLine("DivByZero guard (levels=1024): " +
            (new DpcmCompressionService().Compress(data, new CompressionSettings { QuantizationLevels = 1024 }).Length > 0
             ? "OK (no crash)" : "empty"));
    }
}
