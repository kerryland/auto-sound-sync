using auxmic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using NAudio.Wave;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using auxmic.mediaUtil;
using auxmic.wave;

namespace TestProject
{

    /// <summary>
    ///This is a test class for SoundFileTest and is intended
    ///to contain all SoundFileTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SoundFileTest
    {

        string PathToFFmpegExe = "D:/apps/ffmpeg-4.4-full_build/bin/ffmpeg.exe";

        public SoundFileTest()
        {
            FFmpegTool.PathToFFmpegExe = PathToFFmpegExe;
            FileToWaveFile.FFmpegTool = new FFmpegTool(new ConsoleLogger(), PathToFFmpegExe);

        }




        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get { return testContextInstance; }
            set { testContextInstance = value; }
        }

        #region Additional test attributes

        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //

        #endregion


        /// <summary>
        ///A test for WaveData
        ///</summary>
        [TestMethod()]
        public void WaveData_dtmf_1to0_Test()
        {
            string filename = "dtmf_1to0.wav";

            Int32[] actual;
            Int32[] expected;

            ReadFile(filename, out actual, out expected, TestData.dtmf_1to0);

            AssertMostlyEqual(expected, actual);
        }

        private static void ReadFile(string testData, out Int32[] actual, out Int32[] expected, Int32[] fileData)
        {
            string filename = TestData.Filename(testData);

            WaveFormat resampleFormat = null; // so supposed to figure it out from source

            string tempFilename = FileToWaveFile.CreateSlow(filename, resampleFormat);

            // Now read the new wave file and see if it contains what we expect it to
            using (var waveFileReader = new WaveFileReader(tempFilename))
            {
                List<Int32> leftChannel = new List<Int32>();

                int L = 256; //_syncParams.L;
                long N = waveFileReader.SampleCount;

                for (int i = 0; i <= N - L; i += L) // sb i <= N - L
                {
                    Int32[] data = ReadFile(waveFileReader, L);
                    leftChannel.AddRange(data);
                }

                actual = leftChannel.ToArray();

                expected = fileData.Take(fileData.Length - fileData.Length % L).ToArray();
            }
        }

        private static int[] ReadFile(WaveFileReader waveFileReader, int samplesToRead)
        {
            Int32[] result = new Int32[samplesToRead];

            int blockAlign = waveFileReader.BlockAlign;

            byte[] buffer = new byte[blockAlign * samplesToRead];

            int bytesRead = waveFileReader.Read(buffer, 0, blockAlign * samplesToRead);

            for (int sample = 0; sample < bytesRead / blockAlign; sample++)
            {
                switch (waveFileReader.WaveFormat.BitsPerSample)
                {
                    case 8:
                        result[sample] = (Int16) buffer[sample * blockAlign];
                        break;

                    case 16:
                        result[sample] = BitConverter.ToInt16(buffer, sample * blockAlign);
                        break;

                    case 32:
                        result[sample] = BitConverter.ToInt32(buffer, sample * blockAlign);
                        break;

                    default:
                        throw new NotSupportedException(String.Format(
                            "BitDepth '{0}' not supported. Try 8, 16 or 32-bit audio instead.",
                            waveFileReader.WaveFormat.BitsPerSample));
                }
            }

            return result;
        }

        [TestMethod()]
        public void WaveData_DSC_6785_48kHz_16bit_mono_Test()
        {
            string filename = "DSC_6785_48kHz_16bit_mono.wav";

            Int32[] actual;
            Int32[] expected;

            ReadFile(filename, out actual, out expected, TestData.DSC_6785_48kHz_16bit_mono);

            AssertMostlyEqual(expected, actual);
        }

        [TestMethod()]
        public void WaveData_master_48kHz_16bit_stereo_Test()
        {
            string filename = "master_48kHz_16bit_stereo.wav";

            Int32[] actual;
            Int32[] expected;

            ReadFile(filename, out actual, out expected, TestData.master_48kHz_16bit_stereo);

            AssertMostlyEqual(expected, actual);
        }

        private void AssertMostlyEqual(ICollection expected,
            ICollection actual)
        {
            IEnumerator expectedEnumerator = expected.GetEnumerator();
            IEnumerator actualEnumerator = actual.GetEnumerator();
            int num = 0;
            while (expectedEnumerator.MoveNext() && actualEnumerator.MoveNext())
            {
                if (!Equals(expectedEnumerator.Current, actualEnumerator.Current))
                {
                    throw new ApplicationException(
                        $"Items at element {num} do not match. " +
                        $"Expected {expectedEnumerator.Current} vs " +
                        $"Actual {actualEnumerator.Current}");
                }

                ++num;
            }
        }
    }
}
    
    
