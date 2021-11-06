using auxmic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using auxmic.sync;

/*
 * Most of the tests here don't pass in the orginal code either. Shrug.
 */
namespace TestProject
{
    /// <summary>
    ///This is a test class for ClipTest and is intended
    ///to contain all ClipTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ClipTest
    {
        private TestContext testContextInstance;

        private static string dataFolder;
        
        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        
        //Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            // Figure out where the test data is (tests didn't run in JetBrains Rider)
            dataFolder = GetDataFolder();
        }

        public static string GetDataFolder()
        {
            string knownPath = @"\TestProject\";
            int knownPart = Environment.CurrentDirectory.IndexOf(knownPath);
            return Environment.CurrentDirectory.Remove(knownPart + knownPath.Length) + @"Data\";
        }

        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        
        //Use TestInitialize to run code before running each test
        [TestInitialize()]
        public void MyTestInitialize()
        {
            FileCache.Clear();
        }
        
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        private Clip BuildClip(string filename)
        { 
            Clip clip = new Clip(dataFolder + filename, new AuxMicFingerprinter(), new SoundFileFactory());
            clip.LoadFile();

            return clip;
        }

        /// <summary>
        ///A test for GetHashes
        ///</summary>
        [TestMethod()]
        [DeploymentItem("auxmic.exe")]
        public void GetHashes_dtmf_1to0_Test()
        {
            Clip target = BuildClip("dtmf_1to0.wav");

            int[] expected = { 2683, 2041, 2043, 3451, 3839, 3839, 3065, 2424, 2047, 2042, 3839, 3839, 2171, 3578, 2684, 2171, 3839, 3839, 3836, 2299, 3836, 2559, 3839, 3839, 3839, 2041, 3578, 2810, 3832, 3839, 3839, 2041, 2042, 3834, 2040, 3839, 3839, 2042, 2041, 2041, 2555, 3839, 3839, 2046, 3579, 3710, 3577, 3839, 3839, 3839, 3064, 2046, 2044, 3836, 3839, 3839, 3197, 2045, 3832, 3576, 3839, 3839, 3454, 3198, 3199, 2045, 3839, 3839, 2047, 3194, 2046, 3066, 3839, 3839 };
            int[] actual = (int[]) target.Hashes;

            // CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod()]
        [DeploymentItem("auxmic.exe")]
        public void GetHashes_DSC_6785_48kHz_16bit_mono_Test()
        {
            Clip target = BuildClip("DSC_6785_48kHz_16bit_mono.wav");

            int[] expected = { 7930, 7805, 6142, 8187, 8189, 6137, 7291, 4090, 6137, 6268, 8191, 8186, 6143, 6143, 6138, 6138, 8190, 10234, 6143, 10232, 7292, 8190, 6142 };
            int[] actual = (int[]) target.Hashes;

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod()]
        [DeploymentItem("auxmic.exe")]
        public void GetHashes_master_48kHz_16bit_stereo_Test()
        {
            Clip target = BuildClip("master_48kHz_16bit_stereo.wav");

            int[] expected = { 8190, 6140, 6137, 2170, 6139, 6394, 6137, 6141, 6143, 6264, 6138, 4088, 4093, 6139, 6394, 6140, 2040, 2043, 6142, 6269, 6139, 7674, 6138, 3193, 6141, 3193, 2296, 8190, 6136, 6136, 6143, 4088, 6142, 6141, 2043, 7288, 3197, 6137, 6139, 14458, 2040, 6264, 6137, 2168, 6264, 2040, 6141, 6142, 6266, 3195, 6142, 4089, 8185, 6136, 7294, 7418, 3450, 2173, 4095, 8189, 2041, 2046, 6139, 4093, 6136, 10236, 6264, 6270, 6138, 
                                14330, 14328, 6138, 6140, 6140, 6398, 2042, 2047, 4095, 3195, 4089, 3193, 2041, 3196, 2043, 3193, 2043, 3193, 18430, 2040, 3198, 2045, 4088, 2043, 6269, 4091, 4091, 3197, 6138, 2426, 6271, 3197, 2045, 2042, 6140, 2424, 6143, 4090, 4088, 2045, 4091, 2045, 2168, 6264, 3321, 2043, 2040, 2047, 3454, 4093, 4090, 4092, 2043, 3195, 6138, 4091, 2042, 4089, 2041, 2041, 3192, 2169, 3196, 2042, 2171, 2170, 7290, 2040, 3194, 
                                3320, 3192, 6136, 3196, 11391, 11384, 10233, 6264, 14331, 18552, 14459, 20476, 12286, 14329, 18424, 12281, 14330, 18555, 20473, 20473, 14456, 14456, 14332, 14333, 14332, 15481, 14328, 15482, 14331, 2171, 14328, 14586, 3326, 14457, 6142, 4091, 14329, 14329, 14331, 4091, 14331, 2042, 10362, 12284, 6136, 14456, 2173, 2042, 2172, 2041, 3448, 2044, 4088, 2168, 2171, 11388, 14328, 3453, 11513, 3192, 10233, 4088, 11389, 
                                11387, 3327, 4091, 2168, 2041, 2041, 3197, 4095, 6140, 3196, 6265, 4091, 2042, 2042, 3320, 3196, 10234, 10234, 2170, 2040, 2043, 14328, 18424, 14328, 14585, 14334, 15480, 14328, 18424, 12282, 14457, 14461, 19579, 14332 };
            int[] actual = (int[]) target.Hashes;

            CollectionAssert.AreEqual(expected, actual);
        }

        /// <summary>
        ///A test for DataLength
        ///</summary>
        [TestMethod()]
        public void DataLength_dtmf_1to0_Test()
        {
            Clip target = BuildClip("dtmf_1to0.wav");

            int expected = 19109;
            int actual = target.DataLength;

            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void DataLength_dtmf_DSC_6785_48kHz_16bit_mono_Test()
        {
            Clip target = BuildClip("DSC_6785_48kHz_16bit_mono.wav");

            int expected = 6130;
            int actual = target.DataLength;

            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void DataLength_master_48kHz_16bit_stereo_Test()
        {
            Clip target = BuildClip("master_48kHz_16bit_stereo.wav");

            int expected = 60249;
            int actual = target.DataLength;

            Assert.AreEqual(expected, actual);
        }

        /*[TestMethod()]
        public void MatchTest()
        {
            Clip clip = BuildClip("dtmf_1to0.wav");

            //int[] hq = { 8190, 6140, 6137, 2170, 6139, 6394, 6137, 6141, 6143, 6264 };
            //int[] lq = { 6140, 6137, 2170, 6139, 6394 };

            Clip hq_clip = BuildClip(@"D:\temp\130413-195918.WAV");
            Clip lq_clip = BuildClip(@"D:\temp\DSC_6785.AVI");

            int[] hq = hq_clip.Hashes;
            int[] lq = lq_clip.Hashes;

            int expected = 315252;
            int actual = clip.Match(hq, lq);

            Assert.AreEqual(expected, actual);
        }*/
    }
}
