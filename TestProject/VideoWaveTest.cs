using System.IO;
using System.Threading.Tasks;
using auxmic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Wave;

namespace TestProject
{
    [TestClass()]
    public class VideoWaveTest
    {
        [TestMethod()]
        public void CreateWave()
        {
            var testTask = Task.Run(() =>
            {
                // try
                // {
                // TODO: Fix hard coded path.
                    FileToWaveStream.PathToFFmpegExe = "D:/apps/ffmpeg-4.4-full_build/bin/ffmpeg.exe";
                    FileToWaveStream.Log = new ConsoleLogger();
                    // VideoWave.Log = new RollingLogFile("d:/tmp","xx", 2, 500000);
                    
                    // TODO: Need permanent files
                    FileToWaveStream vw = new FileToWaveStream("D:/Videos/holby-00-master-missing-start.mp4",
                        new WaveFormat(8192, 2), null);
                    
                    // VideoWave vw = new VideoWave("D:/Videos/holby-01-start.mp4");
                    byte[] buffer = new byte[512];
                    File.Delete("D:/Videos/holby-01-start-ffmpeg.wav");
                    var writer = File.OpenWrite("D:/Videos/ffmpeg-test.wav");
                    int read = vw.Read(buffer, 0, 3);
                    while (read > 0)
                    {
                        writer.Write(buffer, 0, read);
                        read = vw.Read(buffer, 0, buffer.Length);
                    }

                    vw.Close();
                    writer.Close();
                // }
                // catch (Exception e)
                // {
                // }
            });

            testTask.Wait();

            WaveFileReader file = new WaveFileReader("D:/Videos/ffmpeg-test.wav");
            Assert.AreEqual(8192, file.WaveFormat.SampleRate);
            Assert.AreEqual(2, file.WaveFormat.Channels);
        }
       
    }
}