using MemoryPack;

namespace ZServer;

public interface IMessaged : IReference
{
    public long id { get; set; }


    private static long __id = 0;

    public static long ID()
    {
        return ++__id;
    }

    public static T Deserialized<T>(byte[] bytes) where T : IMessaged, new()
    {
        return MemoryPackSerializer.Deserialize<T>(bytes);
    }

    public static byte[] Serialized(IMessaged messaged)
    {
        return MemoryPackSerializer.Serialize(messaged);
    }
}