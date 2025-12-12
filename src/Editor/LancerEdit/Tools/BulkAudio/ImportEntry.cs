using System;
using System.Collections.Generic;
using System.Text;

namespace LancerEdit.Tools.BulkAudio
{
    public class ImportEntry
    {
        public string FileName;       
        public string NodeName;       
        public byte[] Data;           
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
