using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using auxmic;
using auxmic.editorExport;
using auxmic.logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
                    VideoWave.PathToFFmpegExe = "D:/apps/ffmpeg-4.4-full_build/bin/ffmpeg.exe";
                    VideoWave.Log = new ConsoleLogger();
                    // VideoWave.Log = new RollingLogFile("d:/tmp","xx", 2, 500000);
                    VideoWave vw = new VideoWave("D:/Videos/holby-00-master-missing-start.mp4");
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
        }
       
    }
}