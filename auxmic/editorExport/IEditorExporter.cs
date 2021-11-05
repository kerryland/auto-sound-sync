using System.Collections.Generic;
using System.IO;

namespace auxmic.editorExport
{
    public interface IEditorExporter
    {
        void Export(Clip master, IList<Clip> clips, TextWriter output);
    }
}