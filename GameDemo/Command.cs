using System.Text;
using TrueSync;
using ZServer;

namespace GameDemo;

public enum SyncCode : byte
{
    JOIN = 100,
    LEAVE = 101,
    READY = 102,
    START = 103,
    SYNC = 104,
    END = 105
}

public class Ready : IMessaged
{
    public uint uid;

    public static byte[] Encode(Ready data)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(data.uid);
                return ms.ToArray();
            }
        }
    }

    public static Ready Decode(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream(data))
        {
            using (BinaryReader br = new BinaryReader(ms))
            {
                Ready ready = RefPooled.Spawner<Ready>();
                ready.uid = br.ReadUInt32();
                return ready;
            }
        }
    }

    public void Release()
    {
    }
}

public class Leave : IMessaged
{
    public uint uid;

    public static byte[] Encode(Leave data)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(data.uid);
                return ms.ToArray();
            }
        }
    }

    public static Leave Decode(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream(data))
        {
            using (BinaryReader br = new BinaryReader(ms))
            {
                Leave leave = RefPooled.Spawner<Leave>();
                leave.uid = br.ReadUInt32();
                return leave;
            }
        }
    }

    public void Release()
    {
    }
}

public class Join : IMessaged
{
    public uint uid;
    public string path;
    public TSVector position;
    public TSQuaternion rotation;

    public void Release()
    {
    }

    public static byte[] Create(uint uid, string path, TSVector position, TSQuaternion rotation)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(uid);
                bw.Write(path);
                bw.Write(position.x.RawValue);
                bw.Write(position.y.RawValue);
                bw.Write(position.z.RawValue);
                bw.Write(rotation.x.RawValue);
                bw.Write(rotation.y.RawValue);
                bw.Write(rotation.z.RawValue);
                bw.Write(rotation.w.RawValue);
                return ms.ToArray();
            }
        }
    }

    public static byte[] Encode(Join data)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(data.uid);
                bw.Write(data.path);
                bw.Write(data.position.x.RawValue);
                bw.Write(data.position.y.RawValue);
                bw.Write(data.position.z.RawValue);
                bw.Write(data.rotation.x.RawValue);
                bw.Write(data.rotation.y.RawValue);
                bw.Write(data.rotation.z.RawValue);
                bw.Write(data.rotation.w.RawValue);
                return ms.ToArray();
            }
        }
    }

    public static Join Decode(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream(data))
        {
            using (BinaryReader br = new BinaryReader(ms))
            {
                Join join = RefPooled.Spawner<Join>();
                join.uid = br.ReadUInt32();
                join.path = br.ReadString();
                join.position = new TSVector(br.ReadInt64(), br.ReadInt64(), br.ReadInt64());
                join.rotation = new TSQuaternion(br.ReadInt64(), br.ReadInt64(), br.ReadInt64(), br.ReadInt64());
                return join;
            }
        }
    }
}

public class StartGame : IMessaged
{
    public void Release()
    {
    }

    public static byte[] Encode(FrameData data)
    {
        return Array.Empty<byte>();
    }

    public static StartGame Decode(byte[] data)
    {
        return RefPooled.Spawner<StartGame>();
    }
}

public class GameOver : IMessaged
{
    public void Release()
    {
    }

    public static byte[] Encode(FrameData data)
    {
        return Array.Empty<byte>();
    }

    public static GameOver Decode(byte[] data)
    {
        return RefPooled.Spawner<GameOver>();
    }
}

/// <summary>
/// 帧同步信息
/// </summary>
public class FrameData : IMessaged
{
    /// <summary>
    /// 帧编号
    /// </summary>
    public int frame;

    /// <summary>
    /// 当前帧所有玩家的输入
    /// </summary>
    public List<InputData> frameData = new();

    public static byte[] Encode(FrameData data)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(data.frame);
                bw.Write(data.frameData.Count);
                foreach (var item in data.frameData)
                {
                    byte[] bytes = InputData.Encode(item);
                    bw.Write(bytes.Length);
                    bw.Write(bytes);
                }

