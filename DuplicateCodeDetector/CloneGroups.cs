using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NearCloneDetector
{
    public class CloneGroups
    {
        public readonly Dictionary<string, HashSet<string>> FileToCloneSet = new Dictionary<string, HashSet<string>>();
        public readonly List<HashSet<string>> CloneSets = new List<HashSet<string>>();

        private void AddElementNonTransitive(string baseFile, string targetFile)
        {
            if (!FileToCloneSet.TryGetValue(baseFile, out var file1Clones))
            {
                file1Clones = new HashSet<string>();
                FileToCloneSet.Add(baseFile, file1Clones);
            }
            file1Clones.Add(targetFile);
        }

        public CloneGroups(IEnumerable<(string File1, string File2)> clonePairs)
        {
            foreach (var (f1, f2) in clonePairs)
            {
                AddElementNonTransitive(f1, f2);
                AddElementNonTransitive(f2, f1);
            }
            Console.WriteLine($"Found {FileToCloneSet.Count} files that are cloned.");
            var numCloneClusters = MakeCloneSetTransitive();
            Console.WriteLine($"Number of unique clone clusters {numCloneClusters}");

            var duplicationFactors = CloneSets.Select(c => c.Count).ToList();

            Console.WriteLine($"Avg Duplication Factor: {duplicationFactors.Average()}");
            duplicationFactors.Sort();
            double median;
            int midpoint = duplicationFactors.Count / 2;
            if (duplicationFactors.Count % 2 == 0)
            {
                median = (duplicationFactors[midpoint] + duplicationFactors[midpoint + 1]) / 2;
            }
            else
            {
                median = duplicationFactors[midpoint];
            }
            Console.WriteLine($"Median Duplication Factor: {median}");
        }

        private int MakeCloneSetTransitive()
        {
            var filesToVisit = new HashSet<string>(FileToCloneSet.Keys);
            int numCloneSets = 0;

            while (filesToVisit.Count > 0)
            {
                var cloneSet = new HashSet<string>() { filesToVisit.First() };
                int lastCloneSetSize;

                do
                {
                    lastCloneSetSize = cloneSet.Count;
                    cloneSet = new HashSet<string>(cloneSet.SelectMany(c => FileToCloneSet[c]).Union(cloneSet));
                }
                while (lastCloneSetSize != cloneSet.Count);

                numCloneSets += 1;
                CloneSets.Add(cloneSet);
                foreach (var f in cloneSet)
                {
                    FileToCloneSet[f] = cloneSet;
                    filesToVisit.Remove(f);
                }
            }
            return numCloneSets;
        }

        public void SaveToJson(string filename)
        {
            File.WriteAllText(filename, JsonConvert.SerializeObject(CloneSets));
        }
    }
}
