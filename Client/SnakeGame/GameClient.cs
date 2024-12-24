using System;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SnakeGame
{
    public class GameClient
    {
        // UDP-клиент для взаимодействия с сервером.
        private UdpClient udpClient;
        // Точка доступа к серверу.
        private IPEndPoint serverEndPoint;
        // Порт, на котором работает сервер.
        private const int port = 12345;

        // Экземпляр игрового движка.
        GameEngine gameEngine;
        // Роль игрока.
        public string role = "";
        // Идентификатор игрока.
        public int playerID;
        // Признак второго игрока.
        public bool secondPlayer = false;
        // Признак ожидания игры.
        public bool waitinGame = false;

        /// <summary>
        /// Конструктор GameClient, инициализирующий UDP-клиент.
        /// </summary>
        public GameClient()
        {
            // Инициализация UDP-клиента.
            udpClient = new UdpClient();
            // Установка конечной точки для сервера.
            serverEndPoint = new IPEndPoint(IPAddress.Loopback, port);
            // Создание экземпляра игрового движка.
            gameEngine = new GameEngine(this);
        }

        /// <summary>
        /// Запуск клиента и отправка сообщения о присоединении к игре.
        /// </summary>
        public void Start()
        {
            // Отправка сообщения о присоединении.
            SendMessage("JOIN|Player1");
            // Создание нового потока для получения данных.
            var receiveThread = new Thread(ReceiveData);
            // Запуск потока для обработки клиентских сообщений.
            receiveThread.Start();
            // Запуск игрового движка.
            gameEngine.Run();
        }

        /// <summary>
        /// Отправка сообщения на сервер.
        /// </summary>
        /// <param name="message">Сообщение для отправки на сервер.</param>
        private void SendMessage(string message)
        {
            // Преобразование строки в байтовый массив.
            byte[] data = Encoding.UTF8.GetBytes(message);
            // Отправка сообщения на сервер.
            udpClient.Send(data, data.Length, serverEndPoint);
            // Вывод информации в консоль.
            Console.WriteLine($"Отправлено сообщение: {message}");
        }

        /// <summary>
        /// Запуск игры и уведомление сервера об этом.
        /// </summary>
        public void StartGame()
        {
            // Отправка сообщения о начале игры.
            SendMessage("START|NOW");
        }

        /// <summary>
        /// Отправка направления движения игрока на сервер.
        /// </summary>
        /// <param name="direction">Направление движения игрока.</param>
        public void SendDirection(Direction direction)
        {
            // Строка направления.
            string stringDirection = "";
            if (direction == Direction.Up) stringDirection = "UP";
            else if (direction == Direction.Down) stringDirection = "DOWN";
            else if (direction == Direction.Left) stringDirection = "LEFT";
            else if (direction == Direction.Right) stringDirection = "RIGHT";
            // Формирование сообщения о движении.
            string message = $"MOVE|{stringDirection}";
            // Преобразование сообщения в байты.
            byte[] data = Encoding.UTF8.GetBytes(message);
            // Отправка сообщения о движении на сервер.
            udpClient.Send(data, data.Length, serverEndPoint);
        }

        /// <summary>
        /// Метод для получения данных от сервера.
        /// </summary>
        private void ReceiveData()
        {
            // Бесконечный цикл для ожидания данных.
            while (true)
            {
                try
                {
                    // Получение данных от сервера.
                    byte[] data = udpClient.Receive(ref serverEndPoint);
                    // Преобразование байтов в строку.
                    string message = Encoding.UTF8.GetString(data);
                    // Обработка полученного сообщения.
                    HandleMessage(message, serverEndPoint);
                }
                catch (Exception ex)
                {
                    // Вывод сообщения об ошибке.
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
            // Вывод сообщения в консоль.
            Console.WriteLine($"!!! {message}");
            // Разделение сообщения на части.
            var parts = message.Split('|');
            // Возвращение, если сообщение неполное.
            if (parts.Length < 2) return;

            // Получение команды.
            var command = parts[0];
            // Получение полезной нагрузки.
            var payload = parts[1];

            switch (command)
            {
                // Обработка роли игрока.
                case "ASSIGN_ROLE":
                    HandleRole(payload, senderEndPoint);
                    break;
                // Обработка начала игры.
                case "GAME":
                    HandleStart(payload, senderEndPoint);
                    break;
                // Обработка движения.
                case "MOVE":
                    HandleMove(payload, senderEndPoint);
                    break;
                // HandleLeave(payload, senderEndPoint); // Обработка выхода (временно закомментировано).
                case "LEAVE":
                    break;
                default:
                    // Сообщение об неизвестной команде.
                    Console.WriteLine($"Неизвестная команда: {command}");
                    break;
            }
        }

        /// <summary>
        /// Обработка назначения роли игрока.
        /// </summary>
        /// <param name="payload">Данные о роли.</param>
        /// <param name="senderEndPoint">Точка отправителя.</param>
        private void HandleRole(string payload, IPEndPoint senderEndPoint)
        {
            // Установка роли игрока.
            role = payload;
            if (payload == "Player1")
            {
                // Установка идентификатора для первого игрока.
                playerID = 0;
                // Установка флага клиент-ведущий.
                gameEngine.isClientHost = true;
                // Переход в главное меню игры.
                gameEngine.gameState = GameState.MainMenu;
            }
            else if (payload == "Player2")
            {
                // Установка идентификатора для второго игрока.
                playerID = 1;
                // Установка флага клиент-неведущий.
                gameEngine.isClientHost = false;
                // Переход в главное меню игры.
                gameEngine.gameState = GameState.MainMenu;
            }
        }

        /// <summary>
        /// Обработка команды движения игрока.
        /// </summary>
        /// <param name="payload">Данные о действии игрока.</param>
        /// <param name="senderEndPoint">Точка отправителя.</param>
        private void HandleMove(string payload, IPEndPoint senderEndPoint)
        {
            // Начальное направление вправо.
            Direction newDir = Direction.Right;

            // Установка направления в зависимости от полученного сообщения.
            if (payload == "UP") newDir = Direction.Up; 
            else if (payload == "DOWN") newDir = Direction.Down; 
            else if (payload == "LEFT") newDir = Direction.Left; 
            else if (payload == "RIGHT") newDir = Direction.Right; 

            // Обновление направления противника.
            gameEngine.opponentDirection = newDir;
        }

        /// <summary>
        /// Обработка начала игры.
        /// </summary>
        /// <param name="payload">Данные о началом игры.</param>
        /// <param name="senderEndPoint">Точка отправителя.</param>
        private void HandleStart(string payload, IPEndPoint senderEndPoint)
        {
            if (payload == "WAIT")
            {
                 // Установка флага ожидания второго игрока.
                secondPlayer = false;
                Console.WriteLine($"BOOL SECOND: {secondPlayer}");
                // Переход в главное меню.
                gameEngine.gameState = GameState.MainMenu;
            }
            else if (payload == "READY")
            {
                // Установка флага, что второй игрок готов.
                secondPlayer = true;
                Console.WriteLine($"BOOL SECOND: {secondPlayer}");
                // Переход в главное меню.
                gameEngine.gameState = GameState.MainMenu;
            }
            else if (payload == "START")
            {
                // Сообщение о начале игры.
                Console.WriteLine($"START GAME");
                Console.WriteLine($"BOOL SECOND: {gameEngine.gameState}");
                // Переход в состояние игры.
                gameEngine.gameState = GameState.InGame;
                Console.WriteLine($"BOOL SECOND: {gameEngine.gameState}");
            }
        }
    }
}

