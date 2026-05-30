using System.IO;
using NAudio.Wave;
using WindowsFormsApp1.Models;

namespace WindowsFormsApp1.Services
{
    public class AudioMetadataService
    {
        public AudioFileInfo GetAudioInfo(string path)
        {
            FileInfo fileInfo = new FileInfo(path);

            using (AudioFileReader reader =
                   new AudioFileReader(path))
            {
                return new AudioFileInfo
                {
                    FileName = fileInfo.Name,

                    FileSize =
                        FormatFileSize(fileInfo.Length),

                    Duration =
                        reader.TotalTime.ToString(
                            @"hh\:mm\:ss"),

                    SampleRate =
                        reader.WaveFormat.SampleRate,

                    Channels =
                        reader.WaveFormat.Channels,

                    BitRate =
                        reader.WaveFormat
                              .AverageBytesPerSecond * 8,

                    CodecType =
                        Path.GetExtension(path)
                            .Replace(".", "")
                            .ToUpper()
                };
            }
        }

        private string FormatFileSize(long bytes)
        {
            double kb = bytes / 1024.0;
            double mb = kb / 1024.0;

            if (mb >= 1)
            {
                return mb.ToString("F2") + " MB";
            }

            return kb.ToString("F2") + " KB";
        }
    }
}