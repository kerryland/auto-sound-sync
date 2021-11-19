using System;
using System.IO;
using auxmic;
using auxmic.mediaUtil;
using auxmic.sync;
using auxmic.wave;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestProject
{

    [TestClass()]
    public class FingerprintStreamProverTest
    {
        private readonly string tmp = Path.GetTempPath() + "auxtest-" + Guid.NewGuid().ToString();

        string PathToFFmpegExe = "D:/apps/ffmpeg-4.4-full_build/bin/ffmpeg.exe";

        [TestInitialize()]
        public void MyTestInitialize()
        {
            VideoWave.PathToFFmpegExe = PathToFFmpegExe;
            // Directory.CreateDirectory(tmp);

            // testMp4 = tmp + "/mlk.mp4";

            // string command = "-loop 1 -t 5 -i " +
                             // TestData.Filename("mlk%02d.jpg") + " -i " +
                             // TestData.Filename("MLKDream_64kb.mp3") +" " + testMp4;

            // launcher.ExecuteFFmpeg(command);
            FileCache.Clear("wav");
        }
        
        [TestCleanup()]
        public void MyTestCleanup()
        {
            FileCache.Clear("wav");
            // Directory.Delete(tmp, true);
        }
        
        [TestMethod()]
        public void CheckFFmpegStream()
        {
            Clip clip = new Clip(TestData.Filename("mlk.mp4"), new SimpleFingerPrinter(), new ConsoleLogger());
            var stream = new PipedWaveProvider().GetStream(clip);
            var wav = new WAV_file();
            wav.loadFile(stream);
            
            Assert.AreEqual(48000, Convert.ToInt32(wav.SampleRate));
        }
        
        [TestMethod()]
        public void CheckNAudioFile()
        {
            Clip clip = new Clip(TestData.Filename("mlk.mp4"), new SimpleFingerPrinter(), new ConsoleLogger()); var stream = new NaudioWavefile().GetStream(clip);
            var wav = new WAV_file();
            wav.loadFile(stream);
            Assert.AreEqual(48000, Convert.ToInt32(wav.SampleRate));
            stream.Close();
        }

        [TestMethod()]
        public void CheckFFmpegWaveFile()
        {
            FFmpegTool _launcher = new FFmpegTool(new ConsoleLogger(), PathToFFmpegExe);

            Clip clip = new Clip(TestData.Filename("mlk.mp4"), new SimpleFingerPrinter(), new ConsoleLogger());
            var stream = new FFmpegWaveFile(_launcher).GetStream(clip);
            var wav = new WAV_file();
            wav.loadFile(stream);
            Assert.AreEqual(48000, Convert.ToInt32(wav.SampleRate));
            stream.Close();
        }
    }
}