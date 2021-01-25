using System;
using System.IO;
using System.Security;
using Markdig;
using Markdig.Syntax.Inlines;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace DocumentationMSBuild
{
    public class DocumentationTask : Task
    {
        public string SourceDir { get; set; }
        public string OutputDir { get; set; }
        
        public string VersionString { get; set; }
        
        [Output]
        public ITaskItem[] Items { get; set; }
        
        public static void Copy(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        
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
                Copy(Path.Combine(SourceDir, "assets"), Path.Combine(OutputDir, "assets"));
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

        static string FixUrl(LinkInline link)
        {
            if (link.IsImage || link.IsAutoLink) return link.Url;
            if (link.Url.EndsWith(".md")) {
                return Path.ChangeExtension(link.Url, ".html");
            }
            return link.Url;
        }
        void Convert(string infile, string outfile)
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseBootstrap()
                .UseUrlRewriter(FixUrl)
                .Build();
            var input = File.ReadAllText(infile);
            input = input.Replace("$(VERSION)", VersionString);
            var innerHTML = Markdown.ToHtml(input, pipeline);
            var template = ResourceText("DocumentationMSBuild.template.html");
            File.WriteAllText(outfile, template.Replace("$(MARKDOWN)", innerHTML));
        }
    }
}