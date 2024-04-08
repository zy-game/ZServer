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

namespace ZServer;

public class KCPServer
{
    private static bool isSsl = false;
    private static IChannel boundChannel;
    private static IEventLoopGroup bossGroup;
    private static Dictionary<IChannelId, Client> clients = new();
    public static bool Started { get; private set; }

    internal static async Task RunServerAsync(ushort port)
    {
        bossGroup = new MultithreadEventLoopGroup(1);
        try
        {
            var bootstrap = new Bootstrap();
            bootstrap.Group(bossGroup); //.Channel<SocketDatagramChannel>();
            bootstrap.ChannelFactory(() => new SocketDatagramChannel(AddressFamily.InterNetwork));
            bootstrap
                .Option(ChannelOption.SoBacklog, 100)
                .Handler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;
                    pipeline.AddLast(new LoggingHandler("SRV-CONN"));
                    pipeline.AddLast(new IdleStateHandler(0, 0, 20));
                    pipeline.AddLast("echo", new UDPServerHandler());
                }));

            boundChannel = await bootstrap.BindAsync(port);
            Started = true;
            App.Log("KCP Server Start");
        }
        catch (Exception)
        {
            await Shutdown();
            throw;
        }
    }

    internal static async Task Shutdown()
    {
        if (boundChannel != null)
        {
            await boundChannel.CloseAsync();
        }

        if (bossGroup != null)
        {
            await bossGroup.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
        }

        Started = false;
    }

    public static void Send(IChannelId id, int opcode, byte[] bytes)
    {
        if (clients.TryGetValue(id, out var client) is false)
        {
            return;
        }

        client.Send(Packet.Create(opcode, bytes));
    }

    public static void Broadcast(int opcode, byte[] bytes, params IChannelId[] ids)
    {
        foreach (var id in ids)
        {
            if (clients.TryGetValue(id, out var client) is false)
            {
                continue;
            }

            client.Send(Packet.Create(opcode, bytes));
        }
    }

    class UDPServerHandler : ChannelHandlerAdapter, IWriteable
    {
        private IChannel _channel;
        private EndPoint Recipient;
        private EndPoint Sender;


        public override async void ChannelRead(IChannelHandlerContext context, object message)
        {
            _channel = context.Channel;
            var packet = message as DatagramPacket;
            Recipient = packet.Recipient;
            Sender = packet.Sender;
            if (clients.TryGetValue(context.Channel.Id, out var client) is false)
            {
                clients.Add(context.Channel.Id, client = Client.Create(context.Channel.Id, this));
            }

            Packet frame = Packet.Deserialized(packet.Content.Array);
            App.Log(client.id + "Receive Message:" + frame.opcode);
            IServer server = App.GetServer(client.id);
            if (server is null)
            {
                App.Log("get free server");
                server = await App.GetFreeServer(client.id);
            }

            App.Log(client.id + "execute message:" + frame.opcode);
            await server.OnMessage(client.id, frame.opcode, frame.Data);
            RefPooled.Release(frame);
            if (packet != null)
                packet.Release();
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            App.Log(string.Format("Exception:{}", exception));
            clients.Remove(context.Channel.Id);
            context.CloseAsync();
        }

        public async void WriteMessage(byte[] bytes)
        {
            DatagramPacket packet = new DatagramPacket(Unpooled.WrappedBuffer(bytes), Recipient, Sender);
            await _channel?.WriteAndFlushAsync(packet);
        }
    }
}