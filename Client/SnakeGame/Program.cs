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

            // Пример отправки команды JOIN
            SendMessage(client, serverEndPoint, "JOIN|Player1");

            // Пример получения ответа от сервера
            var serverResponse = ReceiveMessage(client);
            Console.WriteLine($"Ответ от сервера: {serverResponse}");
        }

        static void SendMessage(UdpClient client, IPEndPoint serverEndPoint, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            client.Send(data, data.Length, serverEndPoint);
            Console.WriteLine($"Отправлено сообщение: {message}");
        }

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
