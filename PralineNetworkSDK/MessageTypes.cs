namespace PA {
    public static class GlobalProtocol {
        public static class ClientToServer {
            public const short ConnectionConfirm = 1000;
            public const short PlayerName = 1001;
            public const short ConnectToRoom = 1002;
        }

        public static class ServerToClient {
            public const short Error = 1000;
            public const short ConnectionConfirm = 1001;
            public const short PlayerNameChanged = 1002;
            public const short ConnectToRoom = 1003;
        }

        public static class ErrorTypes { }
    }

    public static class InGameProtocol {
        public class TCPClientToServer {
            public const short ConnectionConfirm = 1000;
            public const short QuitRoom = 1001;
            public const short Jump = 1002;
            public const short Crouch = 1003;
            public const short DropTrain = 1004;
            public const short Reloading = 1005;
            public const short EnigmaOpened = 1006;
            public const short OpenCrate = 1007;
            public const short TakeItem = 1008;
            public const short DropItem = 1009;
            public const short SwitchItem = 1010;
            public const short SwitchKnife = 1011;
            public const short UseItem = 1012;
            public const short Shoot = 1013;
            public const short HitPlayer = 1014;
            public const short StartThrowing = 1015;
            public const short Throwing = 1016;
            public const short ThrowableEnd = 1017;
            public const short RocketStart = 1018;
            public const short RocketEnd = 1019;
        }

        public static class TCPServerToClient {
            public const short Registered = 1000;
            public const short ListConnectedPlayer = 1001;
            public const short ConectedToRoom = 1002;
            public const short PlayerQuit = 1003;
            public const short Counter = 1004;
            public const short ItemList = 1005;
            public const short EnigmasList = 1006;
            public const short GameStart = 1007;
            public const short StartTrain = 1008;
            public const short StopTrain = 1009;
            public const short DropTrain = 1010;
            public const short EnigmaAccessOpened = 1011;
            public const short EnigmaOpened = 1012;
            public const short OpenCrate = 1013;
            public const short StartPlasma = 1014;
            public const short MovingPlasma = 1015;
            public const short Jump = 1016;
            public const short Crouch = 1017;
            public const short Reloading = 1018;
            public const short TakeItem = 1019;
            public const short DropItem = 1020;
            public const short SwitchItem = 1021;
            public const short SwitchKnife = 1022;
            public const short UseItem = 1023;
            public const short Shoot = 1024;
            public const short HitPlayer = 1025;
            public const short StartThrowing = 1026;
            public const short Throwing = 1027;
            public const short ThrowableEnd = 1028;
            public const short RocketStart = 1029;
            public const short RocketEnd = 2030;
            public const short PlayerKill = 1031;
            public const short PlayerWin = 1032;
        }

        public static class UDPClientToServer {
            public const short Movement = 10001;
            public const short Turn = 10002;
            public const short ThrowableMove = 10003;
            public const short RocketMove = 10004;
        }

        public static class UDPServerToClient {
            public const short MoveTrain = 10001;
            public const short Movement = 10002;
            public const short Turn = 10003;
            public const short ThrowableMove = 10004;
            public const short RocketMove = 10005;
        }
    }
}