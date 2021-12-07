using System;
using System.IO;
using Markdig;
using Markdig.Syntax.Inlines;

namespace BuildLL
{
    public static class DocumentationBuilder
    {
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
        
        static string ResourceText(string rs)
        {
            using (var reader = new StreamReader(typeof(DocumentationBuilder).Assembly.GetManifestResourceStream(rs))) {
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
        static void Convert(string infile, string outfile, string versionString)
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseBootstrap()
                .UseUrlRewriter(FixUrl)
                .Build();
            var input = File.ReadAllText(infile);
            input = input.Replace("$(VERSION)", versionString);
            var innerHTML = Markdown.ToHtml(input, pipeline);
            var template = ResourceText("BuildLL.template.html");
            File.WriteAllText(outfile, template.Replace("$(MARKDOWN)", innerHTML));
        }

        public static void BuildDocs(string sourceDir, string output, string version)
        {
            Directory.CreateDirectory(Path.Combine(output, "assets"));
            //Copy project assets
            if(Directory.Exists(Path.Combine(sourceDir, "assets"))) {
                Copy(Path.Combine(sourceDir, "assets"), Path.Combine(output, "assets"));
            }
            
            //Generate markdown
            foreach (var file in Directory.GetFiles(sourceDir, "*.md"))
            {
                string outPath = Path.Combine(output, Path.ChangeExtension(Path.GetFileName(file), ".html"));
                Console.WriteLine($"{file} -> {outPath}");
                Convert(file, outPath, version);
            }
        }
    }
}