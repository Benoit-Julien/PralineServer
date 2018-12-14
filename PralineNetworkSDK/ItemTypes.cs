namespace PA {
    public static class ItemTypes {
        public enum ItemEnum {
            HandGun = 1,
            Revolver = 2,
            M4 = 3,
            QBZ = 4,
            SMG = 5,
            Shotgun = 6,
            Sniper = 7,
            RocketLauncher = 8,
            Minigun = 9,
            
            Grenade = 101,
            
            Bandage = 1001,
            Medkit = 1002,
            ShieldPotion = 1003,
            
            LightBullet = 10001,
            MediumBullet = 10002,
            HeavyBullet = 10003,
            ShotgunShell = 10004,
            Rocket = 10005
        }
        
        public const short None = 0;
        
        public class WeaponTypes {
            public const short HandGun = 1;
            public const short Revolver = 2;
            public const short M4 = 3;
            public const short QBZ = 4;
            public const short SMG = 5;
            public const short Shotgun = 6;
            public const short Sniper = 7;
            public const short RocketLauncher = 8;
            public const short Minigun = 9;
        }

        public class ThrowableTypes {
            public const short Grenade = 101;
        }

        public class ConsumableTypes {
            public const short Bandage = 1001;
            public const short Medkit = 1002;
            public const short ShieldPotion = 1003;
        }

        public class AmmunitionTypes {
            public const short LightBullet = 10001;
            public const short MediumBullet = 10002;
            public const short HeavyBullet = 10003;
            public const short ShotgunShell = 10004;
            public const short Rocket = 10005;
        }
    }
}