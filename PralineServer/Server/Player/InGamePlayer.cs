using System;
using System.Collections.Generic;
using LiteNetLib;
using PA.Networking.Types;

namespace PA.Networking.Server.Player {
    public class InGamePlayer : APlayer {
        public class Throwable {
            public Vector3 Position;
            public Quaternion Rotation;

            public short Type;

            public Throwable(short type) {
                Type = type;
                Position = new Vector3();
                Rotation = new Quaternion();
            }
        }

        public class Rocket {
            public Vector3 Position;
            public Quaternion Rotation;
        }

        public static readonly int BandageValue = 15;
        public static readonly int BandageCap = 75;
        public static readonly int MedkitValue = 50;
        public static readonly int HPCap = 100;

        public static readonly int ShieldValue = 50;
        public static readonly int ShieldCap = 100;

        public Vector3 Position;
        public Quaternion Rotation;

        public int HP;
        public int Shield;

        public uint KillCounter;

        public bool IsAlive;

        public Dictionary<int, Room.ItemGenerator.Item> Inventory;
        public Room.ItemGenerator.Item CurrentItem;

        public Dictionary<int, Throwable> Throwables;
        public Dictionary<int, Rocket> Rockets;

        public InGamePlayer(NetPeer peer, int id) : base(peer, id) {
            HP = 100;
            Shield = 0;
            IsAlive = true;
            KillCounter = 0;

            Inventory = new Dictionary<int, Room.ItemGenerator.Item>();
            Throwables = new Dictionary<int, Throwable>();
            Rockets = new Dictionary<int, Rocket>();
            
            Position = new Vector3();
            Rotation = new Quaternion();
        }

        public void TakeDamage(short damage, bool affectShield) {
            int toHPdamage = damage;
            if (affectShield && Shield > 0) {
                toHPdamage = Math.Max(toHPdamage - Shield, 0);
                Shield = Math.Max(Shield - damage, 0);
            }

            HP = Math.Max(HP - toHPdamage, 0);

            if (HP <= 0)
                IsAlive = false;
        }

        public bool TakeItem(ref Room.ItemGenerator.Item item, int quantity) {
            if (Room.ItemGenerator.IsWeapon(item.Type)) {
                Logger.WriteLine("Player {0} : Take weapon {1}", Id, item.ID);
                Inventory.Add(item.ID, item);
            }
            else {
                foreach (var i in Inventory) {
                    if (i.Value.Type == item.Type) {
                        Logger.WriteLine("Player {0} : Take item {1} and stack into {2}", Id, item.ID, i.Key);

                        item.Quantity -= quantity;
                        i.Value.Quantity += quantity;
                        if (item.Quantity <= 0)
                            return true;
                        return false;
                    }
                }

                Logger.WriteLine("Player {0} : Take item {1} and added in his inventory", Id, item.ID);
                Inventory.Add(item.ID, item);
            }

            return true;
        }

        public Room.ItemGenerator.Item DropItem(int itemID, int quantity) {
            var item = Inventory[itemID];

            if (item.Quantity == quantity)
                Inventory.Remove(itemID);
            else {
                item.Quantity -= quantity;
                item = new Room.ItemGenerator.Item(item, quantity);
            }

            return item;
        }

        public void SwitchCurrentItem(int itemID) {
            CurrentItem = Inventory[itemID];
        }

        public void UseItem(int itemID, int quantity) {
            Logger.WriteLine("Player {0} : use item {1} quantity = {2}", Id, itemID, quantity);
            var item = Inventory[itemID];

            switch (item.Type) {
                case ItemTypes.ConsumableTypes.Bandage:
                    HP = Math.Min(HP + BandageValue, BandageCap);
                    break;
                case ItemTypes.ConsumableTypes.Medkit:
                    HP = Math.Min(HP + MedkitValue, HPCap);
                    break;
                case ItemTypes.ConsumableTypes.ShieldPotion:
                    Shield = Math.Min(Shield + ShieldValue, ShieldCap);
                    break;
            }

            Logger.WriteLine("Player {0} : HP = {1} Shiled = {2}", Id, HP, Shield);

            item.Quantity -= quantity;
            if (item.Quantity <= 0)
                Inventory.Remove(itemID);
        }

        public void Throwing(int itemID, short type, int index) {
            if (Throwables.ContainsKey(index))
                return;
            UseItem(itemID, 1);

            var throwable = new Throwable(type);
            Throwables.Add(index, throwable);
        }

        public void ThrowableMove(int index, Vector3 position, Quaternion rotation) {
            if (!Throwables.ContainsKey(index))
                return;
            var throwable = Throwables[index];
            throwable.Position = position;
            throwable.Rotation = rotation;
        }

        public void ThrowingEnd(int index) {
            if (!Throwables.ContainsKey(index))
                return;
            Throwables.Remove(index);
        }

        public void RocketStart(int index) {
            if (Rockets.ContainsKey(index))
                return;

            var rocket = new Rocket();
            Rockets.Add(index, rocket);
        }

        public void RocketMove(int index, Vector3 position, Quaternion rotation) {
            if (!Rockets.ContainsKey(index))
                return;
            var rocket = Rockets[index];
            rocket.Position = position;
            rocket.Rotation = rotation;
        }

        public void RocketEnd(int index) {
            if (!Rockets.ContainsKey(index))
                return;
            Rockets.Remove(index);
        }
    }
}