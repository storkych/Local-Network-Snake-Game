using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;

/// <summary>
/// Представляет сервер игры, который управляет игровыми сессиями.
/// </summary>
public class GameServer
{
    private const int Port = 8080; // Порт, на котором сервер будет прослушивать входящие сообщения.
    private Socket _socket; // Сокет для работы с UDP.
    private readonly Dictionary<string, GameSession> _sessions; // Словарь игровых сессий.
    private bool _isRunning; // Флаг, указывающий на состояние работы сервера.

    /// <summary>
    /// Инициализирует новый экземпляр класса GameServer.
    /// </summary>
    public GameServer()
    {
        _sessions = new Dictionary<string, GameSession>();
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(new IPEndPoint(IPAddress.Any, Port));
    }

    /// <summary>
    /// Запускает сервер и начинает получать сообщения.
    /// </summary>
    public void Start()
    {
        _isRunning = true;
        Console.WriteLine($"Сервер запущен на порту {Port}.");

        var receiveThread = new Thread(ReceiveData);
        receiveThread.Start();
    }

    /// <summary>
    /// Останавливает сервер.
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _socket.Close();
        Console.WriteLine("Сервер остановлен.");
    }

    /// <summary>
    /// Получает данные от клиентов и передает их на обработку.
    /// </summary>
    private void ReceiveData()
    {
        while (_isRunning)
        {
            try
            {
                var buffer = new byte[1024];
                EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                int receivedBytes = _socket.ReceiveFrom(buffer, ref endPoint);
                var message = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                Console.WriteLine($"Получено сообщение: {message} от {endPoint}");

                HandleMessage(message, (IPEndPoint)endPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Обрабатывает полученные сообщения от клиентов.
    /// </summary>
    /// <param name="message">Полученное сообщение.</param>
    /// <param name="senderEndPoint">Адрес отправителя.</param>
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
    /// Обрабатывает запрос на присоединение игрока к сессии.
    /// </summary>
    /// <param name="payload">Имя игрока.</param>
    /// <param name="senderEndPoint">Адрес отправителя.</param>
    private void HandleJoin(string payload, IPEndPoint senderEndPoint)
    {
        foreach (var session in _sessions.Values)
        {
            if (session.Player1.Equals(senderEndPoint) || (session.Player2?.Equals(senderEndPoint) ?? false))
            {
                Console.WriteLine($"Игрок {senderEndPoint} уже в сессии.");
                return;
            }
        }

        var availableSession = _sessions.Values.FirstOrDefault(s => !s.IsFull);

        if (availableSession != null)
        {
            availableSession.AddPlayer(senderEndPoint);
            Console.WriteLine($"Игрок {senderEndPoint} добавлен в сессию.");
            SendMessage("ASSIGN_ROLE|Player2", senderEndPoint);
            SendMessage("GAME|READY", availableSession.Player1);
            SendMessage("GAME|READY", senderEndPoint);
        }
        else
        {
            var sessionId = Guid.NewGuid().ToString();
            var newSession = new GameSession(senderEndPoint);
            _sessions[sessionId] = newSession;
            Console.WriteLine($"Создана новая сессия {sessionId} для игрока {senderEndPoint}.");
            SendMessage("ASSIGN_ROLE|Player1", senderEndPoint);
            SendMessage("GAME_WAIT", senderEndPoint);
        }
    }

    /// <summary>
    /// Обрабатывает движение игрока.
    /// </summary>
    /// <param name="payload">Данные о движении.</param>
    /// <param name="senderEndPoint">Адрес отправителя.</param>
    private void HandleMove(string payload, IPEndPoint senderEndPoint)
    {
        var session = _sessions.Values.FirstOrDefault(s =>
            s.Player1.Equals(senderEndPoint) || (s.Player2?.Equals(senderEndPoint) ?? false));

        if (session == null)
        {
            Console.WriteLine($"Игрок {senderEndPoint} не найден в сессии.");
            return;
        }

        session.UpdateState(payload, senderEndPoint);

        if (session.IsGameOverFor(senderEndPoint))
        {
            session.EndSession("Game over", senderEndPoint);
            _sessions.Remove(session.SessionId);
        }
        else
        {
            var opponent = session.Player1.Equals(senderEndPoint) ? session.Player2 : session.Player1;
            if (opponent != null)
            {
                SendMessage($"MOVE|{payload}", opponent);
            }
        }
    }

    /// <summary>
    /// Обрабатывает уход игрока из сессии.
    /// </summary>
    /// <param name="payload">Ничего не содержит.</param>
    /// <param name="senderEndPoint">Адрес отправителя.</param>
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
    /// Отправляет сообщение указанному получателю.
    /// </summary>
    /// <param name="message">Сообщение для отправки.</param>
    /// <param name="recipient">Адрес получателя.</param>
    private void SendMessage(string message, IPEndPoint recipient)
    {
        var data = Encoding.UTF8.GetBytes(message);
        _socket.SendTo(data, recipient);
        Console.WriteLine($"Отправлено сообщение: {message} -> {recipient}");
    }
}

/// <summary>
/// Представляет игровую сессию между двумя игроками.
/// </summary>
public class GameSession
{
    public IPEndPoint Player1 { get; private set; } // Первый игрок.
    public IPEndPoint Player2 { get; private set; } // Второй игрок.
    public bool IsFull => Player1 != null && Player2 != null; // Проверяет, полна ли сессия.
    public string SessionId { get; private set; } // Идентификатор сессии.
    private string _gameState; // Состояние игры.

    /// <summary>
    /// Инициализирует новый экземпляр класса GameSession с первым игроком.
    /// </summary>
    /// <param name="player1">Адрес первого игрока.</param>
    public GameSession(IPEndPoint player1)
    {
        Player1 = player1;
        SessionId = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Добавляет второго игрока в сессию.
    /// </summary>
    /// <param name="player2">Адрес второго игрока.</param>
    public void AddPlayer(IPEndPoint player2)
    {
        Player2 = player2;
    }

    /// <summary>
    /// Обновляет состояние игры на основании движения игрока.
    /// </summary>
    /// <param name="moveData">Данные о движении.</param>
    /// <param name="sender">Адрес игрока, совершившего движение.</param>
    public void UpdateState(string moveData, IPEndPoint sender)
    {
        Console.WriteLine($"Игрок {sender} совершил ход: {moveData}");
        _gameState = moveData;
    }

    /// <summary>
    /// Проверяет, закончилась ли игра для указанного игрока.
    /// </summary>
    /// <param name="sender">Адрес проверяемого игрока.</param>
    /// <returns>Истина, если игра закончилась, иначе ложь.</returns>
    public bool IsGameOverFor(IPEndPoint sender)
    {
        Console.WriteLine($"Проверка завершения игры для {sender}");
        return false; // Логика завершения игры.
    }

    /// <summary>
    /// Завершает сессию игры и уведомляет участников.
    /// </summary>
    /// <param name="reason">Причина завершения игры.</param>
    /// <param name="senderEndPoint">Адрес игрока, завершившего игру.</param>
    public void EndSession(string reason, IPEndPoint senderEndPoint)
    {
        var opponent = Player1.Equals(senderEndPoint) ? Player2 : Player1;
        if (opponent != null)
        {
            // Логика для завершения сессии.
            Console.WriteLine($"Сессия завершена. Причина: {reason}");
        }
    }
}
