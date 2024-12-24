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
        private UdpClient udpClient;
        private IPEndPoint serverEndPoint;
        private const int port = 12345;

        GameEngine gameEngine;
        public string role = "";
        public int playerID;
        public bool secondPlayer = false;
        public bool waitinGame = false;

        public GameClient()
        {
            udpClient = new UdpClient();
            serverEndPoint = new IPEndPoint(IPAddress.Loopback, port);
            gameEngine = new GameEngine(this);
        }

        public void Start()
        {
            SendMessage("JOIN|Player1");
            var receiveThread = new Thread(ReceiveData);
            receiveThread.Start();
            gameEngine.Run();
        }

        // Отправка сообщения на сервер
        private void SendMessage(string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, serverEndPoint);
            Console.WriteLine($"Отправлено сообщение: {message}");
        }

        public void StartGame()
        {
            SendMessage("START|NOW");
        }

        public void SendDirection(Direction direction)
        {
            string stringDirection = "";
            if (direction == Direction.Up) stringDirection = "UP";
            else if (direction == Direction.Down) stringDirection = "DOWN";
            else if (direction == Direction.Left) stringDirection = "LEFT";
            else if (direction == Direction.Right) stringDirection = "RIGHT";

            string message = $"MOVE|{stringDirection}";
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
            Console.WriteLine($"!!! {message}");
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
                    //HandleLeave(payload, senderEndPoint);
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

        /// <summary>
        /// Обработка команды движения игрока.
        /// </summary>
        /// <param name="payload">Данные о действии игрока.</param>
        /// <param name="senderEndPoint">Точка отправителя.</param>
        private void HandleMove(string payload, IPEndPoint senderEndPoint)
        {
            Direction newDir = Direction.Right;

            if (payload == "UP") newDir = Direction.Up;
            else if (payload == "DOWN") newDir = Direction.Down;
            else if (payload == "LEFT") newDir = Direction.Left;
            else if (payload == "RIGHT") newDir = Direction.Right;

            gameEngine.opponentDirection = newDir;
        }
        private void HandleStart(string payload, IPEndPoint senderEndPoint)
        {
            if (payload == "WAIT")
            {
                secondPlayer = false;
                Console.WriteLine($"BOOL SECOND: {secondPlayer}");
                gameEngine.gameState = GameState.MainMenu;
            }
            else if (payload == "READY")
            {
                secondPlayer = true;
                Console.WriteLine($"BOOL SECOND: {secondPlayer}");
                gameEngine.gameState = GameState.MainMenu;
            }
            else if (payload == "START")
            {
                Console.WriteLine($"START GAME");
                Console.WriteLine($"BOOL SECOND: {gameEngine.gameState}");
                gameEngine.gameState = GameState.InGame;
                Console.WriteLine($"BOOL SECOND: {gameEngine.gameState}");
            }

        }
    }
}
