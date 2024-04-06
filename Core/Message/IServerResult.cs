namespace ZServer;

public interface IServerResult : IMessaged
{
    Status status { get; }
    string message { get; }


    class DefaultServerResult : IServerResult
    {
        public long id { get; set; }
        public Status status { get; set; }
        public string message { get; set; }


        public void Release()
        {
            status = Status.Unknown;
            message = null;
        }
    }

    public static IServerResult Create(Status status, string message)
    {
        var result = RefPooled.Spawner<DefaultServerResult>();
        result.status = status;
        result.message = message;
        return result;
    }
}

public interface IServerResult<T> : IServerResult
{
    T data { get; }

    class DefaultServerResult<T> : IServerResult<T>
    {
        public long id { get; set; }
        public Status status { get; set; }
        public string message { get; set; }
        public T data { get; set; }

        public void Release()
        {
            status = Status.Unknown;
            message = null;
            data = default;
        }

        public static IServerResult<T> Create(Status status, string message, T data)
        {
            var result = RefPooled.Spawner<DefaultServerResult<T>>();
            result.status = status;
            result.message = message;
            result.data = data;
            return result;
        }
    }
}