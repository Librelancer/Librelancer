using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Syntax.Inlines;
using Microsoft.VisualBasic;
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

        private static Regex markdownRegex = new Regex(@"(.*)(\.md)(#[A-Za-z0-9\-]*)?$");

        static string FixUrl(LinkInline link)
        {
            if (link.IsImage || link.IsAutoLink) return link.Url;
            return markdownRegex.Replace(link.Url, match => $"{match.Groups[1]}.html{match.Groups[3]}");
        }

        record Document(string title, string href, string content);

        private static Regex xmlRegex = new Regex("<!--(.*)-->");

        static void Convert(string infile, string lnk, string outfile, string versionString, string root, List<Document> documents, bool api = false)
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
                ? $"<div><a href=\"{root}index.html\">« Home</a> | <a href=\"{root}search.html\">Search</a>{apiHTML}</div>"
                : $"<a href=\"{root}search.html\">Search</a>";
            var innerHTML = prefixHTML + Markdown.ToHtml(input, pipeline);
            var content = Markdown.ToPlainText(xmlRegex.Replace(input, ""));
            var title = Path.GetFileNameWithoutExtension(lnk);
            documents.Add(new(title, lnk, content));
            var template = ResourceText("BuildLL.template.html");
            File.WriteAllText(outfile, template.Replace("$(MARKDOWN)", innerHTML)
                .Replace("$(ROOT)", root));
        }

        static void GenerateApiDocs(string sourceDir, string output, string version, List<Document> alldocs, IEnumerable<string> dlls)
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
            RecurseBuildMd(objfolder, "api", "../",
                Path.Combine(output, "api"), version, alldocs);
        }

        static string LnkCombine(string a, string b)
        {
            if (a == "")
                return b;
            return a + "/" + b;
        }

        static void RecurseBuildMd(string folder, string lnkPath, string root, string outdir, string version, List<Document> alldocs, bool api = true)
        {
            foreach (var file in Directory.GetFiles(folder, "*.md"))
            {
                string ashtml = Path.ChangeExtension(Path.GetFileName(file), ".html");
                string outPath = Path.Combine(outdir, ashtml);
                Console.WriteLine($"{file} -> {outPath}");
                Convert(file, LnkCombine(lnkPath, ashtml),outPath, version, root, alldocs, api);
            }
            root += "../";
            foreach (var dir in Directory.GetDirectories(folder))
            {
                var f = Path.GetFileName(dir);
                var d = Path.Combine(outdir, Path.GetFileName(dir));
                Directory.CreateDirectory(d);
                RecurseBuildMd(dir, LnkCombine(lnkPath, f), root, d, version, alldocs, api);
            }
        }

        public static void BuildDocs(string sourceDir, string output, string version, IEnumerable<string> dlls)
        {
            var docs = new List<Document>();
            Directory.CreateDirectory(Path.Combine(output, "assets"));
            Directory.CreateDirectory("obj/docs");
            //Copy project assets
            Console.WriteLine("Copying documentation assets");
            if (Directory.Exists(Path.Combine(sourceDir, "assets")))
            {
                Copy(Path.Combine(sourceDir, "assets"), Path.Combine(output, "assets"));
            }
            Console.WriteLine("Generating HTML");

            GenerateApiDocs(sourceDir, output, version, docs, dlls);
            RecurseBuildMd(sourceDir, "", "", output, version, docs, false);

            var searchPage = File.ReadAllText(Path.Combine(sourceDir, "search.html"));
            var builder = new StringBuilder();
            builder.Append("pagesIndex = ");
            builder.Append(JsonSerializer.Serialize(docs));
            builder.Append(";");
            searchPage = searchPage.Replace("$(INDEX)", builder.ToString());
            File.WriteAllText(Path.Combine(output, "search.html"), searchPage);
        }
    }
}
