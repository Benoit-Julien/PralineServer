using System;
using System.Collections.Generic;

namespace PA.Networking.Server.Room {
    public class ItemGenerator {
        public struct Item {
            public int SpawnIndex;
            public int ID;
            public short Type;
            public short Rarity;
            public int Quantity;

            public Item(int spawnIndex, short type, short rarity = 0) {
                ID = IDGenerator.getInstance().GenerateUniqueID();
                SpawnIndex = spawnIndex;
                Type = type;
                Rarity = rarity;
                Quantity = 1;
            }

            public Item(Item item, int newQuantity) {
                ID = IDGenerator.getInstance().GenerateUniqueID();
                SpawnIndex = item.SpawnIndex;
                Type = item.Type;
                Rarity = item.Rarity;
                Quantity = newQuantity;
            }

            private sealed class IdEqualityComparer : IEqualityComparer<Item> {
                public bool Equals(Item x, Item y) {
                    return x.ID == y.ID;
                }

                public int GetHashCode(Item obj) {
                    return obj.ID;
                }
            }
            private static readonly IEqualityComparer<Item> IdComparerInstance = new IdEqualityComparer();

            public static IEqualityComparer<Item> IdComparer {
                get { return IdComparerInstance; }
            }
        }

        public const int ItemSpawnNumber = 5;
        public const int MaxItemPerSpawn = 4;

        public struct ItemGenerationInfos {
            public float Chance;
            public int MinQuantity;
            public int MaxQuantity;

            public ItemGenerationInfos(float chance, int minQuantity = 1, int maxQuantity = 1) {
                Chance = chance;
                MinQuantity = minQuantity;
                MaxQuantity = maxQuantity;
            }
        }
        public static readonly Dictionary<short, ItemGenerationInfos> ItemChance = new Dictionary<short, ItemGenerationInfos> {
            {ItemTypes.None, new ItemGenerationInfos(0.5f, 0, 0)},

            {ItemTypes.WeaponTypes.HandGun, new ItemGenerationInfos(0.1f)},
            {ItemTypes.WeaponTypes.Revolver, new ItemGenerationInfos(0.05f)},
            {ItemTypes.WeaponTypes.M4, new ItemGenerationInfos(0.1f)},
            {ItemTypes.WeaponTypes.QBZ, new ItemGenerationInfos(0.05f)},
            {ItemTypes.WeaponTypes.SMG, new ItemGenerationInfos(0.1f)},
            {ItemTypes.WeaponTypes.Shotgun, new ItemGenerationInfos(0.2f)},
            {ItemTypes.WeaponTypes.Sniper, new ItemGenerationInfos(0.01f)},
            {ItemTypes.WeaponTypes.RocketLauncher, new ItemGenerationInfos(0.01f)},
            {ItemTypes.WeaponTypes.Minigun, new ItemGenerationInfos(0.01f)},

            {ItemTypes.ThrowableTypes.Grenade, new ItemGenerationInfos(0.1f)},

            {ItemTypes.ConsumableTypes.Bandage, new ItemGenerationInfos(0.3f, 1, 3)},
            {ItemTypes.ConsumableTypes.Medkit, new ItemGenerationInfos(0.05f)},
            {ItemTypes.ConsumableTypes.ShieldPotion, new ItemGenerationInfos(0.1f, 1, 2)},

            {ItemTypes.AmmunitionTypes.LightBullet, new ItemGenerationInfos(0.2f, 30, 100)},
            {ItemTypes.AmmunitionTypes.MediumBullet, new ItemGenerationInfos(0.1f, 30, 70)},
            {ItemTypes.AmmunitionTypes.HeavyBullet, new ItemGenerationInfos(0.05f, 20, 40)},
            {ItemTypes.AmmunitionTypes.ShotgunShell, new ItemGenerationInfos(0.2f, 30, 80)},
            {ItemTypes.AmmunitionTypes.Rocket, new ItemGenerationInfos(0.01f)}
        };

        public static readonly Dictionary<short, short> AmmoCoresponding = new Dictionary<short, short> {
            {ItemTypes.WeaponTypes.HandGun, ItemTypes.AmmunitionTypes.LightBullet},
            {ItemTypes.WeaponTypes.Revolver, ItemTypes.AmmunitionTypes.LightBullet},
            {ItemTypes.WeaponTypes.M4, ItemTypes.AmmunitionTypes.MediumBullet},
            {ItemTypes.WeaponTypes.QBZ, ItemTypes.AmmunitionTypes.MediumBullet},
            {ItemTypes.WeaponTypes.SMG, ItemTypes.AmmunitionTypes.LightBullet},
            {ItemTypes.WeaponTypes.Shotgun, ItemTypes.AmmunitionTypes.ShotgunShell},
            {ItemTypes.WeaponTypes.Sniper, ItemTypes.AmmunitionTypes.HeavyBullet},
            {ItemTypes.WeaponTypes.Minigun, ItemTypes.AmmunitionTypes.LightBullet},
            {ItemTypes.WeaponTypes.RocketLauncher, ItemTypes.AmmunitionTypes.Rocket}
        };

