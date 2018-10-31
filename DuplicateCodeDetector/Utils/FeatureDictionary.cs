using System.Collections.Generic;

namespace NearCloneDetector
{
    class FeatureDictionary
    {
        private readonly List<string> _idToToken = new List<string>();
        private readonly Dictionary<string, int> _tokenToId = new Dictionary<string, int>();

        public int AddOrGet(string token)
        {
            if(_tokenToId.TryGetValue(token, out var id))
            {
                return id;
            }
            id = _idToToken.Count;
            _idToToken.Add(token);
            _tokenToId.Add(token, id);
            return id;
        }

        public int Get(string token)
        {
            return _tokenToId[token];
        }

        public string Get(int id)
        {
            return _idToToken[id];
        }
    }
}
