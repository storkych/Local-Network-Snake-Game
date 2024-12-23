﻿using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System;
using System.Linq;

public class GameServer
{
    // UDP-сервер для обработки клиентских запросов.
    private UdpClient _server;
    // Порт, на котором работает сервер.
    private readonly int _port;
    // Словарь с сессиями игры.
    private readonly Dictionary<string, GameSession> _sessions;
    // Список лучших результатов.
    private readonly List<string> _topScores;
    // Флаг, указывающий на состояние сервера.
    private bool _isRunning;

    /// <summary>
    /// Конструктор GameServer, инициализирующий сервер на указанном порту.
    /// </summary>
    /// <param name="port">Порт для работы сервера.</param>
    public GameServer(int port)
    {
        _port = port;
        _server = new UdpClient(_port);
        _sessions = new Dictionary<string, GameSession>();
        _topScores = new List<string>();
    }

    /// <summary>
    /// Запускает сервер и начинает прослушивание входящих сообщений.
    /// </summary>
    public void Start()
    {
        _isRunning = true;
        Console.WriteLine($"Сервер запущен на порту {_port}.");
        var receiveThread = new Thread(ReceiveData);
        receiveThread.Start();
    }

    /// <summary>
    /// Останавливает работу сервера.
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _server.Close();
        Console.WriteLine("Сервер остановлен.");
    }

    /// <summary>
    /// Метод для получения данных от клиентов.
    /// </summary>
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

    /// <summary>
    /// Обрабатывает входящее сообщение в зависимости от команды.
    /// </summary>
    /// <param name="message">Полученное сообщение.</param>
    /// <param name="senderEndPoint">Точка отправителя сообщения.</param>
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
            case "START":
                foreach (var session in _sessions.Values)
                {
                    if (session.Player1.Equals(senderEndPoint) || (session.Player2?.Equals(senderEndPoint) ?? false))
                    {
                        SendMessage("GAME|START", session.Player1);
                        SendMessage("GAME|START", session.Player2);
                        return;
                    }
                }
                break;
            default:
                Console.WriteLine($"Неизвестная команда: {command}");
                break;
        }
    }

    /// <summary>
    /// Обработка команды игрока на присоединение к сессии.
    /// </summary>
    /// <param name="payload">Данные о присоединении.</param>
    /// <param name="senderEndPoint">Точка отправителя.</param>
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
            // Добавить игрока в свободную сессию.
            availableSession.AddPlayer(senderEndPoint);
            Console.WriteLine($"Игрок {senderEndPoint} добавлен в сессию.");

            // Уведомить игрока о его роли.
            SendMessage("ASSIGN_ROLE|Player2", senderEndPoint);

            // Уведомить первого игрока о старте игры.
            SendMessage("GAME|READY", availableSession.Player1);
            SendMessage("GAME|READY", senderEndPoint);
        }
        else
        {
            // Создать новую сессию.
            var sessionId = Guid.NewGuid().ToString();
            var newSession = new GameSession(senderEndPoint);
            _sessions[sessionId] = newSession;
            Console.WriteLine($"Создана новая сессия {sessionId} для игрока {senderEndPoint}.");

            // Уведомить первого игрока о его роли.
            SendMessage("ASSIGN_ROLE|Player1", senderEndPoint);

            // Уведомить игрока, что он ждёт второго игрока.
            SendMessage("GAME_WAIT", senderEndPoint);
        }
    }

    /// <summary>
    /// Обработка команды движения игрока.
    /// </summary>
    /// <param name="payload">Данные о действии игрока.</param>
    /// <param name="senderEndPoint">Точка отправителя.</param>
    private void HandleMove(string payload, IPEndPoint senderEndPoint)
    {
        var session = _sessions.Values.FirstOrDefault(s =>
            s.Player1.Equals(senderEndPoint) || (s.Player2?.Equals(senderEndPoint) ?? false));

        if (session == null)
        {
            Console.WriteLine($"Игрок {senderEndPoint} не найден в сессии.");
            return;
        }
        Console.WriteLine($"HandleMove");
        // Обновить состояние игры.
        session.UpdateState(payload, senderEndPoint);

        // Если игрок проиграл.
        if (session.IsGameOverFor(senderEndPoint))
        {
            session.EndSession("Game over", senderEndPoint);
            // Удалить сессию.
            _sessions.Remove(session.SessionId);
        }
        else
        {
            // Передать ход другому игроку.
            var opponent = session.Player1.Equals(senderEndPoint) ? session.Player2 : session.Player1;
            if (opponent != null)
            {
                SendMessage($"MOVE|{payload}", opponent);
            }
        }
    }

    /// <summary>
    /// Обработка команды выхода игрока из сессии.
    /// </summary>
    /// <param name="payload">Данные об уходе.</param>
    /// <param name="senderEndPoint">Точка отправителя.</param>
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

    /// <summary>
    /// Отправка сообщения клиенту.
    /// </summary>
    /// <param name="message">Сообщение для отправки.</param>
    /// <param name="recipient">Получатель сообщения.</param>
    private void SendMessage(string message, IPEndPoint recipient)
    {
        var data = Encoding.UTF8.GetBytes(message);
        _server.Send(data, data.Length, recipient);
        Console.WriteLine($"Отправлено сообщение: {message} -> {recipient}");
    }
}

public class GameSession
{
    // Первый игрок в сессии.
    public IPEndPoint Player1 { get; set; }
    // Второй игрок в сессии.
    public IPEndPoint Player2 { get; set; }
    // Проверка, заполнена ли сессия.
    public bool IsFull => Player1 != null && Player2 != null;
    // Идентификатор сессии.
    public string SessionId { get; private set; }
    // Состояние игры.
    private string _gameState;

    /// <summary>
    /// Конструктор GameSession, инициализирующий сессию для первого игрока.
    /// </summary>
    /// <param name="player1">Первый игрок.</param>
    public GameSession(IPEndPoint player1)
    {
        Player1 = player1;
        // Генерация уникального ID для сессии.
        SessionId = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Добавление второго игрока в сессию.
    /// </summary>
    /// <param name="player2">Второй игрок.</param>
    public void AddPlayer(IPEndPoint player2)
    {
        Player2 = player2;
    }

    /// <summary>
    /// Обновление состояния игры на основе действия игрока.
    /// </summary>
    /// <param name="moveData">Данные о ходе игрока.</param>
    /// <param name="sender">Точка отправителя.</param>
    public void UpdateState(string moveData, IPEndPoint sender)
    {
        Console.WriteLine($"Игрок {sender} совершил ход: {moveData}");
        _gameState = moveData;
    }

    /// <summary>
    /// Проверка, закончилась ли игра для конкретного игрока.
    /// </summary>
    /// <param name="sender">Точка отправителя.</param>
    /// <returns>True, если игра закончилась для игрока, иначе - false.</returns>
    public bool IsGameOverFor(IPEndPoint sender)
    {
        Console.WriteLine($"Проверка завершения игры для {sender}");
        return false;
    }

    /// <summary>
    /// Завершение сессии с указанием причины.
    /// </summary>
    /// <param name="reason">Причина окончания сессии.</param>
    /// <param name="senderEndPoint">Точка отправителя.</param>
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
