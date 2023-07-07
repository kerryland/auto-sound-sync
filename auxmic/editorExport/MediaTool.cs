using System;
using NAudio.MediaFoundation;

namespace auxmic.editorExport
{
    // TODO: Move to mediaUtil package
    public class MediaTool : IMediaTool
    {
        public bool IsVideo(string filename)
        {
            MediaFoundationApi.Startup();
            try
            {
                MediaFoundationInterop.MFCreateSourceReaderFromURL(filename, null, out var pReader);
                pReader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_FIRST_VIDEO_STREAM, true);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                MediaFoundationApi.Shutdown();
            }
        }
    }
}