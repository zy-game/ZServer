namespace ZServer
{
    public class AppConfig
    {
        public string name;
        public string version;
        public string hosting;
        public ushort port;
        public string gate;
    }

    public static class App
    {
        public static void Startup<T>(AppConfig cfg) where T : IServer, new()
        {
        }

        public static void Shutdown<T>() where T : IServer, new()
        {
        }
    }
}