namespace LancerEdit.Audio;

public class ConversionResulta
{
    public bool Success { get; set; }
    public string OutputPath { get; set; }
    public string ErrorMessage { get; set; }

    public static ConversionResulta Ok(string outputPath) =>
        new ConversionResulta { Success = true, OutputPath = outputPath };

    public static ConversionResulta Fail(string error) =>
        new ConversionResulta { Success = false, ErrorMessage = error };
}
