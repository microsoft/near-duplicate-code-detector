using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NearCloneDetector
{
    class CloneDetector
    {
        private readonly Dictionary<string, Dictionary<string, SparseVector>> _index = new Dictionary<string, Dictionary<string, SparseVector>>();
        public int NumFiles => _index.Sum(prj => prj.Value.Count);
        private readonly FeatureDictionary _dict = new FeatureDictionary();

        private readonly string _tokensFieldName;
        private readonly string[] _identifyingFields;

        public CloneDetector(string tokensField, string[] entryIdFields)
        {
            _tokensFieldName = tokensField;
            Debug.Assert(entryIdFields.Length > 0);
            _identifyingFields = entryIdFields;
        }

        public readonly Dictionary<string, HashSet<string>> Duplicates = new Dictionary<string, HashSet<string>>();

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void AddDuplicate(string file1, string file2)
        {
            if (!Duplicates.TryGetValue(file1, out var fileDups))
            {
                fileDups = new HashSet<string>();
                Duplicates.Add(file1, fileDups);
            }
            fileDups.Add(file2);
        }

        private static IEnumerable<(string Token, int Count)> Count(IEnumerable<string> tokens)
        {
            var allCounts = new Dictionary<string, int>();
            foreach (var token in tokens)
            {
                if (!allCounts.TryGetValue(token, out var currentCount))
                {
                    currentCount = 0;
                }
                allCounts[token] = currentCount + 1;
            }
            return allCounts.Select(kv => (kv.Key, kv.Value));
        }

        public void BuildIndexForProjects(string tokenizedFilesPath)
        {
            var allFiles = Directory.GetFiles(tokenizedFilesPath, "*.gz")
                .Select(f=> Path.Combine(tokenizedFilesPath, f));
            BuildIndexFromFiles(allFiles);
        }

        public void BuildIndexFromFiles(IEnumerable<string> allFiles)
        {
            foreach (var projectDir in allFiles)
            {
                Console.WriteLine($"Indexing project {projectDir}");
                BuildIndexForProject(projectDir);
            }
        }

        public void BuildIndexForProject(string parsedJsonlPath)
        {
            var projectIndex = new Dictionary<string, SparseVector>();
            _index.Add(parsedJsonlPath, projectIndex);

            using (var stream = new FileStream(parsedJsonlPath, FileMode.Open))
            using (var uncompressed = new GZipStream(stream, CompressionMode.Decompress))
            using (var text = new StreamReader(uncompressed))
            {
                string line = text.ReadLine();
                while (line != null)
                {
                    if (line == "null")
                    {
                        line = text.ReadLine();
                        continue;
                    }
                    var tokenData = JsonConvert.DeserializeObject<IDictionary<string, object>>(line);
                    var tokenCounter = Count(((JArray)tokenData[_tokensFieldName]).Select(t=>t.ToString()));

                    if (tokenCounter.Sum(tc => tc.Count) >= MIN_NUM_TOKENS_FOR_FILE)
                    {
                        var spVect = new SparseVector();
                        spVect.AddElements(tokenCounter.Select(tc => (_dict.AddOrGet(tc.Token), tc.Count)));
                        var entryIdentifier = string.Join(":", _identifyingFields.Select(idf => tokenData[idf].ToString()));
                        projectIndex[entryIdentifier] = spVect;
                    }
                    line = text.ReadLine();
                }
            }
        }

        private IEnumerable<(string Project1, string Project2)> GetAllProjectCombinations()
        {
            var allProjects = _index.Keys.ToArray();
            for (int i = 0; i < allProjects.Length; i++)
            {
                for (int j = i + 1; j < allProjects.Length; j++)
                {
                    yield return (allProjects[i], allProjects[j]);
                }
                yield return (allProjects[i], allProjects[i]);
            }
        }

        public IEnumerable<(string File1, string File2, double JaccardSimilarity, double KeyJacardSimilarity)> FindNearDuplicates(double keyJaccardThreshold, double jaccardThreshold)
        {
            return GetAllProjectCombinations().AsParallel().SelectMany(projs => FindNearDuplicates(keyJaccardThreshold, jaccardThreshold, projs.Project1, projs.Project2));
        }

        private readonly ConcurrentDictionary<string, bool> _alreadyDuplicatedFiles = new ConcurrentDictionary<string, bool>();
        private const int MIN_NUM_TOKENS_FOR_FILE = 20;

        private IEnumerable<(string File1, string File2, double JaccardSimilarity, double KeyJacardSimilarity)> FindNearDuplicates(double keyJaccardThreshold, double jaccardThreshold, string project1, string project2)
        {
            return _index[project1].AsParallel().Where(f => !_alreadyDuplicatedFiles.ContainsKey(f.Key)).SelectMany(fileInProject1 =>
            {
                IEnumerable<(string File1, string File2, double JaccardSimilarity, double KeyJacardSimilarity)> ComputeSimilarity()
                {
                    foreach (var fileInProject2 in _index[project2].Where(f => !_alreadyDuplicatedFiles.ContainsKey(f.Key)))
                    {
                        if (fileInProject1.Key.Equals(fileInProject2.Key))
                        {
                            continue;  // The file is itself
                        }
                        var keyJaccardSimilarity = fileInProject1.Value.KeyJaccardSimilarity(fileInProject2.Value);
                        if (keyJaccardSimilarity < keyJaccardThreshold) continue;

                        var jaccardSimilarity = fileInProject1.Value.JaccardSimilarity(fileInProject2.Value);
                        if (jaccardSimilarity < jaccardThreshold) continue;

                        _alreadyDuplicatedFiles.TryAdd(fileInProject2.Key, true);
                        AddDuplicate(fileInProject1.Key, fileInProject2.Key);
                        yield return (fileInProject1.Key, fileInProject2.Key, jaccardSimilarity, keyJaccardSimilarity);
                    }
                }
                return ComputeSimilarity();
            });
        }
    }
}
