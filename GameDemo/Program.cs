// See https://aka.ms/new-console-template for more information

using GameDemo;
using ZServer;

Console.WriteLine("Hello, World!");
App.name = "Room";
App.version = "1.0.0";
App.fixedUpdateRate = 50;
App.Startup<Demo>(8099);
Console.ReadKey();