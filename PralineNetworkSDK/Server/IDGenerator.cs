using System;
using System.Collections.Generic;

namespace PA.Networking.Server {
    public class IDGenerator {
        private List<int> _usedId;
        private Random _random;

        private IDGenerator() {
            _usedId = new List<int>();
            _random = new Random();
        }

        private static IDGenerator Instance = new IDGenerator();

        public static IDGenerator getInstance() {
            return Instance;
        }

        public int GenerateUniqueID() {
            int id;

            do {
                id = _random.Next();
            } while (_usedId.Contains(id));

            _usedId.Add(id);
            return id;
        }

        public void RemoveUniqueID(int id) {
            if (_usedId.Contains(id))
                _usedId.Remove(id);
        }

        public void SaveUniqueID(int id) {
            if (!_usedId.Contains(id))
                _usedId.Add(id);
        }
    }
}