using System.Collections.Concurrent;

namespace ZServer;

/// <summary>
/// 引用池
/// <para>所有引用对象都应该从这里创建，以减少GC</para>
/// </summary>
public static class RefPooled
{
    class Archetype : IDisposable
    {
        private Queue<IReference> pool;

        public Type owner { get; }

        public Archetype(Type owner)
        {
            this.owner = owner;
            pool = new Queue<IReference>();
        }

        public IReference Spawner()
        {
            if (pool.Count == 0)
            {
                return (IReference)Activator.CreateInstance(owner);
            }

            return pool.Dequeue();
        }

        public void Release(IReference reference)
        {
            pool.Enqueue(reference);
            reference.Release();
            GC.SuppressFinalize(reference);
        }

        public void Dispose()
        {
            for (var i = 0; i < pool.Count; i++)
            {
                pool.Dequeue().Release();
            }

            pool = null;
        }
    }

    private static ConcurrentStack<Archetype> _archetypes = new();

    private static Archetype GetArchetype(Type type)
    {
        //todo check this type is IReference
        if (typeof(IReference).IsAssignableFrom(type) is false)
        {
            throw new NotImplementedException(nameof(IReference));
        }

        Archetype archetype = _archetypes.FirstOrDefault(x => x.owner == type);
        if (archetype == null)
        {
            archetype = new Archetype(type);
            _archetypes.Push(archetype);
        }

        return archetype;
    }

    /// <summary>
    /// 产生一个引用对象
    /// </summary>
    /// <typeparam name="T">引用对象类型</typeparam>
    /// <returns></returns>
    public static T Spawner<T>() where T : class, IReference, new()
    {
        return (T)Spawner(typeof(T));
    }

    /// <summary>
    /// 产生一个引用对象
    /// </summary>
    /// <param name="type">引用对象类型</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static IReference Spawner(Type type)
    {
        //todo check this type is IReference
        if (typeof(IReference).IsAssignableFrom(type) is false)
        {
            throw new NotImplementedException(nameof(IReference));
        }

        return GetArchetype(type).Spawner();
    }

    /// <summary>
    /// 回收引用对象
    /// </summary>
    /// <param name="reference"></param>
    public static void Release(IReference reference)
    {
        GetArchetype(reference.GetType()).Release(reference);
    }
}