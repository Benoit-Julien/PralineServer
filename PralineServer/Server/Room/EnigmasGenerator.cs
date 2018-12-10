using System;
using System.Collections.Generic;

namespace PA.Networking.Server.Room {
    public class EnigmasGenerator {
        public struct Enigmas {
            public int SpawnIndex;
            public short EnigmaType;
            public int EnigmaID;
            public int Zone;

            public bool EnigmaOpened;
            public bool EnigmaAccessOpened;

            public Enigmas(int spawnIndex, short enigmaType, int zone) {
                SpawnIndex = spawnIndex;
                EnigmaID = IDGenerator.getInstance().GenerateUniqueID();
                EnigmaType = enigmaType;
                Zone = zone;

                EnigmaOpened = false;
                EnigmaAccessOpened = false;
            }
        }

        /// <summary>
        /// Zone ID
        /// Spawn Enigmas number
        /// </summary>
        public static readonly Dictionary<int, int> SpawnEnigmaZoneNumber = new Dictionary<int, int> {
            {1, 4},
            {2, 2}
        };

        public const int EnigmasNumber = 2;

        public Dictionary<int, Enigmas> EnigmasList;

        private Random _random;

        public EnigmasGenerator() {
            EnigmasList = new Dictionary<int, Enigmas>();

            _random = new Random();
        }

        public void Generate() {
            int index = 0;

            foreach (var spawn in SpawnEnigmaZoneNumber) {
                for (int i = 0; i < spawn.Value; i++) {
                    short value = (short) _random.Next(EnigmasNumber);

                    var enigma = new Enigmas(index, value, spawn.Key);
                    EnigmasList.Add(enigma.EnigmaID, enigma);

                    //Console.WriteLine("SpawnIndex = " + enigma.SpawnIndex);
                    //Console.WriteLine("EnigmaID = " + enigma.EnigmaID);
                    //Console.WriteLine("EnigmaType = " + enigma.EnigmaType);
                    //Console.WriteLine("Zone = " + enigma.Zone);

                    index++;
                }
            }
        }
    }
}