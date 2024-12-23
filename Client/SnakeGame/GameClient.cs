using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SnakeGame
{
    public class GameClient
    {
        private UdpClient _client; // UDP-клиент для взаимодействия с сервером
        private readonly IPEndPoint _serverEndPoint; // Точка для подключения к серверу
        private bool _isRunning; // Флаг, указывающий на состояние клиента

        /// <summary>
        /// Конструктор GameClient, инициирующий клиента с IP-адресом сервера и портом.
        /// </summary>
        /// <param name="serverIp">IP-адрес сервера.</param>
        /// <param name="serverPort">Порт сервера.</param>
        public GameClient(string serverIp, int serverPort)
        {
            _client = new UdpClient(); // Инициализация UDP-клиента
            _serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort); // Установка конечной точки сервера
        }

        /// <summary>
        /// Запускает клиента и начинает прослушивание сообщений от сервера.
        /// </summary>
        public void Start()
        {
            _isRunning = true;
            Console.WriteLine("Клиент запущен.");

            var receiveThread = new Thread(ReceiveData);
            receiveThread.Start(); // Запуск потока для получения данных от сервера

            SendCommand("JOIN|Player1"); // Отправка команды на присоединение к игре
        }

        /// <summary>
        /// Останавливает клиента и уведомляет сервер о выходе.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            SendCommand("LEAVE|Player1"); // Отправка команды на выход из игры
            _client.Close(); // Закрытие UDP-клиента
        }

        /// <summary>
        /// Метод для получения данных от сервера.
        /// </summary>
        private void ReceiveData()
        {
            while (_isRunning)
            {
                try
                {
                    var serverEndPoint = new IPEndPoint(IPAddress.Any, 0); // Эндпоинт для получения данных
                    var data = _client.Receive(ref serverEndPoint); // Получение данных от сервера
                    var message = Encoding.UTF8.GetString(data); // Преобразование данных в строку

                    Console.WriteLine($"Получено сообщение от сервера: {message}");
                    HandleServerMessage(message); // Обработка сообщения от сервера
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Отправка команды на сервер.
        /// </summary>
        /// <param name="command">Команда для отправки.</param>
        private void SendCommand(string command)
        {
            var data = Encoding.UTF8.GetBytes(command); // Преобразование команды в массив байтов
            _client.Send(data, data.Length, _serverEndPoint); // Отправка команды на сервер
            Console.WriteLine($"Отправлена команда: {command}");
        }

        /// <summary>
        /// Обработка сообщений от сервера.
        /// </summary>
        /// <param name="message">Полученное сообщение от сервера.</param>
        private void HandleServerMessage(string message)
        {
            var parts = message.Split('|'); // Разделение сообщения на части
            var command = parts[0]; // Команда в сообщении
            var payload = parts.Length > 1 ? parts[1] : string.Empty; // Полезная нагрузка

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

        /// <summary>
        /// Отправка данных о ходе игрока.
        /// </summary>
        /// <param name="moveData">Данные о ходе игрока.</param>
        public void SendMove(string moveData)
        {
            SendCommand($"MOVE|{moveData}"); // Отправка хода на сервер
        }
    }
}
