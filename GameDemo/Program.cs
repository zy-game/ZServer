// See https://aka.ms/new-console-template for more information

using GameDemo;
using ZGame;
using ZGame.Game.LockStep;
using ZGame.Networking;
using ZGame.Room;

AppCore.name = "Room";
AppCore.version = "1.0.0";
AppCore.fixedDeltaTime = 100;
AppCore.Subscribe<Client, MSGPacket>((int)MSG_LockStep.CS_PLAYER_JOIN, OnHandleRoomMsg);
AppCore.Subscribe<Client, MSGPacket>((int)MSG_LockStep.CS_PLAYER_LEAVE, OnHandleRoomMsg);
AppCore.Subscribe<Client, MSGPacket>((int)MSG_LockStep.CS_PLAYER_READY, OnHandleRoomMsg);
AppCore.Subscribe<Client, MSGPacket>((int)MSG_LockStep.CS_PLAYER_INPUT, OnHandleRoomMsg);
AppCore.Subscribe<Client, MSGPacket>((int)MSG_LockStep.CS_LOADCOMPLETE, OnHandleRoomMsg);
AppCore.Startup(8090);

static void OnHandleRoomMsg(Client client, MSGPacket packet)
{
    if (client is null || packet is null)
    {
        AppCore.Log("client or packet is null");
        return;
    }

    if (packet.EnsureMessageStatusCode() is false)
    {
        AppCore.Log($"{client.cid} {packet.opcode} {packet.status} {packet.message}");
        return;
    }

    switch (packet.opcode)
    {
        case (int)MSG_LockStep.CS_PLAYER_JOIN:
            using (var join = packet.Decode<MSG_UserJoin>())
            {
                RoomBase room = AppCore.GetRoom<SHDLDRoom>(join.uid);
                if (room is null)
                {
                    room = AppCore.GetFreeRoom<SHDLDRoom>() ?? AppCore.CreateRoom<SHDLDRoom>();
                }

                client.uid = join.uid;
                client.JoinRoom(room);
            }

            break;
        case (int)MSG_LockStep.CS_PLAYER_LEAVE:
            using (var leave = packet.Decode<MSG_UserLeave>())
            {
                RoomBase room = AppCore.GetRoom<SHDLDRoom>(leave.uid);
                if (room is null)
                {
                    client.Send(MSGPacket.Serialize((int)MSG_LockStep.SC_PLAYER_LEVAE, null, 404, "房间不存在"));
                    return;
                }

                client.LeaveRoom();
            }

            break;
        case (int)MSG_LockStep.CS_PLAYER_READY:
            using (var ready = packet.Decode<MSG_UserReady>())
            {
                RoomBase room = AppCore.GetRoom<SHDLDRoom>(ready.uid);
                if (room is null)
                {
                    client.Send(MSGPacket.Serialize((int)MSG_LockStep.SC_PLAYER_READY, null, 404, "房间不存在"));
                    return;
                }

                if (room.state is not RoomState.None)
                {
                    client.Send(MSGPacket.Serialize((int)MSG_LockStep.SC_PLAYER_READY, null, 201, "该房间已经开始游戏了"));
                    return;
                }

                room.UserReady(client.uid);
            }

            break;
        case (int)MSG_LockStep.CS_PLAYER_INPUT:
            using (var input = packet.Decode<MSG_UserInput>())
            {
                SHDLDRoom room = AppCore.GetRoom<SHDLDRoom>(input.command.uid);
                if (room is null)
                {
                    return;
                }

                if (room.state is not RoomState.Running)
                {
                    AppCore.Log("当前房间状态不是运行状态");
                    return;
                }

                room.DoUserInputCommand(client.uid, input.command);
            }

            break;
        case (int)MSG_LockStep.CS_LOADCOMPLETE:

            using (var load = packet.Decode<MSG_LoadComplete>())
            {
                SHDLDRoom room = AppCore.GetRoom<SHDLDRoom>(load.uid);
                if (room is null)
                {
                    return;
                }

                if (room.state is not RoomState.Prepare)
                {
                    AppCore.Log("当前房间状态不是准备状态");
                    return;
                }

                room.UserLoadComplete(client.uid);
            }

            break;
    }
}