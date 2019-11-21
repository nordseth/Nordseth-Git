using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Nordseth.Git
{
    public class Repo
    {
        private const int ShortHashLenght = 7;

        private readonly ObjectParser _objectParser;
        private readonly GitConfigReader _configReader;
        private readonly ObjectReader _objectReader;

        private GitConfig _config;

        public Repo(string path)
        {
            if (path.EndsWith(".git") && Directory.Exists(path))
            {
                RepoPath = path;
            }
            else if (Directory.Exists(Path.Combine(path, ".git")))
            {
                RepoPath = Path.Combine(path, ".git");
            }
            else
            {
                // todo support .git file with gitdir
                throw new InvalidOperationException($"Cannot find folder {path}");
            }

            ValidateGitFolder(RepoPath);

            _objectParser = new ObjectParser();
            _configReader = new GitConfigReader();
            _objectReader = new ObjectReader(RepoPath);
        }

        public string RepoPath { get; }

        public GitConfig LoadConfig()
        {
            using (var configStream = File.OpenRead(Path.Combine(RepoPath, "config")))
            {
                var sections = _configReader.Read(configStream);
                _config = new GitConfig { Sections = sections };
            }

            return _config;
        }

        public IEnumerable<(string name, string hash)> EnumerateRefs(string path = "refs")
        {
            foreach (var f in Directory.EnumerateFiles(Path.Combine(RepoPath, path)))
            {
                yield return ($"{path}/{Path.GetFileName(f)}", File.ReadLines(f).First());
            }

            foreach (var d in Directory.EnumerateDirectories(Path.Combine(RepoPath, path)))
            {
                var dirName = Path.GetFileName(d);
                foreach (var r in EnumerateRefs($"{path}/{dirName}"))
                {
                    yield return r;
                }
            }
        }

        public string FindRef(string refName)
        {
            var filePath = Path.Combine(RepoPath, refName);
            if (File.Exists(filePath))
            {
                return File.ReadLines(filePath).First();
            }
            else
            {
                return null;
            }
        }

        public (string refName, string hash) GetHead()
        {
            var head = File.ReadLines(Path.Combine(RepoPath, "HEAD")).First();
            if (head.StartsWith("ref: "))
            {
                var headRef = head.Substring(5);
                return (headRef, FindRef(headRef));
            }
            else
            {
                return (null, head);
            }
        }

        public Commit GetCommit(string hash)
        {
            var (type, stream) = _objectReader.GetObject(hash);
            if (type == ObjectType.commit)
            {
                return _objectParser.ReadCommit(hash, stream);
            }
            else
            {
                stream.Dispose();
                return null;
            }
        }

        public IEnumerable<Tree> GetTree(string hash)
        {
            var (type, stream) = _objectReader.GetObject(hash);
            if (type == ObjectType.tree)
            {
                return _objectParser.ReadTree(stream).ToList();
            }
            else
            {
                stream.Dispose();
                return null;
            }
        }

        public Stream GetBlob(string hash)
        {
            var (type, stream) = _objectReader.GetObject(hash);
            if (type == ObjectType.blob)
            {
                return stream;
            }
            else
            {
                stream.Dispose();
                return null;
            }
        }

        public Tag GetTag(string hash)
        {
            var (type, stream) = _objectReader.GetObject(hash);
            if (type == ObjectType.commit)
            {
                stream.Dispose();
                return new Tag
                {
                    Commit = hash,
                };
            }
            else if (type == ObjectType.tag)
            {
                return _objectParser.ReadTag(hash, stream);
            }
            else
            {
                stream.Dispose();
                return null;
            }
        }

        public string DescribeCommit(string commitHash)
        {
            var commit = GetCommit(commitHash);
            if (commit == null)
            {
                throw new InvalidOperationException($"{commit} is not a commit");
            }

            var tagRefs = EnumerateRefs("refs/tags");
            var tags = tagRefs.Select(r => GetTag(r.hash))
                .Where(t => t.Name != null)
                .OrderByDescending(t => t.Tagger.When)
                .ToList();

            if (tags.Any())
            {
                int depth = 0;
                var commitsChecked = new HashSet<string>();
                IEnumerable<string> currentCommits = new[] { commitHash };
                while (currentCommits.Any())
                {
                    var tag = tags.FirstOrDefault(t => currentCommits.Any(c => c == t.Commit));
                    if (tag != null)
                    {
                        if (depth == 0)
                        {
                            return tag.Name;
                        }
                        else
                        {
                            return $"{tag.Name}-{depth}-{commitHash.Substring(0, ShortHashLenght)}";
                        }
                    }

                    foreach (var h in currentCommits)
                    {
                        commitsChecked.Add(h);
                    }

                    currentCommits = currentCommits
                        .Select(h => GetCommit(h))
                        .SelectMany(c => c.Parents)
                        .Where(c => !commitsChecked.Contains(c))
                        .ToList();

                    depth++;
                }
            }

            // default to hash of last commit if no tags are found
            return commitHash.Substring(0, ShortHashLenght);
        }

        public GitInfo GetGitInfo(string commitHash = null)
        {
            Commit commit;
            string branch = null;
            if (commitHash == null)
            {
                var (refName, hash) = GetHead();
                commitHash = hash;
                branch = refName;
                commit = GetCommit(hash);
            }
            else
            {
                commit = GetCommit(commitHash);
                if (commit == null)
                {
                    throw new Exception($"Commit {commitHash} not found");
                }
            }

            var info = new GitInfo
            {
                CommitId = commitHash,
                Branch = branch,
                CommitAuthor = $"{commit.Author?.Name} <{commit.Author?.Email}>",
                CommitDate = commit.Author?.When.DateTime.ToString("u"),
                CommitMessage = commit.MessageShort,
            };

            try
            {
                if (_config == null)
                {
                    LoadConfig();
                }

                info.OriginUrl = _config["remote", "origin", "url"]?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                info.CommitDescription = $"ERROR! failed to read origin url: {ex.Message}";
            }

            try
            {
                info.CommitDescription = DescribeCommit(commit.Id);
            }
            catch (Exception ex)
            {
                info.CommitDescription = $"ERROR! failed to describe: {ex.Message}";
            }

            return info;
        }

        private static void ValidateGitFolder(string gitFolder)
        {
            if (!Directory.Exists(Path.Combine(gitFolder, "refs")))
            {
                throw new InvalidOperationException($"Invalid git repo {gitFolder}");
            }

            if (!Directory.Exists(Path.Combine(gitFolder, "objects")))
            {
                throw new InvalidOperationException($"Invalid git repo {gitFolder}");
            }

            if (!File.Exists(Path.Combine(gitFolder, "HEAD")))
            {
                throw new InvalidOperationException($"Invalid git repo {gitFolder}");
            }

            if (!File.Exists(Path.Combine(gitFolder, "config")))
            {
                throw new InvalidOperationException($"Invalid git repo {gitFolder}");
            }
        }
    }
}
