using System;
using System.IO;
using System.Security;
using Markdig;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DocumentationMSBuild
{
    public class DocumentationTask : Task
    {
        public string SourceDir { get; set; }
        public string OutputDir { get; set; }
        
        public override bool Execute()
        {
            Log.LogMessage(MessageImportance.High, "Building documentation");
            Directory.CreateDirectory(OutputDir);
            Log.LogMessage(MessageImportance.High, OutputDir);
            
            
            Directory.CreateDirectory(Path.Combine(OutputDir, "assets"));
            //Copy template assets
            var asm = typeof(DocumentationTask).Assembly;
            foreach (var item in asm.GetManifestResourceNames())
            {
                int idx = item.IndexOf("assets.");
                if (idx != -1)
                {
                    var filename = item.Substring(idx + "assets.".Length);
                    using (var output = File.Create(Path.Combine(OutputDir, "assets", filename)))
                    {
                        using (var asset = asm.GetManifestResourceStream(item))
                        {
                            asset.CopyTo(output);
                        }
                    }
                }
            }
            //Copy project assets
            if(Directory.Exists(Path.Combine(SourceDir, "assets"))) {
                foreach (var file in Directory.GetFiles(Path.Combine(SourceDir, "assets")))
                {
                    string outPath = Path.Combine(OutputDir, Path.GetFileName(file));
                    File.Copy(file, outPath);
                }
            }
            //Generate markdown
            foreach (var file in Directory.GetFiles(SourceDir, "*.md"))
            {
                string outPath = Path.Combine(OutputDir, Path.ChangeExtension(Path.GetFileName(file), ".html"));
                Log.LogMessage(MessageImportance.High, $"{file} -> {outPath}");
                Convert(file, outPath);
            }
            return true;
        }

        static string ResourceText(string rs)
        {
            using (var reader = new StreamReader(typeof(DocumentationTask).Assembly.GetManifestResourceStream(rs))) {
                return reader.ReadToEnd();
            }
        }
        
        static void Convert(string infile, string outfile)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().UseBootstrap().Build();
            var innerHTML = Markdown.ToHtml(File.ReadAllText(infile), pipeline);
            var template = ResourceText("DocumentationMSBuild.template.html");
            File.WriteAllText(outfile, template.Replace("$(MARKDOWN)", innerHTML));
        }
    }
}