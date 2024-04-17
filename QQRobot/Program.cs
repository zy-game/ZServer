// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.WebSockets;
using Newtonsoft.Json;
using TouchSocket.Core;
using TouchSocket.Http;
using TouchSocket.Http.WebSockets;
using TouchSocket.Sockets;
using HttpClient = System.Net.Http.HttpClient;

Console.WriteLine("Hello, World!");
HttpClient client = new HttpClient();
client.DefaultRequestHeaders.Add("Authorization", $"Bot {RobotSetting.app_id}.{RobotSetting.token}");
var response = await client.GetAsync(RobotSetting.open_api + "/gateway");
response.EnsureSuccessStatusCode();
dynamic result = JsonConvert.DeserializeObject<dynamic>(await response.Content.ReadAsStringAsync());
Console.WriteLine(result.url);
object s = default;
int heartbeat_interval = 0;
ClientWebSocket webSocket = new ClientWebSocket();
WebSocketClient myWSClient = new WebSocketClient();
myWSClient.Received = OnReceive;
myWSClient.Handshaked = Authentication;

async Task OnReceive(WebSocketClient client, WSDataFrameEventArgs args)
{
    var eventArgs = args.DataFrame;
    Console.WriteLine(eventArgs.ToText());
    dynamic msg = JsonConvert.DeserializeObject<dynamic>(eventArgs.ToText());
    Opcode opcode = (Opcode)msg.op;
    switch (opcode)
    {
        case Opcode.Hello:
            heartbeat_interval = (int)msg.d.heartbeat_interval;
            Task.Factory.StartNew(OnStartHeartbeat);
            break;
        case Opcode.Dispatch:
            s = msg.s;
            Console.WriteLine("Robot Ready!!!!");
            break;
        case Opcode.Heartbeat_S:
            Task.Factory.StartNew(OnStartHeartbeat);
            break;
    }
}

async Task Authentication(WebSocketClientBase client, HttpContextEventArgs args)
{
    await myWSClient.SendAsync(JsonConvert.SerializeObject(new
    {
        op = (int)Opcode.Identify,
        d = new
        {
            token = $"Bot {RobotSetting.app_id}.{RobotSetting.token}",
            intents = 0 | EventName.GUILDS | EventName.PUBLIC_GUILD_MESSAGES | EventName.GUILD_MEMBERS,
            shard = new[] { 0, 1 },
            properties = default(object),
        }
    }));
}

async void OnStartHeartbeat()
{
    await Task.Delay(heartbeat_interval);
    await myWSClient.SendAsync(JsonConvert.SerializeObject(new
    {
        op = (int)Opcode.Heartbeat_C,
        d = s
    }));
}

myWSClient.Setup(new TouchSocketConfig().SetRemoteIPHost((string)result.url));
myWSClient.Connect();
Console.ReadKey();

public class RobotSetting
{
    public const string open_api = "https://sandbox.api.sgroup.qq.com";
    public const string robot_id = "3889019699";
    public const string app_id = "102106240";
    public const string token = "80ZnnMxGrIejmO5lJfW485NnklZHLT17";
    public const string secret = "tvxz147ADGJMQUYcgkpuz49EKQWciou1";
}


public enum Opcode : byte
{
    Dispatch = 0, //Receive	服务端进行消息推送
    Heartbeat_C = 1, //Send/Receive	客户端或服务端发送心跳
    Identify = 2, //Send	客户端发送鉴权
    Resume = 6, //Send	客户端恢复连接
    Reconnect = 7, //Receive	服务端通知客户端重新连接
    Invalid = 9, //Session	Receive	当 identify 或 resume 的时候，如果参数有错，服务端会返回该消息
    Hello = 10, //Receive	当客户端与网关建立 ws 连接之后，网关下发的第一条消息
    Heartbeat_S = 11, //ACK	Receive/Reply	当发送心跳成功之后，就会收到该消息
    HTTPCallbackACK = 12, //Reply	仅用于 http 回调模式的回包，代表机器人收到了平台推送的数据
}

public enum EventName
{
    GUILDS = (1 << 0),

    // - GUILD_CREATE           // 当机器人加入新guild时
    //- GUILD_UPDATE           // 当guild资料发生变更时
    //- GUILD_DELETE           // 当机器人退出guild时
    //- CHANNEL_CREATE         // 当channel被创建时
    //- CHANNEL_UPDATE         // 当channel被更新时
    //- CHANNEL_DELETE         // 当channel被删除时
    GUILD_MEMBERS = (1 << 1),

    //- GUILD_MEMBER_ADD       // 当成员加入时
    //- GUILD_MEMBER_UPDATE    // 当成员资料变更时
    //- GUILD_MEMBER_REMOVE    // 当成员被移除时
    DIRECT_MESSAGE = (1 << 12),
    //- DIRECT_MESSAGE_CREATE   // 当收到用户发给机器人的私信消息时
    //- DIRECT_MESSAGE_DELETE   // 删除（撤回）消息事件

    INTERACTION = (1 << 26),
    //- INTERACTION_CREATE     // 互动事件创建时

    MESSAGE_AUDIT = (1 << 27),
    //- MESSAGE_AUDIT_PASS     // 消息审核通过
    //- MESSAGE_AUDIT_REJECT   // 消息审核不通过

    FORUMS_EVENT = (1 << 28), // 论坛事件，仅 *私域* 机器人能够设置此 intents。
    //- FORUM_THREAD_CREATE     // 当用户创建主题时
    //- FORUM_THREAD_UPDATE     // 当用户更新主题时
    //- FORUM_THREAD_DELETE     // 当用户删除主题时
    //- FORUM_POST_CREATE       // 当用户创建帖子时
    //- FORUM_POST_DELETE       // 当用户删除帖子时
    //- FORUM_REPLY_CREATE      // 当用户回复评论时
    //- FORUM_REPLY_DELETE      // 当用户回复评论时
    //- FORUM_PUBLISH_AUDIT_RESULT      // 当用户发表审核通过时

    AUDIO_ACTION = (1 << 29),
    //- AUDIO_START             // 音频开始播放时
    //- AUDIO_FINISH            // 音频播放结束时
    //- AUDIO_ON_MIC            // 上麦时
    //- AUDIO_OFF_MIC           // 下麦时

    PUBLIC_GUILD_MESSAGES = (1 << 30) // 消息事件，此为公域的消息事件
    //- AT_MESSAGE_CREATE       // 当收到@机器人的消息时
    //- PUBLIC_MESSAGE_DELETE   // 当频道的消息被删除时
}