﻿using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System;
using System.Linq;

public class GameServer
{
    private UdpClient _server;
    private readonly int _port;
    private readonly Dictionary<string, GameSession> _sessions;
    private readonly List<string> _topScores;
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
        // Проверить, не находится ли игрок уже в сессии
        foreach (var session in _sessions.Values)
        {
            if (session.Player1.Equals(senderEndPoint) || (session.Player2?.Equals(senderEndPoint) ?? false))
            {
                Console.WriteLine($"Игрок {senderEndPoint} уже в сессии.");
                return;
            }
        }

        // Найти свободную сессию
        var availableSession = _sessions.Values.FirstOrDefault(s => !s.IsFull);

        if (availableSession != null)
        {
            // Добавить игрока в свободную сессию
            availableSession.AddPlayer(senderEndPoint);
            Console.WriteLine($"Игрок {senderEndPoint} добавлен в сессию.");
            SendMessage("START|2", senderEndPoint); // Сообщить игроку, что игра началась
        }
        else
        {
            // Создать новую сессию
            var sessionId = Guid.NewGuid().ToString();
            var newSession = new GameSession(senderEndPoint);
            _sessions[sessionId] = newSession;
            Console.WriteLine($"Создана новая сессия {sessionId} для игрока {senderEndPoint}.");
            SendMessage("START|1", senderEndPoint); // Сообщить игроку, что он ждёт второго игрока
        }
    }

    private void HandleMove(string payload, IPEndPoint senderEndPoint)
    {
        var session = _sessions.Values.FirstOrDefault(s =>
            s.Player1.Equals(senderEndPoint) || (s.Player2?.Equals(senderEndPoint) ?? false));

        if (session == null)
        {
            Console.WriteLine($"Игрок {senderEndPoint} не найден в сессии.");
            return;
        }

        // Обновить состояние игры
        session.UpdateState(payload, senderEndPoint);

        // Если игрок проиграл
        if (session.IsGameOverFor(senderEndPoint))
        {
            session.EndSession("Game over", senderEndPoint);
            _sessions.Remove(session.SessionId); // Удалить сессию
        }
        else
        {
            // Переслать ход другому игроку
            var opponent = session.Player1.Equals(senderEndPoint) ? session.Player2 : session.Player1;
            if (opponent != null)
            {
                SendMessage($"MOVE|{payload}", opponent);
            }
        }
    }

    private void HandleLeave(string payload, IPEndPoint senderEndPoint)
    {
        var sessionId = _sessions.FirstOrDefault(s =>
            s.Value.Player1.Equals(senderEndPoint) || (s.Value.Player2?.Equals(senderEndPoint) ?? false)).Key;

        if (sessionId != null)
        {
            _sessions.Remove(sessionId);
            Console.WriteLine($"Игрок {senderEndPoint} покинул сессию {sessionId}.");
            SendMessage("END|Opponent left", senderEndPoint);
        }
    }

    private void SendMessage(string message, IPEndPoint recipient)
    {
        var data = Encoding.UTF8.GetBytes(message);
        _server.Send(data, data.Length, recipient);
        Console.WriteLine($"Отправлено сообщение: {message} -> {recipient}");
    }
}

public class GameSession
{
    public IPEndPoint Player1 { get; set; }
    public IPEndPoint Player2 { get; set; }
    public bool IsFull => Player1 != null && Player2 != null;
    public string SessionId { get; private set; }
    private string _gameState; // Состояние игры

    public GameSession(IPEndPoint player1)
    {
        Player1 = player1;
        SessionId = Guid.NewGuid().ToString(); // Генерация уникального ID для сессии
    }

    public void AddPlayer(IPEndPoint player2)
    {
        Player2 = player2;
    }

    // Обновление состояния игры
    public void UpdateState(string moveData, IPEndPoint sender)
    {
        Console.WriteLine($"Игрок {sender} совершил ход: {moveData}");
        _gameState = moveData; // Здесь можно обновить состояние игры на основе данных
    }

    // Проверка окончания игры для конкретного игрока
    public bool IsGameOverFor(IPEndPoint sender)
    {
        Console.WriteLine($"Проверка завершения игры для {sender}");
        return false; // Например, можно возвращать true, если игрок столкнулся с чем-то
    }

    public void EndSession(string reason, IPEndPoint senderEndPoint)
    {
        var opponent = Player1.Equals(senderEndPoint) ? Player2 : Player1;
        if (opponent != null)
        {
            // Используем GameServer для отправки сообщения
            Console.WriteLine($"Сессия завершена. Причина: {reason}");
        }
    }
}
