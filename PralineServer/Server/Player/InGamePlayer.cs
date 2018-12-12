using System.Collections.Generic;
using LiteNetLib;
using PA.Networking.Types;

namespace PA.Networking.Server.Player {
    public class InGamePlayer : APlayer {
        public struct Throwable {
            public Vector3 Position;
            public Quaternion Rotation;

            public short Type;
            public Throwable(short type) {
                Type = type;
                Position = new Vector3();
                Rotation = new Quaternion();
            }
        }
        
        public Vector3 Position;
        public Quaternion Rotation;

        public short HP;
        public short Shield;

        public uint KillCounter;

        public bool IsAlive;

        public Dictionary<int, Room.ItemGenerator.Item> Inventory;
        public Room.ItemGenerator.Item CurrentItem;

        public Dictionary<int, Throwable> Throwables;
        
        public InGamePlayer(NetPeer peer, int id) : base(peer, id) {
            HP = 100;
            Shield = 0;
            IsAlive = true;
            KillCounter = 0;

            Inventory = new Dictionary<int, Room.ItemGenerator.Item>();
            Throwables = new Dictionary<int, Throwable>();
        }

        public void TakeDamage(short damage) {
            short toHPdamage = damage;
            if (Shield > 0) {
                toHPdamage -= Shield;
                Shield -= damage;
                if (Shield < 0)
                    Shield = 0;
            }

            if (toHPdamage > 0) {
                HP -= toHPdamage;
                if (HP <= 0)
                    IsAlive = false;
            }
        }

        public bool TakeItem(ref Room.ItemGenerator.Item item, int quantity) {
            if (Room.ItemGenerator.IsWeapon(item.Type))
                Inventory.Add(item.ID, item);
            else {
                foreach (var i in Inventory) {
                    var currentItem = i.Value;
                    if (currentItem.Type == item.Type) {
                        item.Quantity -= quantity;
                        currentItem.Quantity += quantity;

                        if (item.Quantity == 0)
                            return true;
                        return false;
                    }
                }
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
            var item = Inventory[itemID];

            if (item.Quantity == quantity)
                Inventory.Remove(itemID);
            else
                item.Quantity -= quantity;
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
    }
}