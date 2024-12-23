using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SnakeGame
{
    public class GameClient
    {
        private UdpClient udpClient;
        private IPEndPoint serverEndPoint;
        private string direction = "RIGHT"; // Направление по умолчанию
        private const int port = 8888;

        public GameClient(string serverIp)
        {
            udpClient = new UdpClient();
            serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), port);
        }

        public void Start()
        {
            Thread receiveThread = new Thread(new ThreadStart(ReceiveData));
            receiveThread.Start();

            while (true)
            {
                // Получаем ввод пользователя для управления змейкой
                string input = Console.ReadKey(true).Key.ToString();
                if (input == "W") direction = "UP";
                if (input == "S") direction = "DOWN";
                if (input == "A") direction = "LEFT";
                if (input == "D") direction = "RIGHT";

                // Отправляем команду на сервер
                SendDirection();
            }
        }

        private void SendDirection()
        {
            string message = $"MOVE,{direction}";
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, serverEndPoint);
        }

        private void ReceiveData()
        {
            while (true)
            {
                try
                {
                    byte[] data = udpClient.Receive(ref serverEndPoint);
                    string message = Encoding.UTF8.GetString(data);
                    Console.Clear();
                    Console.WriteLine("Game state:");
                    Console.WriteLine(message); // Выводим состояние игры, полученное от сервера
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error receiving data: " + ex.Message);
                }
            }
        }
    }
}
