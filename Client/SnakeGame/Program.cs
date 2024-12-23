using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SnakeGame
{
    class Program
    {

        static void Main(string[] args)
        {
            var client = new UdpClient();
            var serverEndPoint = new IPEndPoint(IPAddress.Loopback, 12345); // Адрес сервера и порт

            // Пример отправки команды JOIN для каждого игрока
            SendMessage(client, serverEndPoint, "JOIN|Player1");

            // Получаем ответ от сервера (например, подтверждение подключения)
            var serverResponse = ReceiveMessage(client);
            Console.WriteLine($"Ответ от сервера: {serverResponse}");

            // Запускаем игровую логику (например, движение змейки)
            GameEngine gameEngine = new GameEngine(client, serverEndPoint);
            gameEngine.Run();
        }

        // Отправка сообщения на сервер
        static void SendMessage(UdpClient client, IPEndPoint serverEndPoint, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            client.Send(data, data.Length, serverEndPoint);
            Console.WriteLine($"Отправлено сообщение: {message}");
        }

        // Получение сообщения от сервера
        static string ReceiveMessage(UdpClient client)
        {
            // Создаем изменяемую переменную для EndPoint
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            // Получаем данные от сервера
            byte[] receivedData = client.Receive(ref remoteEndPoint);

            // Преобразуем данные в строку
            return Encoding.UTF8.GetString(receivedData);
        }



        /*
        static void Main()
        {
            GameEngine gameEngine = new GameEngine();
            gameEngine.Run();
        }
        */
    }


}
