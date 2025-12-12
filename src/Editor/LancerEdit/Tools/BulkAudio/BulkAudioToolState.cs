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
        NodeNameInvalid
    }
    public enum ToolState
    {
        SelectFiles,
        TrimTool,
        Converting,
        ConversionResults,
        Importing,
        ImportResults
    }
    public ToolState CurrentState { get; set; } = ToolState.SelectFiles;
    public string StatusMessage { get; set; } = string.Empty;
    public ErrorTypes ErrorType { get; set; } = ErrorTypes.None;
    public bool IsError { get; set; } = false;

    // For progress display
    public float Progress { get; set; }

    // For deleting entries from the table
    public int DeleteIndex { get; set; } = -1;

    // Modal: which entry is being edited for trim
    public ConversionEntry TrimEditingEntry { get; set; } = null;
    public List<ConversionEntry> ConversionEntries { get; set; } = new();
    public List<ImportEntry> ImportEntries { get; set; } = new();
    public int BackupTrimStart;
    public int BackupTrimEnd;
    public string OutputFolder { get; set; } = "";
}
