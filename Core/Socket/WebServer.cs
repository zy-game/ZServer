using System.Diagnostics;
using System.Net;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Codecs.Protobuf;
using DotNetty.Common.Utilities;
using DotNetty.Handlers.Tls;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using HttpMethod = DotNetty.Codecs.Http.HttpMethod;
using static DotNetty.Codecs.Http.HttpVersion;
using static DotNetty.Codecs.Http.HttpResponseStatus;

namespace ZServer;

public class WebServer
{
    private static IChannel bootstrapChannel;

    public static async Task RunServerAsync(ushort port)
    {
        Console.WriteLine($"\n{RuntimeInformation.OSArchitecture} {RuntimeInformation.OSDescription}"
                          + $"\n{RuntimeInformation.ProcessArchitecture} {RuntimeInformation.FrameworkDescription}"
                          + $"\nProcessor Count : {Environment.ProcessorCount}\n");

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;
        }

        Console.WriteLine($"Server garbage collection : {(GCSettings.IsServerGC ? "Enabled" : "Disabled")}");
        Console.WriteLine($"Current latency mode for garbage collection: {GCSettings.LatencyMode}");
        Console.WriteLine("\n");

        IEventLoopGroup bossGroup = new MultithreadEventLoopGroup(1);
        IEventLoopGroup workGroup = new MultithreadEventLoopGroup();
        X509Certificate2 tlsCertificate = null;
        try
        {
            var bootstrap = new ServerBootstrap();
            bootstrap.Group(bossGroup, workGroup);
            bootstrap.Channel<TcpServerSocketChannel>();
            bootstrap.Option(ChannelOption.SoBacklog, 8192).ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
            {
                IChannelPipeline pipeline = channel.Pipeline;
                if (tlsCertificate != null)
                {
                    pipeline.AddLast(TlsHandler.Server(tlsCertificate));
                }

                pipeline.AddLast(new HttpServerCodec());
                pipeline.AddLast(new HttpObjectAggregator(65536));
                pipeline.AddLast(new WebSocketServerHandler());
            }));

            bootstrapChannel = await bootstrap.BindAsync(IPAddress.Loopback, port);
            Console.WriteLine("Open your web browser and navigate to " + $"http://127.0.0.1:{port}/");
            Console.WriteLine("Listening on " + $"ws://127.0.0.1:{port}/websocket");
        }
        finally
        {
            workGroup.ShutdownGracefullyAsync().Wait();
            bossGroup.ShutdownGracefullyAsync().Wait();
        }
    }

    public static async Task Shutdown()
    {
        await bootstrapChannel.CloseAsync();
    }

    public sealed class WebSocketServerHandler : SimpleChannelInboundHandler<object>
    {
        const string WebsocketPath = "/websocket";

        WebSocketServerHandshaker handshaker;

        protected override void ChannelRead0(IChannelHandlerContext ctx, object msg)
        {
            if (msg is IFullHttpRequest request)
            {
                this.HandleHttpRequest(ctx, request);
            }
            else if (msg is WebSocketFrame frame)
            {
                this.HandleWebSocketFrame(ctx, frame);
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        void HandleHttpRequest(IChannelHandlerContext ctx, IFullHttpRequest req)
        {
            // Handle a bad request.
            if (!req.Result.IsSuccess)
            {
                SendHttpResponse(ctx, req, new DefaultFullHttpResponse(Http11, BadRequest));
                return;
            }

            // Allow only GET methods.
            if (!Equals(req.Method, HttpMethod.Get))
            {
                SendHttpResponse(ctx, req, new DefaultFullHttpResponse(Http11, Forbidden));
                return;
            }


            if ("/favicon.ico".Equals(req.Uri))
            {
                var res = new DefaultFullHttpResponse(Http11, NotFound);
                SendHttpResponse(ctx, req, res);
                return;
            }

            // Handshake
            var wsFactory = new WebSocketServerHandshakerFactory(
                GetWebSocketLocation(req), null, true, 5 * 1024 * 1024);
            this.handshaker = wsFactory.NewHandshaker(req);
            if (this.handshaker == null)
            {
                WebSocketServerHandshakerFactory.SendUnsupportedVersionResponse(ctx.Channel);
            }
            else
            {
                this.handshaker.HandshakeAsync(ctx.Channel, req);
            }
        }

        void HandleWebSocketFrame(IChannelHandlerContext ctx, WebSocketFrame frame)
        {
            // Check for closing frame
            if (frame is CloseWebSocketFrame)
            {
                this.handshaker.CloseAsync(ctx.Channel, (CloseWebSocketFrame)frame.Retain());
                return;
            }

            if (frame is PingWebSocketFrame)
            {
                ctx.WriteAsync(new PongWebSocketFrame((IByteBuffer)frame.Content.Retain()));
                return;
            }

            if (frame is TextWebSocketFrame)
            {
                // Echo the frame
                ctx.WriteAsync(frame.Retain());
                return;
            }

            if (frame is BinaryWebSocketFrame)
            {
                // Echo the frame
                ctx.WriteAsync(frame.Retain());
            }
        }

        static void SendHttpResponse(IChannelHandlerContext ctx, IFullHttpRequest req, IFullHttpResponse res)
        {
            // Generate an error page if response getStatus code is not OK (200).
            if (res.Status.Code != 200)
            {
                IByteBuffer buf = Unpooled.CopiedBuffer(Encoding.UTF8.GetBytes(res.Status.ToString()));
                res.Content.WriteBytes(buf);
                buf.Release();
                HttpUtil.SetContentLength(res, res.Content.ReadableBytes);
            }

            // Send the response and close the connection if necessary.
            Task task = ctx.Channel.WriteAndFlushAsync(res);
            if (!HttpUtil.IsKeepAlive(req) || res.Status.Code != 200)
            {
                task.ContinueWith((t, c) => ((IChannelHandlerContext)c).CloseAsync(),
                    ctx, TaskContinuationOptions.ExecuteSynchronously);
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
        {
            Console.WriteLine($"{nameof(WebSocketServerHandler)} {0}", e);
            ctx.CloseAsync();
        }

        static string GetWebSocketLocation(IFullHttpRequest req)
        {
            bool result = req.Headers.TryGet(HttpHeaderNames.Host, out ICharSequence value);
            Debug.Assert(result, "Host header does not exist.");
            string location = value.ToString() + WebsocketPath;
            return "ws://" + location;
        }
    }
}