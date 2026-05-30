using AudioCompressor.Models;

namespace AudioCompressor.Services
{
    public interface IAudioCompressionService
    {
        byte[] Compress(byte[] input, CompressionSettings settings);

        byte[] Decompress(byte[] input, CompressionSettings settings);
    }
}