using System;
using System.Collections.Generic;

namespace LancerEdit.Tools.BulkAudio;

public class BulkAudioToolState
{
    public enum ErrorTypes
    {
        None = 0,
        NoInputs,
        NoOutput,
        ConversionError,
        NodeNameInvalid,
        NoImports
    }
    public enum ToolState
    {
        SelectFiles,
        TrimTool,
        Converting,
        ConversionResults,
        Importing
    }
    public ToolState CurrentState = ToolState.SelectFiles;
    public string StatusMessage = string.Empty;
    public ErrorTypes ErrorType = ErrorTypes.None;
    public bool IsError = false;

    // For progress display
    public float Progress { get; set; }

    // For deleting entries from the table
    public int DeleteIndex = -1;

    // Modal: which entry is being edited for trim
    public ConversionEntry TrimEditingEntry = null;
    public List<ConversionEntry> ConversionEntries = new();
    public List<ImportEntry> ImportEntries = new();
    public int BackupTrimStart;
    public int BackupTrimEnd;
    public string OutputFolder = "";
}
