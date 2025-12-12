using System;
using System.Collections.Generic;
using System.Text;

namespace LancerEdit.Tools.BulkAudio
{
    public class ImportEntry
    {
        public string OriginalPath;
        public string ConvertedPath;
        public string FileName;       
        public string NodeName;       
        public byte[] Data;           

        public bool IsVersionLocked = false;
        public bool IsActionLocked = false;
        public bool IsNodeNameLocked = false;
        public ImportVersion Version;
        public ImportAction Action;

    }

    public enum ImportAction
    {
        Ignore,
        Import
    }

    public enum ImportVersion
    {
        Original,
        Converted
    }
}
