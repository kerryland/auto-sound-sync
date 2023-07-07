using System;

namespace auxmic.logging
{
    public interface AuxMicLog
    {
        public void Log(string message, Exception e = null);
    }
}