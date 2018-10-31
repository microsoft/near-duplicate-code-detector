using System;
using System.IO;
using System.Linq;

namespace NearCloneDetector
{
    class CloneDetectorCli
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Usage detect <rootDir> <clonesOutputFilePrefix>");
                return;
            }
            var rootDir = args[1];
            var clonesOutputFilePrefix = args[2];

            if (args[0] == "detect")
            {
                DetectClones(rootDir, clonesOutputFilePrefix);
            }
            else
            {
                throw new NotSupportedException($"Unsupported option {args[0]}");
            }
        }



        public static void DetectClones(string rootDir, string clonesOutputFilePrefix,
            double keyJaccardSimilarityThreshold = 0.8, double jaccardSimilarityThreshold = 0.7)
        {
            var cd = new CloneDetector();
            cd.BuildIndexForProjects(rootDir);

            Console.WriteLine($"[{DateTime.Now}] Searching for near duplicates...");
            var startTime = DateTime.Now;
            using (var writer = new StreamWriter(clonesOutputFilePrefix + ".txt"))
            {
                foreach (var (File1, File2, JaccardSimilarity, KeyJacardSimilarity) in
                    cd.FindNearDuplicates(keyJaccardSimilarityThreshold, jaccardSimilarityThreshold))
                {
                    Console.WriteLine($"Near duplicate: ({File1}-->{File2}) [scores: {JaccardSimilarity: #.##}, {KeyJacardSimilarity: #.#}]");
                    writer.WriteLine($"{File1},{File2},{JaccardSimilarity},{KeyJacardSimilarity}");
                }
            }

            var elapsedTime = DateTime.Now - startTime;
            Console.WriteLine($"Finished looking for duplicates in {cd.NumFiles} files.");
            Console.WriteLine($"Duplicate search took {elapsedTime}.");

            var cloneGroups = new CloneGroups(cd.Duplicates.SelectMany(c => c.Value.Select(f => (c.Key, f))));
            cloneGroups.SaveToJson(clonesOutputFilePrefix + ".json");
        }
    }
}
