using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using TrueSync;
using ZServer;

namespace GameDemo;

public class Demo : IServer
{
    public uint id { get; set; } = ID.Generic();

    public ServerState state => isReady() ? ServerState.Running : ServerState.Free;

    private long tick = 0;
    private List<UserData> clients = new();
    private Queue<SyncData> syncQueue = new();
    private int count = 1;

    class UserData
    {
        public uint uid;
        public IChannelId cid;
        public SyncData sync;
        public bool isReady;
    }

    private void AddSync(SyncData sync)
    {
        syncQueue.Enqueue(sync);
        if (syncQueue.Count > 20)
        {
            syncQueue.Dequeue();
        }
    }

    public Task<Status> Start()
    {
        App.Log("serid id:" + id + " startup");
        return Task.FromResult(Status.Success);
    }

    public Task<Status> Shutdown()
    {
        App.Log("serid id:" + id + " shutdown");
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
                OnUserLevae(Leave.Decode(messaged));
                break;
            case SyncCode.SYNC:
                OnSyncData(SyncData.Decode(messaged), cid);
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
    }

    private void OnUserJoin(Join join, IChannelId cid)
    {
        UserData user = clients.Find(x => x.uid == join.uid);
        if (user is not null)
        {
            App.Log("玩家重复进入：" + join.uid);
            return;
        }

        App.Log("玩家进入：" + join.uid);
        clients.Add(user = new UserData() { cid = cid, uid = join.uid });
        KCPServer.Broadcast((int)SyncCode.JOIN, Join.Encode(join), clients.Select(x => x.cid).ToArray());
    }

    private void OnUserLevae(Leave leave)
    {
        if (clients.Remove(clients.Find(x => x.uid == leave.uid)))
        {
            App.Log("玩家离开：" + leave.uid);
            KCPServer.Broadcast((int)SyncCode.LEAVE, Leave.Encode(leave), clients.Select(x => x.cid).ToArray());
        }
    }

    private void OnSyncData(SyncData sync, IChannelId cid)
    {
        UserData user = clients.Find(x => x.cid == cid);
        if (user is null)
        {
            return;
        }

        App.Log("玩家同步数据：" + user.uid);
        user.sync = sync;
    }

    private bool isReady()
    {
        return clients.All(x => x.isReady) && clients.Count == count;
    }

    public void FixedUpdate()
    {
        if (isReady() is false)
        {
            return;
        }

        tick++;
        foreach (UserData user in clients)
        {
            if (user.sync is null)
            {
                user.sync = new SyncData();
                user.sync.AddFrameData(new InputData() { owner = user.uid });
            }
        }

        SyncData sync = SyncData.Merge(clients.Select(x => x.sync).ToArray());
        AddSync(sync);
        KCPServer.Broadcast((int)SyncCode.SYNC, SyncData.Encode(sync), clients.Select(x => x.cid).ToArray());
    }

    public void Release()
    {
    }
}