﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SnakeGame
{
    public class GameServer
    {
        private UdpClient _server;
        private readonly int _port;
        private readonly Dictionary<string, GameSession> _sessions; // Активные сессии
        private readonly List<string> _topScores; // Таблица рекордов
        private bool _isRunning;

        public GameServer(int port)
        {
            _port = port;
            _server = new UdpClient(_port);
            _sessions = new Dictionary<string, GameSession>();
            _topScores = new List<string>();
        }

        public void Start()
        {
            _isRunning = true;
            Console.WriteLine($"Сервер запущен на порту {_port}.");
            var receiveThread = new Thread(ReceiveData);
            receiveThread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
            _server.Close();
            Console.WriteLine("Сервер остановлен.");
        }

        private void ReceiveData()
        {
            while (_isRunning)
            {
                try
                {
                    var endPoint = new IPEndPoint(IPAddress.Any, 0);
                    var data = _server.Receive(ref endPoint);
                    var message = Encoding.UTF8.GetString(data);
                    Console.WriteLine($"Получено сообщение: {message} от {endPoint}");

                    HandleMessage(message, endPoint);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }
            }
        }

        private void HandleMessage(string message, IPEndPoint senderEndPoint)
        {
            var parts = message.Split('|');
            if (parts.Length < 2) return;

            var command = parts[0];
            var payload = parts[1];

            switch (command)
            {
                case "JOIN":
                    HandleJoin(payload, senderEndPoint);
                    break;
                case "MOVE":
                    HandleMove(payload, senderEndPoint);
                    break;
                case "LEAVE":
                    HandleLeave(payload, senderEndPoint);
                    break;
                default:
                    Console.WriteLine($"Неизвестная команда: {command}");
                    break;
            }
        }

        private void HandleJoin(string payload, IPEndPoint senderEndPoint)
        {
            Console.WriteLine($"Игрок подключился: {senderEndPoint}");
            // Создать или найти сессию и добавить игрока
        }

        private void HandleMove(string payload, IPEndPoint senderEndPoint)
        {
            Console.WriteLine($"Движение игрока: {payload}");
            // Обновить состояние игры
        }

        private void HandleLeave(string payload, IPEndPoint senderEndPoint)
        {
            Console.WriteLine($"Игрок покинул игру: {senderEndPoint}");
            // Удалить игрока из сессии
        }
    }

    public class GameSession
    {
        public IPEndPoint Player1 { get; set; }
        public IPEndPoint Player2 { get; set; }
        public bool IsFull => Player1 != null && Player2 != null;

        public GameSession(IPEndPoint player1)
        {
            Player1 = player1;
        }

        public void AddPlayer(IPEndPoint player2)
        {
            Player2 = player2;
        }
    }
}
