using System.Collections.Generic;
using LiteNetLib;

namespace PA.Server.Player {
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

        public List<Room.ItemGenerator.Item> Inventory;
        public Room.ItemGenerator.Item CurrentItem;

        public Dictionary<int, Throwable> Throwables;
        
        public InGamePlayer(NetPeer peer, int id) : base(peer, id) {
            HP = 100;
            Shield = 0;
            IsAlive = true;
            KillCounter = 0;

            Inventory = new List<Room.ItemGenerator.Item>();
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

        public void TakeItem(Room.ItemGenerator.Item item) {
            Inventory.Add(item);
        }

        public Room.ItemGenerator.Item DropItem(int itemID, int quantity) {
            var item = Inventory[itemID];
            
            if (item.Quantity == quantity)
                Inventory.Remove(item);
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
                Inventory.Remove(item);
            else
                item.Quantity -= 1;
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