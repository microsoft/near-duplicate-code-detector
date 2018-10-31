using System;
using System.Collections.Generic;
using System.Linq;

namespace NearCloneDetector
{
    class SparseVector
    {
        private readonly Dictionary<int, int> _vector = new Dictionary<int, int>();
        
        public void AddElements(IEnumerable<(int Idx, int Count)> elements)
        {
            foreach (var element in elements)
            {
                if (!_vector.TryGetValue(element.Idx, out var count))
                {
                    count = 0;
                }
                _vector[element.Idx] = count + element.Count;
            }
        }

        public double KeyJaccardSimilarity(SparseVector other)
        {
            int sameKeys = 0;
            int numDistinct = other._vector.Count;
            foreach (var key in _vector.Keys)
            {
                if (other._vector.ContainsKey(key))
                {
                    sameKeys++;
                }
                else
                {
                    numDistinct++;
                }
            }
            return ((double)sameKeys) / numDistinct;
        }

        public double JaccardSimilarity(SparseVector other)
        {
            int numerator = 0;
            int denominator = 0;

            foreach(var idx in _vector.Keys.Concat(other._vector.Keys).Distinct())
            {
                if (!_vector.TryGetValue(idx, out var thisIdxCount))
                {
                    thisIdxCount = 0;
                }
                if (!other._vector.TryGetValue(idx, out var otherIdxCount))
                {
                    otherIdxCount = 0;
                }
                numerator += Math.Min(thisIdxCount, otherIdxCount);
                denominator += Math.Max(thisIdxCount, otherIdxCount);
            }
            return denominator==0?0: ((double)numerator / denominator);
        }
    }
}
