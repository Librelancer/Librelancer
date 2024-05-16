using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Syntax.Inlines;
using Pek.Markdig.HighlightJs;
using XmlDocMarkdown.Core;

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
            using (var reader = new StreamReader(typeof(DocumentationBuilder).Assembly.GetManifestResourceStream(rs)))
            {
                return reader.ReadToEnd();
            }
        }

        static string FixUrl(LinkInline link)
        {
            if (link.IsImage || link.IsAutoLink) return link.Url;
            if (link.Url.EndsWith(".md"))
            {
                return Path.ChangeExtension(link.Url, ".html");
            }

            return link.Url;
        }

        static void Convert(string infile, string outfile, string versionString, string root, bool api = false)
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseBootstrap()
                .UseHighlightJs()
                .UseUrlRewriter(FixUrl)
                .UseTableOfContent(tocAction: opt =>
                    {
                        opt.ContainerTag = "div";
                        opt.ContainerId = "toc";
                        opt.OverrideTitle = "Contents";
                        opt.TitleClass = "toc_title";
                    })
                .Build();
            var input = File.ReadAllText(infile).Trim();
            if (input.StartsWith("<!-- API -->"))
                api = true;
            input = input.Replace("$(VERSION)", versionString);
            var apiHTML = api && Path.GetFileNameWithoutExtension(infile).ToLowerInvariant() != "reference"
                ? $" | <a href=\"{root}api/reference.html\">API Reference</a>"
                : "";
            var prefixHTML = !Path.GetFileNameWithoutExtension(infile)
                .Equals("index", StringComparison.OrdinalIgnoreCase)
                ? $"<div><a href=\"{root}index.html\">« Home</a>{apiHTML}</div>"
                : "";
            var innerHTML = prefixHTML + Markdown.ToHtml(input, pipeline);
            var template = ResourceText("BuildLL.template.html");
            File.WriteAllText(outfile, template.Replace("$(MARKDOWN)", innerHTML)
                .Replace("$(ROOT)", root));
        }

        static void GenerateApiDocs(string sourceDir, string output, string version, IEnumerable<string> dlls)
        {
            var settings = new XmlDocMarkdownSettings();
            string objfolder = "./obj/docs";
            foreach (var dll in dlls)
            {
                XmlDocMarkdownGenerator.Generate(dll, "./obj/docs/", settings);
            }
            var builder = new StringBuilder();
            builder.AppendLine("# API Reference");
            builder.AppendLine();
            foreach (var file in Directory.GetFiles(objfolder, "*.md"))
            {
                if (Path.GetFileNameWithoutExtension(file).ToLowerInvariant() == "reference")
                    continue;
                builder.AppendLine($"* [{Path.GetFileNameWithoutExtension(file)}]({Path.GetFileName(file)})");
            }
            builder.AppendLine(File.ReadAllText(Path.Combine(sourceDir, "api/api.txt")));
            File.WriteAllText(Path.Combine(objfolder, "reference.md"), builder.ToString());
            Directory.CreateDirectory(Path.Combine(output, "api"));
            RecurseBuildMd("/home/cmcging/src/Librelancer/obj/docs", "../",
                Path.Combine(output, "api"), version);
        }

        static void RecurseBuildMd(string folder, string root, string outdir, string version, bool api = true)
        {
            foreach (var file in Directory.GetFiles(folder, "*.md"))
            {
                string outPath = Path.Combine(outdir, Path.ChangeExtension(Path.GetFileName(file), ".html"));
                Console.WriteLine($"{file} -> {outPath}");
                Convert(file, outPath, version, root, api);
            }
            root += "../";
            foreach (var dir in Directory.GetDirectories(folder))
            {
                var d = Path.Combine(outdir, Path.GetFileName(dir));
                Directory.CreateDirectory(d);
                RecurseBuildMd(dir, root, d, version, api);
            }
        }

        public static void BuildDocs(string sourceDir, string output, string version, IEnumerable<string> dlls)
        {
            Directory.CreateDirectory(Path.Combine(output, "assets"));
            //Copy project assets
            Console.WriteLine("Copying documentation assets");
            if (Directory.Exists(Path.Combine(sourceDir, "assets")))
            {
                Copy(Path.Combine(sourceDir, "assets"), Path.Combine(output, "assets"));
            }
            Console.WriteLine("Generating HTML");

            GenerateApiDocs(sourceDir, output, version, dlls);
            RecurseBuildMd(sourceDir, "", output, version, false);
        }
    }
}
