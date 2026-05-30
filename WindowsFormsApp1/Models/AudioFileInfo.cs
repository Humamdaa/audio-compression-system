namespace WindowsFormsApp1.Models
{
    public class AudioFileInfo
    {
        public string FileName { get; set; }

        public string FileSize { get; set; }

        public string Duration { get; set; }

        public int SampleRate { get; set; }

        public int Channels { get; set; }

        public int BitRate { get; set; }

        public string CodecType { get; set; }
    }
}