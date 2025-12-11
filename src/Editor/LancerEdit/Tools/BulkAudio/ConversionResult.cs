namespace LancerEdit.Tools.BulkAudio;

public class ConversionResult
{
    public bool Success { get; set; }
    public string OutputPath { get; set; }
    public string ErrorMessage { get; set; }

    public static ConversionResult Ok(string outputPath) =>
        new ConversionResult { Success = true, OutputPath = outputPath };

    public static ConversionResult Fail(string error) =>
        new ConversionResult { Success = false, ErrorMessage = error };
}
