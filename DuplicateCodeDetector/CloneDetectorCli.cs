using DocoptNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NearCloneDetector
{
    class CloneDetectorCli
    {
        private const string usage = @"Near Clone Detector.

    Usage:
      CloneDetectorCli [options] (--dir=<folder> | --input=<file>) <output-file-prefix>

    Options:
      -h --help                      Show this screen.
      --dir=<path>                   Directory where .jsonl.gz files live.
      --input=<path>                 The path to the input .jsonl.gz file.
      --id-fields=<fields>           A colon (:)-separated list of names of fields that form the identity of each entry [default: filename].
      --tokens-field=<name>          The name of the field containing the tokens of the code [default: tokens].
      --key-jaccard-threshold=<val>  The Jaccard similarity threshold for token-sets [default: 0.8].
      --jaccard-threshold=<val>      The Jaccard similarity threshold for token multisets [default: 0.7].

    ";

            static void Main(string[] args)
        {
            var arguments = new Docopt().Apply(usage, args, version: "Near Clone Detector", exit: true);
            DetectClones(arguments);            
        }



        public static void DetectClones(IDictionary<string, ValueObject> arguments)
        {
            var cd = new CloneDetector(arguments["--tokens-field"].ToString(), arguments["--id-fields"].ToString().Split(':'));
            if (arguments.TryGetValue("--dir", out var dataDirectory) && dataDirectory != null)
            {
                cd.BuildIndexForProjects(dataDirectory.ToString());
            }
            else if (arguments.TryGetValue("--input", out var dataFile) && dataFile != null)
            {
                cd.BuildIndexFromFiles(new[] { dataFile.ToString() });
            }
            else
            {
                throw new Exception("Either --dir or --input need to be provided.");
            }
                
            Console.WriteLine($"[{DateTime.Now}] Searching for near duplicates...");
            var startTime = DateTime.Now;
            using (var writer = new StreamWriter(arguments["<output-file-prefix>"].ToString() + ".log"))
            {
                foreach (var (File1, File2, JaccardSimilarity, KeyJacardSimilarity) in
                    cd.FindNearDuplicates(double.Parse(arguments["--key-jaccard-threshold"].ToString()),
                                          double.Parse(arguments["--jaccard-threshold"].ToString())))
                {
                    Console.WriteLine($"Near duplicate: ({File1}-->{File2}) [scores: {JaccardSimilarity: #.##}, {KeyJacardSimilarity: #.#}]");
                    writer.WriteLine($"{File1},{File2},{JaccardSimilarity},{KeyJacardSimilarity}");
                }
            }

            var elapsedTime = DateTime.Now - startTime;
            Console.WriteLine($"Finished looking for duplicates in {cd.NumFiles} files.");
            Console.WriteLine($"Duplicate search took {elapsedTime}.");

            var cloneGroups = new CloneGroups(cd.Duplicates.SelectMany(c => c.Value.Select(f => (c.Key, f))));
            cloneGroups.SaveToJson(arguments["<output-file-prefix>"].ToString() + ".json");
        }
    }
}
