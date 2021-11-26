using System;
using System.IO;
using auxmic;
using auxmic.mediaUtil;
using auxmic.sync;
using auxmic.wave;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Wave;

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
            WaveFormat waveFormat = new WaveFormat(44100, 2);
            Clip clip = new Clip(TestData.Filename("mlk.mp4"), new SimpleFingerPrinter(), new ConsoleLogger(), 
                new SimpleSoundFileFactory(new SimpleSoundFile().WithWaveFormat(waveFormat))); 
            
            clip.LoadFile();
            using var stream = new PipedWaveProvider().GetStream(clip);
            var wav = new WAV_file();
            wav.loadFile(stream);
            // stream.Close();
            // clip.Dispose();
            
            Assert.AreEqual(44100, Convert.ToInt32(wav.SampleRate));
            Assert.AreEqual(2, Convert.ToInt32(wav.NumOfChannels));
        }
       
        [TestMethod()]
        public void CheckNAudioFile()
        {
            FingerprintStreamProvider.Log = new ConsoleLogger();
            
            Clip clip = new Clip(TestData.Filename("mlk.mp4"), new SimpleFingerPrinter(),
                FingerprintStreamProvider.Log);
            
            using var stream = new NaudioWavefile().GetStream(clip);
            var wav = new WAV_file();
            
            wav.loadFile(stream);
            Assert.AreEqual(22050, Convert.ToInt32(wav.SampleRate));
            Assert.AreEqual(1, Convert.ToInt32(wav.NumOfChannels));
        }

        
        [TestMethod()]
        public void CheckFFmpegWaveFile()
        {
            var log = new ConsoleLogger();
            FileToWaveFile.FFmpegTool = new FFmpegTool(log, PathToFFmpegExe);
            FingerprintStreamProvider.Log = log;

            Clip clip = new Clip(TestData.Filename("mlk.mp4"), new SimpleFingerPrinter(), log);
            using var stream = new FFmpegWaveFile().GetStream(clip);
            var wav = new WAV_file();
            wav.loadFile(stream);
            Assert.AreEqual(22050, Convert.ToInt32(wav.SampleRate));
            Assert.AreEqual(1, Convert.ToInt32(wav.NumOfChannels));
        }
    }
}