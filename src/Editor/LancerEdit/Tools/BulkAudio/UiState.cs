using System;

namespace LancerEdit.Tools.BulkAudio;

public class UiState
{
    public enum ErrorTypes
    {
        None = 0,
        NoInputs,
        NoOutput,
        ConversionError
    }

    public string StatusMessage { get; set; } = string.Empty;
    public ErrorTypes ErrorType { get; set; } = ErrorTypes.None;
    public bool IsError { get; set; } = false;

    // For progress display
    public float Progress { get; set; }

    // For deleting entries from the table
    public int DeleteIndex { get; set; } = -1;

    // Modal: which entry is being edited for trim
    public BulkAudioEntry TrimEditingEntry { get; set; } = null;
}
