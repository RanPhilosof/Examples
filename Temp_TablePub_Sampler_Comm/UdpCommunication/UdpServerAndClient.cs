using ENet;
using StateOfTheArtTablePublisher;
using System.Diagnostics;
using System.Net;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static UdpCommunication.UdpServer;

namespace UdpCommunication
{
    public class UdpClient : IClientCommunication
    {
        public event Action<byte[], long> NewMessageArrived;

        private Host client;
        private Address address;
        private Event netEvent;
        private Peer peer;
        private byte[] _data;

        private ushort _port;
        private string _ip;
        
        private ILogger _logger;

        public UdpClient(ushort port, string ip, ILogger logger)
        {
            Library.Initialize();

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
            peer = client.Connect(address, 5);
            // TODO: Remove it later
            peer.Timeout(2, 500, 1000);
            peer.PingInterval(250);
        }

        public void Init(long maxMessageSize)
        {
            _data = new byte[maxMessageSize];

            client = new Host();

            address = new Address();

            address.SetHost(_ip);
            address.Port = _port;
            client.Create();
            client.SetBandwidthLimit(12_500_000, 500_000);
        }

        public void SendDataToServer(byte[] data, long count)
        {
            messageToSend = new MessageToSend() { data = data, count = count };
            slimSemaphore.Wait();
        }

        private SemaphoreSlim slimSemaphore = new SemaphoreSlim(0);

        private MessageToSend messageToSend;

        public void HandleEvents()
        {
            while (true)
            {
                if (messageToSend != null)
                {
                    var data = messageToSend.data;
                    var count = messageToSend.count;

                    Packet packet = default(Packet);
                    packet.Create(data, (int)count, PacketFlags.Reliable);

                    if (!peer.Send(0, ref packet))
                        _logger.Error("Client Failed To Send Message");

                    messageToSend = null;

                    slimSemaphore.Release();
                }

                if (client.CheckEvents(out netEvent) <= 0)                    
                    if (client.Service(1, out netEvent) <= 0) //if (client.Service(15, out netEvent) <= 0)
                        continue;

                switch (netEvent.Type)
                {
                    case EventType.None:
                        break;

                    case EventType.Connect:
                        _logger?.Info("Client connected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                        break;

                    case EventType.Disconnect:
                        _logger?.Info("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                        TryConnectToPeer();
                        break;

                    case EventType.Timeout:
                        _logger?.Info("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                        TryConnectToPeer();
                        break;

                    case EventType.Receive:
                        _logger?.Info("Packet received from - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);
                        netEvent.Packet.CopyTo(_data);
                        var length = netEvent.Packet.Length;
                        netEvent.Packet.Dispose();
                        NewMessageArrived?.Invoke(_data, length);
                        Interlocked.Increment(ref messagesReceivedCounter);
                        break;
                }
                
                client.Flush();
            }
        }

        private static long messagesReceivedCounter = 0;
        public long GetTotalReceivedMessages()
        {
            return messagesReceivedCounter;
        }
    }

    public class UdpServer : IServerCommunication
    {
        public event Action<Int128> OnNewClient;
        public event Action<Int128, byte[], long> OnNewClientMessage;

        private Dictionary<Int128, Peer> connectedClients = new Dictionary<Int128, Peer>();

        private Host server;
        private Address address;
        private Event netEvent;
        private byte[] _data;
        
        private ushort _port;
        private int _maxClients;

        private ILogger _logger;

        public UdpServer(ushort port, int maxClients, ILogger logger)
        {
            Library.Initialize();

            _port = port;
            _maxClients = maxClients;
            _logger = logger;
        }

        public List<Int128> GetClients()
        {
            List<Int128> clients = new List<Int128>();

            lock (connectedClients)
                clients = connectedClients.Keys.ToList();

            return clients;
        }

        public void Init(long maxMessageSize)
        {
            _data = new byte[maxMessageSize];
            
            var port = _port;
            var maxClients = _maxClients;

            server = new Host();            

            address = new Address();

            address.Port = port;
            server.Create(address, 5); // maxClients);
            server.SetBandwidthLimit(500_000, 12_500_000);
            
            Task.Factory.StartNew(HandleEvents, TaskCreationOptions.LongRunning);
        }

        private static long messagesSentCounter = 0;
        public long GetTotalSentMessages()
        {
            return messagesSentCounter;
        }

        private SemaphoreSlim slimSemaphore = new SemaphoreSlim(0);
        public void SendDataToClient(Int128 clientId, byte[] data, long count)
        {
            messageToSend = new MessageToSend() { clientId = clientId, data = data, count = count };
            slimSemaphore.Wait();
        }

        private MessageToSend messageToSend;
        public class MessageToSend
        {
            public Int128 clientId;
            public byte[] data;
            public long count;
        }

        public void HandleEvents()
        {
            while (true)
            {
                if (messageToSend != null)
                {
                    var clientId = messageToSend.clientId;
                    var data = messageToSend.data;
                    var count = messageToSend.count;

                    Packet packet = default(Packet);
                    var peer = connectedClients[clientId];
                    packet.Create(data, (int)count, PacketFlags.Reliable);
                    if (!peer.Send(0, ref packet))
                        _logger.Error("Server Failed To Send Message");
                    else
                        _logger?.Info("Packet sent to - ID: " + clientId + ", Data length: " + count);

                    Interlocked.Increment(ref messagesSentCounter);
                    messageToSend = null;

                    slimSemaphore.Release();
                }

                if (server.CheckEvents(out netEvent) <= 0)                    
                    if (server.Service(1, out netEvent) <= 0) //if (server.Service(15, out netEvent) <= 0)
                        continue;

                switch (netEvent.Type)
                {
                    case EventType.None:
                        break;

                    case EventType.Connect:
                        _logger?.Info("Client connected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                        lock (connectedClients)
                            connectedClients[netEvent.Peer.ID] = netEvent.Peer;
                        OnNewClient.Invoke(netEvent.Peer.ID);
                        break;

                    case EventType.Disconnect:
                        _logger?.Info("Client disconnected - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                        lock (connectedClients)
                            connectedClients.Remove(netEvent.Peer.ID);
                        break;

                    case EventType.Timeout:
                        _logger?.Info("Client timeout - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP);
                        lock (connectedClients)
                            connectedClients.Remove(netEvent.Peer.ID);
                        break;

                    case EventType.Receive:
                        _logger?.Info("Packet received from - ID: " + netEvent.Peer.ID + ", IP: " + netEvent.Peer.IP + ", Channel ID: " + netEvent.ChannelID + ", Data length: " + netEvent.Packet.Length);

                        netEvent.Packet.CopyTo(_data);
                        var length = netEvent.Packet.Length;
                        netEvent.Packet.Dispose();
                        OnNewClientMessage?.Invoke(netEvent.Peer.ID, _data, length);
                        break;
                }

                server.Flush();
            }
        }

        public void Dispose()
        {
            server.Dispose();
        }
    }
}
