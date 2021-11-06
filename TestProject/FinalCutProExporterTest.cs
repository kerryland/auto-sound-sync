using auxmic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using auxmic.editorExport;
using auxmic.sync;
using NAudio.Wave;

namespace TestProject
{
    [TestClass()]
    public class FinalCutProExporterTest
    {
        /*
         * Given a master track that is 30 minutes long
         * and clip 1 that begins 5 minutes before the master track, and is 10 minutes long
         * and clip 2 that begins 15 minutes into the master track, and is 8 minutes long
         * and clip 3 that begins 7 minutes before the end of the master track, and is 14 minutes long
         *
         *      ------------------------------              master
         * ----------                                       clip 1
         *                --------                          clip 2
         *                             --------------       clip 3
         * <------- total duration 43 minutes ------>
         *
         * Then the Final Cut Pro XML should include code like this. Please note that
         * Final Cut Pro durations are seconds * 25.
         * 
         * <sequence>
         *    <name>Sequence 1</name>
         *    <duration>63000</duration>    63000 / 25 =  5 mins + 30 mins + 7 mins. 42 Mins * 60 = 2520 seconds
         *
         *  <name>master.mp4</name>
         *  <duration>45000</duration>      45000 / 25 = 1800 seconds, or 30 minutes
         *  <start>7500</start>              7500 / 25 =  300 seconds, or 5 minutes
         *  <end>52500</end>                52500 / 25 = 2100 seconds, or 35 minutes
         *  <in>0</in>
         *  <out>45000</out>
         *
         *  see the test file in TestProject/Data/FinalCutProject.xml
         */
        [TestMethod()]
        public void CheckExportFormatWhenMasterIsVideo()
        {
            bool masterIsVideo = true;
            string masterFilename = "master.mp4";
            string expectedXmlFile = "FinalCutProject-master-is-video.xml";
            
            PerformTest(masterFilename, masterIsVideo, expectedXmlFile);
        }

        [TestMethod()]
        public void CheckExportFormatWhenMasterIsAudio()
        {
            bool masterIsVideo = false;
            string masterFilename = "master.wav";
            string expectedXmlFile = "FinalCutProject-master-is-audio.xml";
            
            PerformTest(masterFilename, masterIsVideo, expectedXmlFile);
        }

        private static void PerformTest(string masterFilename, bool masterIsVideo, string expectedXmlFile)
        {
            SimpleFingerPrinter fingerPrinter = new SimpleFingerPrinter();

            SimpleSoundFile masterSoundFile = new SimpleSoundFile()
                .WithWaveFormat(new WaveFormat(48000, 2))
                .WithDurationInSeconds(30 * 60);

            Clip master = new Clip(masterFilename, fingerPrinter, new SimpleSoundFileFactory(masterSoundFile));
            master.LoadFile();

            SimpleSoundFile clip1SoundFile = new SimpleSoundFile()
                .WithWaveFormat(new WaveFormat(48000, 2))
                .WithDurationInSeconds(10 * 60);

            SimpleSoundFile clip2SoundFile = new SimpleSoundFile()
                .WithWaveFormat(new WaveFormat(48000, 2))
                .WithDurationInSeconds(8 * 60);

            SimpleSoundFile clip3SoundFile = new SimpleSoundFile()
                .WithWaveFormat(new WaveFormat(48000, 2))
                .WithDurationInSeconds(14 * 60);

            var clip1 = new Clip("clip01.mp4", fingerPrinter, new SimpleSoundFileFactory(clip1SoundFile));
            var clip2 = new Clip("clip02.mp4", fingerPrinter, new SimpleSoundFileFactory(clip2SoundFile));
            var clip3 = new Clip("clip03.mp4", fingerPrinter, new SimpleSoundFileFactory(clip3SoundFile));

            clip1.LoadFile();
            clip2.LoadFile();
            clip3.LoadFile();

            var lqClips = new List<Clip>
            {
                clip1,
                clip2,
                clip3
            };

            IDictionary<Clip, ClipMatch> matches = new Dictionary<Clip, ClipMatch>();
            matches[clip1] = new ClipMatch(5 * 60, 0, -5 * 60);
            matches[clip2] = new ClipMatch(0, 15 * 60, 15 * 60);
            matches[clip3] = new ClipMatch(0, 23 * 60, 23 * 60);
            fingerPrinter.Prepare(matches);

            master.CalcHashes();
            clip1.CalcHashes();
            clip2.CalcHashes();
            clip3.CalcHashes();

            clip1.Sync(master);
            clip2.Sync(master);
            clip3.Sync(master);

            var writer = new StringWriter();
            IEditorExporter exporter = new FinalCutProExporter(new SimpleMediaTool(masterIsVideo));
            exporter.Export(master, lqClips, writer);

            XDocument expectedXml = XDocument.Load(ClipTest.GetDataFolder() + expectedXmlFile);
            XDocument actualXml = XDocument.Load(new StringReader(writer.ToString()));

            var deepEquals = XNode.DeepEquals(expectedXml, actualXml);
            Assert.IsTrue(deepEquals, "Expected XML not generated");
        }
    }

    class SimpleMediaTool : IMediaTool
    {
        private bool _isVideo;

        public SimpleMediaTool(bool isVideo)
        {
            _isVideo = isVideo;
        }

        public bool IsVideo(string filename)
        {
            return _isVideo;
        }
    }

    class SimpleFingerPrinter : IFingerprinter
    {
        private IDictionary<Clip, ClipMatch> _dictionary;

        public void Prepare(IDictionary<Clip, ClipMatch> dictionary)
        {
            _dictionary = dictionary;
        }

        public object CreateFingerPrints(Clip clip)
        {
            return clip; // any object will do
        }

        public ClipMatch matchClips(Clip master, Clip lqClip)
        {
            ClipMatch match = _dictionary[lqClip];
            lqClip.Offset = TimeSpan.FromSeconds(match.Offset);
            return match;
        }

        public void Cleanup(Clip clip)
        {
        }
    }

    class SimpleSoundFileFactory : ISoundFileFactory
    {
        private readonly SimpleSoundFile _soundFile;

        public SimpleSoundFileFactory(SimpleSoundFile soundFile)
        {
            _soundFile = soundFile;
        }

        public ISoundFile CreateSoundFile(string filename, WaveFormat resampleFormat)
        {
            _soundFile.Filename = filename;
            if (resampleFormat != null)
            {
                _soundFile.WaveFormat = resampleFormat;
            }

            return _soundFile;
        }
    }
}