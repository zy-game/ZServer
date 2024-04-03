// See https://aka.ms/new-console-template for more information

using GameDemo;
using ZServer;

Console.WriteLine("Hello, World!");
App.Startup<Demo>(new AppConfig()
{
    name = "GameDemo",
    version = "1.0.0",
    hosting = "127.0.0.1",
    port = 8080,
    gate = "/api"
});