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

    class UDPServerHandler : ChannelHandlerAdapter, IWriteable
    {
        private IChannel _channel;

        public override async void ChannelRead(IChannelHandlerContext context, object message)
        {
            _channel = context.Channel;
            var datagramPacket = message as DatagramPacket;
            int cid = datagramPacket.Sender.GetHashCode();
            Packet packet = Packet.Deserialized(datagramPacket.Content.Array);
            EndPoint sender = datagramPacket.Sender;
            EndPoint Recipient = datagramPacket.Recipient;
            App.HandleClientMessaged(cid, packet, sender, Recipient, this);
            if (datagramPacket != null)
            {
                datagramPacket.Release();
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            App.Log(string.Format("Exception:{}", exception));
            // clients.RemoveAll(x => x.cid == client.cid);
            context.CloseAsync();
        }

        public async void Write(Client client, byte[] bytes)
        {
            App.Log(string.Format($"{client.cid} | SENDTO | {client.Sender}"));
            DatagramPacket packet = new DatagramPacket(Unpooled.WrappedBuffer(bytes), client.Recipient, client.Sender);
            await _channel?.WriteAndFlushAsync(packet);
        }
    }
}