using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SnakeGame
{
    public class GameClient
    {
        private UdpClient _client;
        private readonly IPEndPoint _serverEndPoint;
        private bool _isRunning;

        public GameClient(string serverIp, int serverPort)
        {
            _client = new UdpClient();
            _serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
        }

        public void Start()
        {
            _isRunning = true;
            Console.WriteLine("Клиент запущен.");

            var receiveThread = new Thread(ReceiveData);
            receiveThread.Start();

            SendCommand("JOIN|Player1");
        }

        public void Stop()
        {
            _isRunning = false;
            SendCommand("LEAVE|Player1");
            _client.Close();
        }

        private void ReceiveData()
        {
            while (_isRunning)
            {
                try
                {
                    var serverEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    var data = _client.Receive(ref serverEndPoint);
                    var message = Encoding.UTF8.GetString(data);

                    Console.WriteLine($"Получено сообщение от сервера: {message}");
                    HandleServerMessage(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }
            }
        }

        private void SendCommand(string command)
        {
            var data = Encoding.UTF8.GetBytes(command);
            _client.Send(data, data.Length, _serverEndPoint);
            Console.WriteLine($"Отправлена команда: {command}");
        }

        private void HandleServerMessage(string message)
        {
            var parts = message.Split('|');
            var command = parts[0];
            var payload = parts.Length > 1 ? parts[1] : string.Empty;

            switch (command)
            {
                case "START":
                    Console.WriteLine($"Игра началась. Игроков: {payload}");
                    // Можно вызвать метод запуска игры
                    break;
                case "MOVE":
                    Console.WriteLine($"Ход оппонента: {payload}");
                    // Обновить состояние игры на основе данных оппонента
                    break;
                case "END":
                    Console.WriteLine("Игра завершена: " + payload);
                    _isRunning = false; // Останавливаем клиент
                    break;
            }
        }

        public void SendMove(string moveData)
        {
            SendCommand($"MOVE|{moveData}");
        }

    }
}