        public const float ChanceToSpawnAmmoWithWeapon = 0.65f;

        public const float OneWeaponSocket = 0.9f;
        public const float TwoWeaponSockets = 0.09f;
        public const float ThreeWeaponSockets = 0.01f;

        public Dictionary<int, Item> ItemList;

        private Random _random;
        private int _maxItemValue;

        public ItemGenerator() {
            _random = new Random();

            float max = 0;
            foreach (var item in ItemChance) {
                max += item.Value.Chance;
            }

            _maxItemValue = (int) (max * 100);

            ItemList = new Dictionary<int, Item>();
        }

        public void Generate() {
            for (int s = 0; s < ItemSpawnNumber; s++) {
                int itemnb = _random.Next(MaxItemPerSpawn);

                for (int i = 0; i < itemnb; i++) {
                    float value = _random.Next(_maxItemValue) / 100f;
                    float current = 0;
                    short itemType = 0;

                    foreach (var item in ItemChance) {
                        current += item.Value.Chance;
                        itemType = item.Key;
                        if (current > value)
                            break;
                    }

                    if (itemType == ItemTypes.None)
                        continue;

                    if (IsWeapon(itemType)) {
                        short rarity = 0;
                        float rarityValue = _random.Next(100) / 100f;

                        if (rarityValue < OneWeaponSocket)
                            rarity = 1;
                        else if (rarityValue < OneWeaponSocket + TwoWeaponSockets)
                            rarity = 2;
                        else if (rarityValue < OneWeaponSocket + TwoWeaponSockets + ThreeWeaponSockets)
                            rarity = 3;

                        var item = new Item(s, itemType, rarity);
                        ItemList.Add(item.ID, item);

                        float spawnAmmoValue = _random.Next(100) / 100f;
                        if (spawnAmmoValue < ChanceToSpawnAmmoWithWeapon) {
                            var info = ItemChance[AmmoCoresponding[itemType]];
                            item = new Item(s, AmmoCoresponding[itemType]);
                            item.Quantity = GenerateQuantity(info.MinQuantity, info.MaxQuantity);
                            ItemList.Add(item.ID, item);
                        }
                    }
                    else if (itemType != ItemTypes.ThrowableTypes.Grenade) {
                        var item = new Item(s, itemType);
                        item.Quantity = GenerateQuantity(ItemChance[itemType].MinQuantity, ItemChance[itemType].MaxQuantity);
                        ItemList.Add(item.ID, item);
                    }
                    else {
                        var item = new Item(s, itemType);
                        ItemList.Add(item.ID, item);
                    }
                }
            }
        }
        
        /// <summary>
        /// Calculate a bezier height Y of a parameter in the range [0..1]
        /// </summary>
        public static float CalculateBezierHeight(float t, float y1, float y2, float y3, float y4)
        {
            float tPower3 = t * t * t;
            float tPower2 = t * t;
            float oneMinusT = 1 - t;
            float oneMinusTPower3 = oneMinusT * oneMinusT * oneMinusT;
            float oneMinusTPower2 = oneMinusT * oneMinusT;
            float Y = oneMinusTPower3 * y1 + (3 * oneMinusTPower2 * t * y2) + (3 * oneMinusT * tPower2 * y3) + tPower3 * y4;
            return Y;
        }

        private int GenerateQuantity(int min, int max) {
            float rand = (float) _random.Next(1000) / 1000;
            while (true) {
                float rand2 = (float) _random.Next(1000) / 1000;
                float eval = CalculateBezierHeight(rand2, 1, 0.41f, 0.12f, 0);

                if (rand <= eval)
                    return min + (int) ((max - min) * rand2);
            }
        }

        public static bool IsWeapon(short type) {
            return type == ItemTypes.WeaponTypes.HandGun ||
                   type == ItemTypes.WeaponTypes.Revolver ||
                   type == ItemTypes.WeaponTypes.M4 ||
                   type == ItemTypes.WeaponTypes.QBZ ||
                   type == ItemTypes.WeaponTypes.SMG ||
                   type == ItemTypes.WeaponTypes.Shotgun ||
                   type == ItemTypes.WeaponTypes.Sniper ||
                   type == ItemTypes.WeaponTypes.Minigun ||
                   type == ItemTypes.WeaponTypes.RocketLauncher;
        }
    }
}