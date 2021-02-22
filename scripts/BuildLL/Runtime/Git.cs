using System;
using static BuildLL.Runtime;
using System.IO;
using System.Linq;
using LibGit2Sharp;

namespace BuildLL
{
    public static class Git
    {
        public static string ShaTip(string dir)
        {
            static string Sha_Bash(string dir)
            {
                return Bash($"cd {Quote(Path.GetFullPath(dir))} && git rev-parse HEAD", false);
            }
            static string Sha_LibGitSharp(string dir)
            {
                using (var repo = new Repository(dir))
                {
                    var c = repo.Commits.First();
                    return c.Sha;
                }
            }
            if (IsWindows) return Sha_LibGitSharp(dir);
            else return Sha_Bash(dir);
        }
    }
}