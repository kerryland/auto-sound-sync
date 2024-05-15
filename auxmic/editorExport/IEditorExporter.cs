using System.Collections.Generic;
using System.IO;
using auxmic.mediaUtil;

namespace auxmic.editorExport
{
    public interface IEditorExporter
    {
        void Export(Clip master, IList<Clip> clips, bool wantSecondaryAudio, TextWriter output);
    }
}