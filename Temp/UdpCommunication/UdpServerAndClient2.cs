/*
using ENet;
using LiteNetLib;
using LiteNetLib.Utils;
using StateOfTheArtTablePublisher;
using System.Diagnostics;
using System.Net;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static UdpCommunication.UdpServer;

namespace UdpCommunication
{
    public class UdpClient2 : IClientCommunication
    {
        public event Action<byte[], long> NewMessageArrived;

        private EventBasedNetListener listener;
        private NetManager client;
        private NetPeer peer;

        private byte[] _data;

        private ushort _port;
        private string _ip;
        
        private ILogger _logger;

        public UdpClient2(ushort port, string ip, ILogger logger)
        {
            _port = port;
            _ip = ip;
            _logger = logger;
        }

        public void ConnectToServer()
        {
            TryConnectToPeer();

            Task.Factory.StartNew(HandleEvents, TaskCreationOptions.LongRunning);
        }

        private void TryConnectToPeer()
        {
            peer = client.Connect(_ip, _port, "Client Ran");
        }

        public void Init(long maxMessageSize)
        {
            _data = new byte[maxMessageSize];

            listener = new EventBasedNetListener();
            client = new NetManager(listener);
            client.Start();            

            listener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) =>
            {
                var length = reader.AvailableBytes;
                var id = (uint)peer.Id;
                _logger?.Info($"Packet received from - {id}, Channel ID: {channel}, Data length: {length}");
                reader.GetBytes(_data, length);
                reader.Recycle();
                NewMessageArrived?.Invoke(_data, length);
                Interlocked.Increment(ref messagesReceivedCounter);
            };

            listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                _logger?.Info($"Client disconnected - {peer}");
                TryConnectToPeer();
            };

            listener.PeerConnectedEvent += peer =>
            {
                _logger?.Info($"Client connected - {peer}");
            };

            TryConnectToPeer();
        }
        
        private NetDataWriter writer = new NetDataWriter();
        public void SendDataToServer(byte[] data, long count)
        {
            writer.Put(data, 0, (int)count);            
            peer.Send(writer, DeliveryMethod.ReliableOrdered);
            _logger?.Info("Packet sent to - ID: " + peer.Id + ", Data length: " + count);
        }

        public void HandleEvents()
        {
            while (true)
            {
                client.PollEvents();
                Thread.Sleep(15);
            }
        }
        
        private static long messagesReceivedCounter = 0;
        public long GetTotalReceivedMessages()
        {
            return messagesReceivedCounter;
        }

        public void Dispose()
        {
            client.DisconnectAll();
        }
    }

    public class UdpServer2 : IServerCommunication
    {
        public event Action<uint> OnNewClient;
        public event Action<uint, byte[], long> OnNewClientMessage;

        private Dictionary<uint, NetPeer> connectedClients = new Dictionary<uint, NetPeer>();

        private EventBasedNetListener listener;
        private NetManager server;


        private byte[] _data;
        
        private ushort _port;
        private int _maxClients;

        private ILogger _logger;

        public UdpServer2(ushort port, int maxClients, ILogger logger)
        {            
            _port = port;
            _maxClients = maxClients;
            _logger = logger;
        }

        public List<uint> GetClients()
        {
            List<uint> clients = new List<uint>();

            lock (connectedClients)
                clients = connectedClients.Keys.ToList();

            return clients;
        }

        public void Init(long maxMessageSize)
        {
            _data = new byte[maxMessageSize];
            
            var port = _port;
            var maxClients = _maxClients;

            listener = new EventBasedNetListener();
            server = new NetManager(listener);
            server.Start(port);

            listener.ConnectionRequestEvent += request =>
            {
                if (server.ConnectedPeersCount < _maxClients)
                    request.AcceptIfKey("Client Ran");
                else
                    request.Reject();
            };

            listener.PeerConnectedEvent += peer =>
            {
                _logger?.Info($"Client connected - {peer}");

                lock (connectedClients)
                    connectedClients[(uint)peer.Id] = peer;
                OnNewClient.Invoke((uint)peer.Id);
            };

            listener.PeerDisconnectedEvent += (peer, disconnectInfo) =>
            {
                _logger?.Info($"Client disconnected - {peer}");
                lock (connectedClients)
                    connectedClients.Remove((uint)peer.Id);
            };

            listener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) =>
            {
                var length = reader.AvailableBytes;
                var id = (uint) peer.Id;
                _logger?.Info($"Packet received from - {id}, Channel ID: {channel}, Data length: {length}");
                reader.GetBytes(_data, length);
                reader.Recycle();

                OnNewClientMessage?.Invoke(id, _data, length);
            };

            Task.Factory.StartNew(HandleEvents, TaskCreationOptions.LongRunning);
        }

        private static long messagesSentCounter = 0;
        public long GetTotalSentMessages()
        {
            return messagesSentCounter;
        }

        private NetDataWriter writer = new NetDataWriter();
        public void SendDataToClient(uint clientId, byte[] data, long count)
        {
            writer.Put(data, 0, (int)count);
            var peer = connectedClients[clientId];
            peer.Send(writer, DeliveryMethod.ReliableOrdered);

            _logger?.Info("Packet sent to - ID: " + clientId + ", Data length: " + count);
            Interlocked.Increment(ref messagesSentCounter);
        }

        public void HandleEvents()
        {
            while (true)
            {
                server.PollEvents();
                Thread.Sleep(15);
            }
        }

        public void Dispose()
        {
            server.Stop();
        }
    }
}
*/