using System.Net;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace ZServer;

public interface IWriteable
{
    void Write(Client client, byte[] bytes);
}

public class Client : IReference
{
    private IWriteable _channel;
    private DateTime heartbeatTime;
    private int timeoutCount = 0;
    private const int MAX_TIMEOUT_COUNT = 3;

    /// <summary>
    /// 网络链接ID
    /// </summary>
    public int cid { get; private set; }

    /// <summary>
    /// 发送地址
    /// </summary>
    public EndPoint Sender { get; private set; }

    /// <summary>
    /// 接收地址
    /// </summary>
    public EndPoint Recipient { get; private set; }


    /// <summary>
    /// 发送数据
    /// </summary>
    /// <param name="messaged"></param>
    public void Write(byte[] messaged)
    {
        _channel.Write(this, messaged);
    }

    public void Write(int opcde, byte[] bytes)
    {
        Write(Packet.Create(opcde, bytes));
    }

    public void Release()
    {
        _channel = null;
        Recipient = null;
        Sender = null;
        cid = 0;
    }

    public bool Timeout()
    {
        if (DateTime.Now - heartbeatTime < TimeSpan.FromSeconds(20))
        {
            return false;
        }

        timeoutCount++;
        return timeoutCount >= MAX_TIMEOUT_COUNT;
    }

    public void RefreshHeartbeat()
    {
        timeoutCount = 0;
        heartbeatTime = DateTime.Now + TimeSpan.FromSeconds(20);
    }

    public static Client Create(int cid, IWriteable adapter, EndPoint Sender, EndPoint Recipient)
    {
        Client client = RefPooled.Spawner<Client>();
        client.cid = cid;
        client._channel = adapter;
        client.Sender = Sender;
        client.Recipient = Recipient;
        client.heartbeatTime = DateTime.Now;
        return client;
    }
}