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
        private const int port = 12345;

        GameEngine gameEngine;

        public GameClient()
        {
            udpClient = new UdpClient();
            serverEndPoint = new IPEndPoint(IPAddress.Loopback, port);
        }

        public void Start()
        {
            SendMessage("LOL");
            SendMessage("JOIN|Player1");
            var receiveThread = new Thread(ReceiveData);
            receiveThread.Start();

            gameEngine = new GameEngine(this);
            gameEngine.Run();
        }

        // Отправка сообщения на сервер
        private void SendMessage(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, serverEndPoint);
            Console.WriteLine($"Отправлено сообщение: {message}");
        }

        public void SendDirection(string direction)
        {
            string message = $"MOVE|{direction}";
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
                    HandleMessage(message, serverEndPoint);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error receiving data: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Обрабатывает входящее сообщение в зависимости от команды.
        /// </summary>
        /// <param name="message">Полученное сообщение.</param>
        /// <param name="senderEndPoint">Точка отправителя сообщения.</param>
        private void HandleMessage(string message, IPEndPoint senderEndPoint)
        {
            Console.WriteLine($"HandleMessage");
            var parts = message.Split('|');
            if (parts.Length < 2) return;

            var command = parts[0];
            var payload = parts[1];

            switch (command)
            {
                case "START":
                    HandleStart(payload, senderEndPoint);
                    break;
                case "MOVE":
                    HandleMove(payload, senderEndPoint);
                    break;
                case "LEAVE":
                    //HandleLeave(payload, senderEndPoint);
                    break;
                default:
                    Console.WriteLine($"Неизвестная команда: {command}");
                    break;
            }
        }

        /// <summary>
        /// Обработка команды движения игрока.
        /// </summary>
        /// <param name="payload">Данные о действии игрока.</param>
        /// <param name="senderEndPoint">Точка отправителя.</param>
        private void HandleMove(string payload, IPEndPoint senderEndPoint)
        {
            Console.WriteLine($"ПРИШЁЛ ХОД: {payload}");
        }
        private void HandleStart(string payload, IPEndPoint senderEndPoint)
        {
            if (payload == "2")
            {
                gameEngine.isClientHost = false;
            }
        }
    }
}
