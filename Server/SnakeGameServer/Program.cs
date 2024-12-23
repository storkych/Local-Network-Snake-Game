using System;

namespace SnakeGameServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new GameServer(12345); // Укажите порт
            server.Start();

            Console.WriteLine("Нажмите любую клавишу, чтобы остановить сервер...");
            Console.ReadKey();

            server.Stop(); // Остановка сервера
        }
    }
}
