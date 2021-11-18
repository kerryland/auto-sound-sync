using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using auxmic.logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestProject
{
    [TestClass()]
    public class RollingLogFileTest
    {
        private readonly string folder = Path.GetTempPath() + "auxtest-" + Guid.NewGuid().ToString();

        private static int
            PADDING = 23; // Each line is prefixed with a timestamp, and suffixed with EOL that adds 32 characters

        [TestInitialize()]
        public void MyTestInitialize()
        {
            Directory.CreateDirectory(folder);
        }

        [TestCleanup()]
        public void MyTestCleanup()
        {
            Directory.Delete(folder, true);
        }

        [TestMethod()]
        public void CheckFileRolls()
        {
            // Create a log file that rolls when it gets larger than 50 bytes, and has no more than 3 log files.
            RollingLogFile rollingLogFile = new RollingLogFile(folder, "auxtest", 3, 50);

            WriteToFile(rollingLogFile, 40);
            Assert.IsFalse(File.Exists(folder + "/auxtest-01.log"), "File should not have rolled yet");

            // This log should trigger rollover to a new file
            WriteToFile(rollingLogFile, 60);

            Assert.AreEqual(60 + PADDING, new FileInfo(folder + "/auxtest.log").Length,
                "Base log file has wrong size");

            // Original log file should have rolled over
            Assert.AreEqual(40 + PADDING, new FileInfo(folder + "/auxtest-01.log").Length,
                "01 log file has wrong size");

            Assert.IsFalse(File.Exists(folder + "/auxtest-02.log"), "02 File should not exist");

            // Add log again
            WriteToFile(rollingLogFile, 75); // +32 == 107

            Assert.AreEqual(75 + PADDING, new FileInfo(folder + "/auxtest.log").Length,
                "Base log file has wrong size");

            Assert.AreEqual(60 + PADDING, new FileInfo(folder + "/auxtest-01.log").Length,
                "01 log file has wrong size");

            Assert.AreEqual(40 + PADDING, new FileInfo(folder + "/auxtest-02.log").Length,
                "02 log file has wrong size");

            // Force another log roll. This one should cause the old 02 file to drop off
            WriteToFile(rollingLogFile, 100);

            Assert.AreEqual(100 + PADDING, new FileInfo(folder + "/auxtest.log").Length,
                "00 log file has wrong size");

            Assert.AreEqual(75 + PADDING, new FileInfo(folder + "/auxtest-01.log").Length,
                "01 log file has wrong size");

            // 01 should now be 02
            Assert.AreEqual(60 + PADDING, new FileInfo(folder + "/auxtest-02.log").Length,
                "02 log file has wrong size");

            Assert.IsFalse(new FileInfo(folder + "/auxtest-03.log").Exists, "There should be no 03 file");
        }

        [TestMethod()]
        // If user only wants one file, check that they only get one file.
        public void CheckFileDoesNotRoll()
        {
            RollingLogFile rollingLogFile = new RollingLogFile(folder, "auxtest", 1, 50);

            WriteToFile(rollingLogFile, 40);
            WriteToFile(rollingLogFile, 50);
            WriteToFile(rollingLogFile, 60);
            Assert.IsFalse(File.Exists(folder + "/auxtest-01.log"), "File should not have rolled");

            // Each write will trigger "rollover", so the file should only contain the last write.
            Assert.AreEqual(60 + PADDING, new FileInfo(folder + "/auxtest.log").Length,
                "Base log file has wrong size");
        }
        
        private char _fill = 'A';

        private void WriteToFile(RollingLogFile rollingLogFile, int size)
        {
            StringBuilder sb = new StringBuilder(size);
            for (int i = 0; i < size; i++)
            {
                sb.Append(_fill);
            }

            _fill++;
            rollingLogFile.Log(sb.ToString());
        }
    }
}