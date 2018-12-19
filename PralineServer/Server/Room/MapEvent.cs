using System;
using System.Collections.Generic;
using System.Threading;
using LiteNetLib;
using PA.Networking.Types;

namespace PA.Networking.Server.Room {
    public class MapEvent {
        public delegate void EventDelegate();

        private struct Event {
            public uint Timer;
            public uint Duration;
            public EventDelegate Delegate;

            public Event(uint timer, uint duration, EventDelegate del) {
                Timer = timer;
                Duration = duration;
                Delegate = del;
            }
        }

        private struct RadiusDescription {
            public float StartRadius;
            public float EndRadius;
            public uint Duration;

            public RadiusDescription(float startRadius, float endRadius, uint duration) {
                StartRadius = startRadius;
                EndRadius = endRadius;
                Duration = duration;
            }
        }

        public Dictionary<int, ItemGenerator.Item> ItemList;
        public Dictionary<int, EnigmasGenerator.Enigmas> EnigmasList;
        public MyNetworkServer<Player.InGamePlayer> Server;

        private static readonly Dictionary<int, RadiusDescription> RadiusZone = new Dictionary<int, RadiusDescription> {
            {1, new RadiusDescription(800, 400, 60)},
            {2, new RadiusDescription(400, 100, 60)}
        };

        private List<Event> _events;

        private int _currentZoneIndex;
        private float _currentZoneRadius;
        private DateTime _start;
        private Thread _trainThread;
        private Thread _plasmaThread;
        private bool _stop;

        public MapEvent() {
            _events = new List<Event> {
                new Event(0, 0, StartTrain),
                new Event(120, 0, OpenAccessZone),
                new Event(0, 0, StartingPlasmaZone),
                //new Event(0, RadiusZone[1].Duration, MovingPlasmaZone),
                new Event(360, 0, OpenAccessZone),
                new Event(540, 0, StartingPlasmaZone),
                //new Event(540, RadiusZone[2].Duration, MovingPlasmaZone)
            };

            _currentZoneIndex = 1;
            _currentZoneRadius = RadiusZone[1].StartRadius;

            _start = DateTime.Now;
            _stop = false;
        }

        ~MapEvent() {
            _stop = true;
            _trainThread.Join();
            _plasmaThread.Join();
        }

        /// <summary>
        /// Call each second
        /// </summary>
        public void Update() {
            var diff = DateTime.Now - _start;
            double time = diff.TotalSeconds;
            
            foreach (var e in _events) {
                if (time >= e.Timer && time < e.Timer + e.Duration + 1)
                    e.Delegate.Invoke();
            }
        }

        public void Stop() {
            _stop = true;
            _trainThread.Join();
        }

        public bool CheckPlayerInPlasma(Vector3 pos) {
            return pos.magnitude >= _currentZoneRadius;
        }

        private void TrainFunction() {
            NetworkWriter writer;
            float radiusPercent = 0;
            float sleepTimeSeconds = 1f / 100;
            int sleepTimeMiliseconds = (int) (1000 * sleepTimeSeconds);
            float radiusStep = 100f / (60000f / sleepTimeMiliseconds);

            while (radiusPercent < 100) {
                if (_stop)
                    return;
                
                radiusPercent += radiusStep;

                writer = new NetworkWriter(InGameProtocol.UDPServerToClient.MoveTrain);
                writer.Put(radiusPercent);
                Server.SendAll(writer, DeliveryMethod.Unreliable);

                Thread.Sleep(sleepTimeMiliseconds);
            }

            writer = new NetworkWriter(InGameProtocol.TCPServerToClient.StopTrain);
            Server.SendAll(writer, DeliveryMethod.ReliableOrdered);
        }

        private void StartTrain() {
            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.StartTrain);
            Server.SendAll(writer, DeliveryMethod.ReliableOrdered);

            _trainThread = new Thread(TrainFunction);
            _trainThread.Start();
        }

        private void OpenAccessZone() {
            foreach (var e in EnigmasList) {
                var enigma = EnigmasList[e.Key];
                if (enigma.Zone == _currentZoneIndex)
                    enigma.EnigmaAccessOpened = true;
            }

            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.EnigmaAccessOpened);
            writer.Put(_currentZoneIndex + 1);
            Server.SendAll(writer, DeliveryMethod.ReliableOrdered);
        }

        private void PlasmaFunction() {
            var zone = RadiusZone[_currentZoneIndex];
            
            float radiusPercent = 0;
            float sleepTimeSeconds = 1f / 100;
            int sleepTimeMiliseconds = (int) (1000 * sleepTimeSeconds);
            float radiusStep = 100f / ((zone.Duration * 1000f) / sleepTimeMiliseconds);
            float radiusDiff = zone.StartRadius - zone.EndRadius;
            
            while (radiusPercent < 100) {
                if (_stop)
                    return;
                radiusPercent += radiusStep;
                
                _currentZoneRadius = zone.StartRadius - (radiusDiff * (radiusPercent / 100f));
                
                var writer = new NetworkWriter(InGameProtocol.UDPServerToClient.MovingPlasma);
                writer.Put(_currentZoneIndex);
                writer.Put(_currentZoneRadius);
                Server.SendAll(writer, DeliveryMethod.Unreliable);
                
                Thread.Sleep(sleepTimeMiliseconds);
            }
            _currentZoneIndex++;
        }
        
        private void StartingPlasmaZone() {
            var writer = new NetworkWriter(InGameProtocol.TCPServerToClient.StartPlasma);
            writer.Put(_currentZoneIndex);
            Server.SendAll(writer, DeliveryMethod.ReliableOrdered);

            _plasmaThread = new Thread(PlasmaFunction);
            _plasmaThread.Start();
        }
    }
}