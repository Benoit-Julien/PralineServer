using System;
using System.Threading;

namespace PA.Server {
    internal class Program {
        public static void Main(string[] args) {
            ServerManager manager = new ServerManager();

            manager.StartServer();
            while (!Console.KeyAvailable) {
                manager.Update();
                Thread.Sleep(15);
            }
            manager.StopServer();
        }
    }
}