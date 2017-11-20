using MP3Sharp;
using NAudio.Wave;
using NLayer.NAudioSupport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestMp3Player
{
    class Program
    {
        public string BaseDirectory { get; set; }
        public string Mp3FilePath { get; set; }

        static void Main(string[] args)
        {
            Program p = new Program();
            p.Init();

            //p.MP3SharpDoing();
            //p.NLayerDoing();

            p.PlayerDoing();

            Console.ReadKey();
        }

        public void Init()
        {
            this.BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            this.Mp3FilePath = Path.Combine(this.BaseDirectory, "Getz_Me_to_Brazil.mp3");
        }

        public void NLayerDoing()
        {
            var builder = new Mp3FileReader.FrameDecompressorBuilder(wf => new Mp3FrameDecompressor(wf));
            var reader = new Mp3FileReader(this.Mp3FilePath, builder);
            //// play or process the file, e.g.:
            //waveOut.Init(reader);
            //waveOut.Play();
        }

        public void MP3SharpDoing()
        {
            // open the mp3 file.
            MP3Stream stream = new MP3Stream(this.Mp3FilePath);
            // Create the buffer.
            byte[] buffer = new byte[4096];
            // read the entire mp3 file.
            int bytesReturned = 1;
            int totalBytesRead = 0;
            while (bytesReturned > 0)
            {
                bytesReturned = stream.Read(buffer, 0, buffer.Length);
                totalBytesRead += bytesReturned;
            }
            // close the stream after we're done with it.
            stream.Close();
        }

        public void PlayerDoing()
        {
            Player player = new Player();
            player.Open(this.Mp3FilePath);

            player.Play();

            do
            {
                Thread.Sleep(1000);
            } while (true);
        }
    }
}
