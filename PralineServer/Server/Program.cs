using System;
using System.Threading;
using System.Collections.Generic;

namespace PA.Networking.Server {
    internal class Program {
        private delegate bool CommandDelegate(ServerManager manager, string[] args);

        private static readonly Dictionary<string, CommandDelegate> Commands = new Dictionary<string, CommandDelegate> {
            {"help", Help},
            {"exit", Exit},
            {"printRooms", PrintRooms},
            {"createRoom", CreateRoom},
            {"room", RoomFunc}
        };

        public static void Main() {
            ServerManager manager = new ServerManager();
            bool stop = false;

            Thread updater = new Thread(() => {
                while (!stop) {
                    try {
                        manager.Update();
                    }
                    catch (Exception e) {
                        Logger.WriteLine(e);
                        throw;
                    }

                    Thread.Sleep(15);
                }
            });

            manager.StartServer();
            updater.Start();
            while (true) {
                string line = Console.ReadLine().Trim();

                if (line.Length > 0) {
                    string[] tab = line.Split(' ', '\t', '\v');
                    string command = tab[0];

                    for (var i = 0; i < tab.Length; i++)
                        tab[i] = tab[i].Trim();

                    if (Commands.ContainsKey(command)) {
                        if (!Commands[command].Invoke(manager, tab))
                            break;
                    }
                    else
                        Console.WriteLine("error: Unknown command {0}.", command);
                }
            }

            stop = true;
            updater.Join();
            manager.StopServer();
        }

        private static bool Exit(ServerManager manager, string[] args) {
            return false;
        }

        private static readonly string HelpText =
            "Praline Server help :\n" +
            "\texit : Stop the server.\n" +
            "\thelp : Show this message\n" +
            "\tprintRooms : Print all room that currently running with its informations.\n" +
            "\tcreateRoom : Create a room --> arguments : [maxPlayer = 32, minPlayerToStart = 12, TimeBeforeStart = 60]\n" +
            "\troom : Access to some functions to interact with the room --> arguments : [index, [command]].\n" +
            "\t\tstart : Start the room.\n" +
            "\t\tdelete : Delete the room.";

        private static bool Help(ServerManager manager, string[] args) {
            Console.WriteLine(HelpText);
            return true;
        }

        private static bool PrintRooms(ServerManager manager, string[] args) {
            Console.Write("Rooms {0} : [", manager.Rooms.Count);
            foreach (var room in manager.Rooms) {
                Console.Write("\n\tRoom {0} : {1}/{2}", room.Value.Id, room.Value.PlayerCount, room.Value.MaxPlayer);
                Console.Write(room.Value.GameStarted ? " --> Game Started !!" : "");
            }

            if (manager.Rooms.Count > 0) Console.Write("\n");
            Console.WriteLine("]");
            return true;
        }

        private static bool CreateRoom(ServerManager manager, string[] args) {
            int maxPlayer = 32;
            int minPlayerToStart = 12;
            int timeBeforeStart = 60;
            int index = 1;

            if (index < args.Length)
                maxPlayer = int.Parse(args[index++]);
            if (index < args.Length)
                minPlayerToStart = int.Parse(args[index++]);
            if (index < args.Length)
                timeBeforeStart = int.Parse(args[index++]);

            Console.WriteLine("Create a new room with {0} maximum player, {1} minimum player to start the game and {2} seconds before the game start.",
                maxPlayer,
                minPlayerToStart,
                timeBeforeStart);

            var room = manager.CreateRoom(maxPlayer, minPlayerToStart, timeBeforeStart);
            Console.WriteLine("Room created : id = " + room.Id);

            return true;
        }

        private static bool RoomFunc(ServerManager manager, string[] args) {
            if (args.Length < 2) {
                Console.WriteLine("error: not enough arguments are provided.");
                Help(manager, args);
                return true;
            }

            int index;
            if (!int.TryParse(args[1], out index)) {
                Console.WriteLine("error: cannot get index.");
                return true;
            }

            if (index < 0 || index >= manager.Rooms.Count) {
                Console.WriteLine("error: index out in range.");
                return true;
            }

            if (args.Length > 2) {
                switch (args[2]) {
                    case "start":
                        manager.StartRoom(index);
                        break;
                    case "delete":
                        manager.DeleteRoom(index);
                        break;
                }
            }
            else
                manager.PrintRoom(index);

            return true;
        }
    }
}