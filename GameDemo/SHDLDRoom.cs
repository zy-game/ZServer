using FixMath.NET;
using Newtonsoft.Json;
using ZGame;
using ZGame.Game.LockStep;
using ZGame.Networking;
using ZGame.Room;
using Random = ZGame.Random;

namespace GameDemo;

class SHDLDRoom : RoomBase
{
    private long _frameId;
    private Random _random;
    private FrameData _frameData;


    protected override void DoAwake()
    {
        SetUserLimit(2);
        _random = new Random(Crc32.GetCRC32Str(Guid.NewGuid().ToString()));
        _frameData = RefPooled.Alloc<FrameData>();
    }

    protected override void DoFixedUpdate()
    {
        if (state is not RoomState.Running)
        {
            return;
        }

        if (clients.Length == 0 || state == RoomState.End)
        {
            return;
        }

        using (MSG_Frame msg = RefPooled.Alloc<MSG_Frame>())
        {
            msg.rid = rid;
            msg.frame = _frameData.Clone();
            msg.frame.SetFrameId(_frameId);
            AppCore.Log($"{_frameId}帧 :{JsonConvert.SerializeObject(msg)}");
            Broadcast(MSGPacket.Serialize((int)MSG_LockStep.SC_FRAME, msg));
        }

        _frameId++;
        _frameData.Reset();
    }

    public override void Release()
    {
        _frameId = 0;
        RefPooled.Free(_frameData);
        _frameData = null;
    }

    public void DoUserInputCommand(uint uid, Command command)
    {
        _frameData.Set(uid, command);
    }

    protected override void DoUserJoin(Client client)
    {
        using (MSG_RoomInfo roomInfo = RefPooled.Alloc<MSG_RoomInfo>())
        {
            roomInfo.rid = rid;
            roomInfo.rName = name;
            roomInfo.seed = _random.randSeed;
            client.Send(MSGPacket.Serialize((int)MSG_LockStep.SC_ROOM_INFO, roomInfo));
        }

        foreach (var VARIABLE in clients)
        {
            if (VARIABLE.uid == client.uid)
            {
                continue;
            }

            using (MSG_UserInfo userData = RefPooled.Alloc<MSG_UserInfo>())
            {
                userData.uid = VARIABLE.uid;
                userData.name = "Test_" + VARIABLE.uid;
                userData.avatar = String.Empty;
                AppCore.Log(JsonConvert.SerializeObject(userData));
                client.Send(MSGPacket.Serialize((int)MSG_LockStep.SC_PLAYER_JOIN, userData));
            }
        }

        using (MSG_UserInfo userData = RefPooled.Alloc<MSG_UserInfo>())
        {
            userData.uid = client.uid;
            userData.name = "Test_" + client.uid;
            userData.avatar = String.Empty;
            Broadcast(MSGPacket.Serialize((int)MSG_LockStep.SC_PLAYER_JOIN, userData));
        }
    }

    protected override void DoUserLeave(uint uid)
    {
        if (clients.Length == 0)
        {
            return;
        }

        using (MSG_UserLeave msg = RefPooled.Alloc<MSG_UserLeave>())
        {
            msg.rid = rid;
            msg.uid = uid;
            Broadcast(MSGPacket.Serialize((int)MSG_LockStep.SC_PLAYER_LEVAE, msg));
        }
    }

    protected override void DoUserReady(uint uid)
    {
        using (var msg = RefPooled.Alloc<MSG_UserReady>())
        {
            msg.uid = uid;
            Broadcast(MSGPacket.Serialize((int)MSG_LockStep.SC_PLAYER_READY, msg));
        }
    }

    protected override void DoLoadGame()
    {
        //TODO 通知所有玩家准备游戏开始
        Broadcast(MSGPacket.Serialize((int)MSG_LockStep.SC_LOADGAME, new MSG_LoadGame()));
    }

    protected override void DoGameStart()
    {
        _frameData = FrameData.Create(0, clients.Select(x => Command.Create(x.uid, new())).ToList());
        Broadcast(MSGPacket.Serialize((int)MSG_LockStep.SC_GAME_START, new MSG_GameStart()));
    }

    protected override void DoGameOver()
    {
        using (var msg = RefPooled.Alloc<MSG_GameOver>())
        {
            Broadcast(MSGPacket.Serialize((int)MSG_LockStep.SC_GAME_OVER, msg));
        }
    }

    protected override void DoBalance()
    {
        using (var msg = RefPooled.Alloc<MSG_Balance>())
        {
            Broadcast(MSGPacket.Serialize((int)MSG_LockStep.SC_BALANCE, msg));
        }
    }
}