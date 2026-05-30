using NAudio.Wave;

namespace WindowsFormsApp1.Services
{
    public class AudioPlayerService
    {
        private WaveOutEvent outputDevice;
        private AudioFileReader audioFile;

        public void Play(string path)
        {
            Stop();

            audioFile = new AudioFileReader(path);

            outputDevice = new WaveOutEvent();

            outputDevice.Init(audioFile);

            outputDevice.Play();
        }

        public void Stop()
        {
            outputDevice?.Stop();
            outputDevice?.Dispose();

            audioFile?.Dispose();

            outputDevice = null;
            audioFile = null;
        }
    }
}