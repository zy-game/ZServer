namespace ZServer;

public class Packet : IMessaged
{
    public int opcode { get; set; }
    public byte[] Data { get; set; }

    public void Release()
    {
        opcode = 0;
        Data = null;
    }

    public static byte[] Create(int opcode, byte[] bytes)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write(opcode);
                bw.Write(bytes.Length);
                bw.Write(bytes);
                return ms.ToArray();
            }
        }
    }


    public static Packet Deserialized(byte[] bytes)
    {
        using (MemoryStream ms = new MemoryStream(bytes))
        {
            using (BinaryReader br = new BinaryReader(ms))
            {
                Packet msg = RefPooled.Spawner<Packet>();
                msg.opcode = br.ReadInt32();
                msg.Data = br.ReadBytes(br.ReadInt32());
                return msg;
            }
        }
    }

    public static byte[] Serialize(Packet msg)
    {
        return Create(msg.opcode, msg.Data);
    }
}