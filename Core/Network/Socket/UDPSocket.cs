using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Handlers.Logging;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using fec;
using LiteNetLib;
using LiteNetLib.Utils;

namespace ZGame.Networking;

public interface INetClient : IReference
{
    int cid { get; }
    bool isConnected { get; }
    void Write(byte[] bytes);
}

public class UDPSocket : INetEventListener, INetLogger, ISocket
{
    private NetManager _netServer;
    private List<UDPClient> _clients;
    private Action<INetClient> _OnConnect;
    private Action<INetClient> _OnDisconnect;
    private Action<INetClient, MSGPacket> _OnRecvie;

    /// <summary>
    /// 链接成功回调
    /// </summary>
    public event Action<INetClient> OnConnect
    {
        add { _OnConnect += value; }
        remove { _OnConnect -= value; }
    }

    /// <summary>
    /// 收到链接发送的消息回调
    /// </summary>
    public event Action<INetClient, MSGPacket> OnRecvie
    {
        add { _OnRecvie += value; }
        remove { _OnRecvie -= value; }
    }

    /// <summary>
    /// 链接断开回调
    /// </summary>
    public event Action<INetClient> OnDisconnect
    {
        add { _OnDisconnect += value; }
        remove { _OnDisconnect -= value; }
    }

    class UDPClient : INetClient
    {
        public NetPeer peer;
        public int cid => peer.GetHashCode();
        public bool isConnected => peer.ConnectionState == ConnectionState.Connected;

        public void Write(byte[] bytes)
        {
            if (isConnected is false)
            {
                return;
            }

            peer.Send(bytes, DeliveryMethod.ReliableOrdered);
        }

        public void Release()
        {
            peer = null;
        }
    }

    public void OnStart(ushort port)
    {
        NetDebug.Logger = this;
        _clients = new List<UDPClient>();
        _netServer = new NetManager(this);
        _netServer.Start(port);
        _netServer.BroadcastReceiveEnabled = true;
        _netServer.UpdateTime = 15;
    }

    public void OnUpdate()
    {
        _netServer.PollEvents();
    }

    public void Release()
    {
        NetDebug.Logger = null;
        if (_netServer != null)
            _netServer.Stop();
    }

    void INetEventListener.OnPeerConnected(NetPeer peer)
    {
        if (_clients.Exists(x => x.peer == peer))
        {
            return;
        }

        AppCore.Log("[SERVER] We have new peer " + peer);
        UDPClient client = RefPooled.Alloc<UDPClient>();
        client.peer = peer;
        _clients.Add(client);
        _OnConnect?.Invoke(client);
    }

    void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketErrorCode)
    {
        AppCore.Log("[SERVER] error " + socketErrorCode);
    }

    void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        if (messageType == UnconnectedMessageType.Broadcast)
        {
            // App.Log("[SERVER] Received discovery request. Send discovery response");
            NetDataWriter resp = new NetDataWriter();
            resp.Put(1);
            _netServer.SendUnconnectedMessage(resp, remoteEndPoint);
        }
    }

    void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
    }

    void INetEventListener.OnConnectionRequest(ConnectionRequest request)
    {
        request.AcceptIfKey("sample_app");
    }

    void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        AppCore.Log("[SERVER] peer disconnected " + peer + ", info: " + disconnectInfo.Reason);
        UDPClient client = _clients.Find(x => x.peer == peer);

        if (client is null)
        {
            return;
        }

        _OnDisconnect?.Invoke(client);
        _clients.Remove(client);
        RefPooled.Free(client);
    }

    void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        UDPClient client = _clients.Find(x => x.peer == peer);
        if (client is null)
        {
            AppCore.Log("[SERVER] Received message from unknown peer " + peer);
            return;
        }


        var packet = MSGPacket.Deserialize(reader.GetRemainingBytes());
        // AppCore.Log($"[{client.cid}] recvie opcode:{packet.opcode} status:{packet.status} message:{packet.message} lenght:{packet.Data.Length}");
        _OnRecvie?.Invoke(client, packet);
    }

    void INetLogger.WriteNet(NetLogLevel level, string str, params object[] args)
    {
        AppCore.Log(string.Format(str, args));
    }
}