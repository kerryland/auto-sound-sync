using auxmic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using auxmic.editorExport;
using auxmic.sync;
using NAudio.Wave;

namespace TestProject
{
    [TestClass()]
    public class FinalCutProExporterTest
    {

        [TestMethod()]
        public void CheckExportFormat()
        {
            SimpleSoundFile masterSoundFile = new SimpleSoundFile()
                .WithLength(40000L)
                .WithDataLength(20000)
                .WithWaveFormat(new WaveFormat(48000, 2));
            
            Clip master = new Clip("master.mp4", null, new SimpleSoundFileFactory(masterSoundFile));
            master.LoadFile();
            
            SimpleSoundFile clip1SoundFile = new SimpleSoundFile()
                .WithLength(10000L)
                .WithDataLength(5000)
                .WithWaveFormat(new WaveFormat(48000, 2));

            SimpleSoundFile clip2SoundFile = new SimpleSoundFile()
                .WithLength(30000L)
                .WithDataLength(15000)
                .WithWaveFormat(new WaveFormat(48000, 2));

            var clip1 = new Clip("clip01.mp4", null, new SimpleSoundFileFactory(clip1SoundFile));
            var clip2 = new Clip("clip02.mp4", null, new SimpleSoundFileFactory(clip2SoundFile));

            clip1.LoadFile();
            clip2.LoadFile();
            
            var lqClips = new List<Clip>
            {
                clip1,
                clip2
            };

            var writer = new StringWriter();
            
            IEditorExporter exporter = new FinalCutProExporter();
            exporter.Export(master, lqClips, writer );
            
            Assert.AreEqual("different", writer.ToString(), false);

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