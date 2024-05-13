using auxmic.mediaUtil;

namespace auxmic.editorExport
{
    public interface IMediaTool
    {
        MediaProperties LoadMetadata(string filename);
    }
}