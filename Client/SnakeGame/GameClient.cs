using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SnakeGame
{
    public class GameClient
    {
        private Socket _socket; // Сокет для работы с клиентом.
        private IPEndPoint _serverEndPoint; // Конечная точка сервера.
        private const int Port = 8080; // Порт сервера.

        // Экземпляр игрового движка.
        private GameEngine gameEngine;
        public string role = ""; // Роль игрока.
        public int playerID; // Идентификатор игрока.
        public bool secondPlayer = false; // Признак второго игрока.
        public bool waitingGame = false; // Признак ожидания игры.

        public GameClient()
        {
            // Инициализация сокета.
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // Установка конечной точки для сервера.
            _serverEndPoint = new IPEndPoint(IPAddress.Parse("192.168.1.77"), Port);
            // Создание экземпляра игрового движка.
            gameEngine = new GameEngine(this);
        }

        public void Start()
        {
            // Отправка сообщения о присоединении.
            SendMessage("JOIN|Player1");
            // Создание нового потока для получения данных от сервера.
            var receiveThread = new Thread(ReceiveData);
            receiveThread.Start();
            // Запуск игрового движка.
            gameEngine.Run();
        }

        private void SendMessage(string message)
        {
            // Преобразование строки в байтовый массив.
            byte[] data = Encoding.UTF8.GetBytes(message);
            // Отправка сообщения на сервер.
            _socket.SendTo(data, _serverEndPoint);
            // Вывод информации в консоль.
            //Console.WriteLine($"Отправлено сообщение: {message}");
        }

        public void StartGame()
        {
            // Отправка сообщения о начале игры.
            SendMessage("START|NOW");
        }

        public void SendDirection(Direction direction)
        {
            string stringDirection = direction.ToString().ToUpper(); // Преобразование направления в строку.

            // Формирование сообщения о движении.
            string message = $"MOVE|{stringDirection}";
            SendMessage(message);
        }

        private void ReceiveData()
        {
            // Бесконечный цикл для ожидания данных.
            while (true)
            {
                try
                {
                    var buffer = new byte[1024]; // Буфер для получения данных.
                    EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    // Получение данных от сервера.
                    int receivedBytes = _socket.ReceiveFrom(buffer, ref remoteEP);
                    // Преобразование байтов в строку.
                    string message = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                    // Обработка полученного сообщения.
                    HandleMessage(message, (IPEndPoint)remoteEP);
                }
                catch (Exception ex)
                {
                    // Вывод сообщения об ошибке.
                    Console.WriteLine("Ошибка при получении данных: " + ex.Message);
                }
            }
        }

        private void HandleMessage(string message, IPEndPoint senderEndPoint)
        {
            // Вывод сообщения в консоль.
            //Console.WriteLine($"Получено сообщение: {message} от {senderEndPoint}");
            // Разделение сообщения на части.
            var parts = message.Split('|');
            if (parts.Length < 2) return;

            var command = parts[0];
            var payload = parts[1];

            switch (command)
            {
                case "ASSIGN_ROLE":
                    HandleRole(payload, senderEndPoint);
                    break;
                case "GAME":
                    HandleStart(payload, senderEndPoint);
                    break;
                case "MOVE":
                    HandleMove(payload, senderEndPoint);
                    break;
                case "LEAVE":
                    break;
                default:
                    Console.WriteLine($"Неизвестная команда: {command}");
                    break;
            }
        }

        private void HandleRole(string payload, IPEndPoint senderEndPoint)
        {
            role = payload;
            if (payload == "Player1")
            {
                playerID = 0;
                gameEngine.isClientHost = true;
                gameEngine.gameState = GameState.MainMenu;
            }
            else if (payload == "Player2")
            {
                playerID = 1;
                gameEngine.isClientHost = false;
                gameEngine.gameState = GameState.MainMenu;
            }
        }

        private void HandleMove(string payload, IPEndPoint senderEndPoint)
        {
            Direction newDir = Direction.Right;

            // Установка нового направления в зависимости от полученного сообщения.
            if (payload == "UP") newDir = Direction.Up;
            else if (payload == "DOWN") newDir = Direction.Down;
            else if (payload == "LEFT") newDir = Direction.Left;
            else if (payload == "RIGHT") newDir = Direction.Right;

            // Обновление направления противника.
            gameEngine.opponentDirection = newDir;
        }

        private void HandleStart(string payload, IPEndPoint senderEndPoint)
        {
            if (payload == "WAIT")
            {
                secondPlayer = false;
                gameEngine.gameState = GameState.MainMenu;
            }
            else if (payload == "READY")
            {
                secondPlayer = true;
                gameEngine.gameState = GameState.MainMenu;
            }
            else if (payload == "START")
            {
                Console.WriteLine("Игра началась!");
                gameEngine.gameState = GameState.InGame;
            }
        }
    }
}