                return ms.ToArray();
            }
        }
    }

    public static FrameData Decode(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream(data))
        {
            using (BinaryReader br = new BinaryReader(ms))
            {
                FrameData frameData = RefPooled.Spawner<FrameData>();
                frameData.frame = br.ReadInt32();
                int count = br.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    byte[] bytes = new byte[br.ReadInt32()];
                    br.Read(bytes, 0, bytes.Length);
                    InputData inputData = InputData.Decode(bytes);
                    frameData.SetInputData(inputData);
                }

                return frameData;
            }
        }
    }


    /// <summary>
    /// 添加玩家输入数据
    /// </summary>
    /// <param name="data"></param>
    public void SetInputData(InputData data)
    {
        frameData.Add(data);
    }

    /// <summary>
    /// 获取当前玩家的输入数据
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public InputData GetFrameData(uint id)
    {
        InputData inputData = frameData.Find(x => x.owner == id);
        if (inputData is null)
        {
            inputData = RefPooled.Spawner<InputData>();
        }

        return inputData;
    }

    public void RemoveFrameData(uint id)
    {
        frameData.RemoveAll(x => x.owner == id);
    }

    public bool Contains(uint uid)
    {
        return frameData.Exists(x => x.owner == uid);
    }

    public static FrameData Merge(params FrameData[] data)
    {
        FrameData frameData = RefPooled.Spawner<FrameData>();
        foreach (FrameData sync in data)
        {
            foreach (InputData inputData in sync.frameData)
            {
                if (frameData.Contains(inputData.owner))
                {
                    frameData.RemoveFrameData(inputData.owner);
                }

                frameData.SetInputData(inputData);
            }
        }

        return frameData;
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("frame:" + frame);
        foreach (InputData inputData in frameData)
        {
            sb.Append("{" + inputData.ToString() + "}");
        }

        return sb.ToString();
    }

    public void Release()
    {
        frameData.Clear();
    }
}

/// <summary>
/// 玩家输入数据
/// </summary>
public class InputData : IReference
{
    /// <summary>
    /// 玩家id
    /// </summary>
    public uint owner;

    public Dictionary<byte, FP> fpList = new();

    /// <summary>
    /// 序列化输入数据
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static byte[] Encode(InputData data)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(data.owner);
                bw.Write(data.fpList.Count);
                foreach (var item in data.fpList)
                {
                    bw.Write(item.Key);
                    bw.Write((float)item.Value);
                }

                return ms.ToArray();
            }
        }
    }

    /// <summary>
    /// 反序列化输入数据
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static InputData Decode(byte[] data)
    {
        using (MemoryStream ms = new MemoryStream(data))
        {
            using (BinaryReader br = new BinaryReader(ms))
            {
                InputData inputData = RefPooled.Spawner<InputData>();
                inputData.owner = br.ReadUInt32();
                int count = br.ReadInt32();
                inputData.fpList = new Dictionary<byte, FP>();
                for (int i = 0; i < count; i++)
                {
                    inputData.fpList.Add(br.ReadByte(), br.ReadSingle());
                }

                return inputData;
            }
        }
    }

    /// <summary>
    /// 获取输入
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public FP Get(byte id)
    {
        if (fpList.TryGetValue(id, out var value))
        {
            return value;
        }

        return default(FP);
    }

    /// <summary>
    /// 设置输入数据
    /// </summary>
    /// <param name="id"></param>
    /// <param name="value"></param>
    public void Set(byte id, FP value)
    {
        if (fpList.ContainsKey(id))
        {
            fpList[id] = value;
        }
        else
        {
            fpList.Add(id, value);
        }
    }


    public void Clear()
    {
        fpList.Clear();
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("uid:" + owner + " ");
        foreach (var item in fpList)
        {
            sb.Append($"{item.Key}:{item.Value} ");
        }

        return sb.ToString();
    }

    public void Release()
    {
        Clear();
    }
}