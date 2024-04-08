using System.Text;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using TrueSync;
using ZServer;

namespace GameDemo;

public enum RoomState
{
    Ready,
    Running,
    End
}

public class Demo : IServer
{
    public uint id { get; set; } = ID.Generic();
    private int tick = 0;
    private List<UserData> clients = new();
    private Queue<FrameData> syncQueue = new();
    private int count = 1;
    private RoomState state = RoomState.Ready;

    class UserData
    {
        public uint uid;
        public IChannelId cid;
        public FrameData Frame;
        public bool isReady;
    }

    private void AddSync(FrameData frame)
    {
        syncQueue.Enqueue(frame);
        if (syncQueue.Count > 20)
        {
            syncQueue.Dequeue();
        }
    }

    public Task<Status> Start()
    {
        App.Log("server id:" + id + " startup");
        return Task.FromResult(Status.Success);
    }

    public Task<Status> Shutdown()
    {
        App.Log("server id:" + id + " shutdown");
        return Task.FromResult(Status.Success);
    }

    public Task OnMessage(IChannelId cid, int opcode, byte[] messaged)
    {
        switch ((SyncCode)opcode)
        {
            case SyncCode.JOIN:
                OnUserJoin(Join.Decode(messaged), cid);
                break;
            case SyncCode.LEAVE:
                OnUserLeave(Leave.Decode(messaged));
                break;
            case SyncCode.SYNC:
                OnSyncData(FrameData.Decode(messaged), cid);
                break;
            case SyncCode.READY:
                OnUserReady(Ready.Decode(messaged));
                break;
        }

        return Task.CompletedTask;
    }

    private void OnUserReady(Ready ready)
    {
        UserData user = clients.Find(x => x.uid == ready.uid);
        if (user is null)
        {
            return;
        }

        App.Log("玩家准备：" + ready.uid);
        user.isReady = !user.isReady;
        KCPServer.Broadcast((int)SyncCode.READY, Ready.Encode(ready), clients.Select(x => x.cid).ToArray());
        if (state == RoomState.Ready && clients.All(x => x.isReady))
        {
            state = RoomState.Running;
            KCPServer.Broadcast((int)SyncCode.START, StartGame.Encode(null), clients.Select(x => x.cid).ToArray());
        }
    }

    private void OnUserJoin(Join join, IChannelId cid)
    {
        UserData user = clients.Find(x => x.uid == join.uid);
        App.Log("玩家进入：" + join.uid);
        clients.Add(user = new UserData() { cid = cid, uid = join.uid });
        KCPServer.Broadcast((int)SyncCode.JOIN, Join.Encode(join), clients.Select(x => x.cid).ToArray());
    }

    private void OnUserLeave(Leave leave)
    {
        if (state is RoomState.Ready)
        {
            clients.Remove(clients.Find(x => x.uid == leave.uid));
        }

        App.Log("玩家离开：" + leave.uid);
        KCPServer.Broadcast((int)SyncCode.LEAVE, Leave.Encode(leave), clients.Select(x => x.cid).ToArray());
    }

    private void OnSyncData(FrameData frame, IChannelId cid)
    {
        UserData user = clients.Find(x => x.cid == cid);
        if (user is null)
        {
            return;
        }

        user.Frame = frame;
    }

    public void FixedUpdate()
    {
        if (state is not RoomState.Running)
        {
            return;
        }

        tick++;
        state = tick == 10000 ? RoomState.End : RoomState.Running;
        foreach (UserData user in clients)
        {
            if (user.Frame is null)
            {
                user.Frame = new FrameData();
                user.Frame.SetInputData(new InputData() { owner = user.uid });
            }
        }

        FrameData frame = RefPooled.Spawner<FrameData>();
        frame.frame = tick;
        foreach (var VARIABLE in clients)
        {
            InputData input = VARIABLE.Frame.GetFrameData(VARIABLE.uid);
            frame.SetInputData(input);
        }

        App.Log(frame.ToString());
        AddSync(frame);
        KCPServer.Broadcast((int)SyncCode.SYNC, FrameData.Encode(frame), clients.Select(x => x.cid).ToArray());
    }


    public void Release()
    {
    }
}