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
    public int id { get; set; } = Guid.NewGuid().GetHashCode();
    private int tick = 0;
    private List<UserData> clients = new();
    private Queue<FrameData> syncQueue = new();
    private int count = 1;
    private RoomState state = RoomState.Ready;

    class UserData
    {
        public uint uid;
        public int cid;
        public string path;
        public TSVector pos;
        public TSQuaternion rot;
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

    public Task OnMessage(Client client, int opcode, byte[] messaged)
    {
        switch ((SyncCode)opcode)
        {
            case SyncCode.JOIN:
                OnUserJoin(Join.Decode(messaged), client);
                break;
            case SyncCode.LEAVE:
                OnUserLeave(Leave.Decode(messaged));
                break;
            case SyncCode.SYNC:
                OnSyncData(FrameData.Decode(messaged), client);
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
        App.Broadcast((int)SyncCode.READY, Ready.Encode(ready));
        if (state == RoomState.Ready && clients.All(x => x.isReady) && clients.Count == count)
        {
            state = RoomState.Running;
            App.Broadcast((int)SyncCode.START, StartGame.Encode(null));
            App.Log("游戏开始");
        }
    }

    private void OnUserJoin(Join join, Client client)
    {
        UserData user = clients.Find(x => x.uid == join.uid);
        if (user is not null)
        {
            clients.Remove(user);
        }

        user = new UserData();
        user.cid = client.cid;
        user.isReady = false;
        user.path = join.path;
        user.pos = new TSVector(clients.Count * 5, 2, 0);
        user.rot = join.rotation;
        clients.Add(user);
        App.Broadcast((int)SyncCode.JOIN, Join.Create(user.uid, user.path, user.pos, user.rot));
        for (int i = 0; i < clients.Count; i++)
        {
            if (clients[i].uid == join.uid)
            {
                continue;
            }

            client.Write((int)SyncCode.JOIN, Join.Create(clients[i].uid, clients[i].path, clients[i].pos, clients[i].rot));
        }

        App.Log("玩家进入：" + join.uid);
    }

    private void OnUserLeave(Leave leave)
    {
        clients.Remove(clients.Find(x => x.uid == leave.uid));
        if (clients.Count == 0)
        {
            Release();
        }

        App.Log("玩家离开：" + leave.uid);
        App.Broadcast((int)SyncCode.LEAVE, Leave.Encode(leave));
    }

    private void OnSyncData(FrameData frame, Client client)
    {
        UserData user = clients.Find(x => x.cid == client.cid);
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

        AddSync(frame);
        App.Broadcast((int)SyncCode.SYNC, FrameData.Encode(frame));
    }


    public void Release()
    {
        state = RoomState.Ready;
        tick = 0;
        syncQueue.Clear();
        App.Log("重置房间");
    }
}