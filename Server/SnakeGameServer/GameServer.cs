using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;

public class GameServer
{
    private const int Port = 8080;
    private Socket _socket;
    private readonly Dictionary<string, GameSession> _sessions;
    private bool _isRunning;

    public GameServer()
    {
        _sessions = new Dictionary<string, GameSession>();
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.Bind(new IPEndPoint(IPAddress.Any, Port));
    }

    public void Start()
    {
        _isRunning = true;
        Console.WriteLine($"Сервер запущен на порту {Port}.");

        var receiveThread = new Thread(ReceiveData);
        receiveThread.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        _socket.Close();
        Console.WriteLine("Сервер остановлен.");
    }

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
        _socket.SendTo(data, recipient);
        Console.WriteLine($"Отправлено сообщение: {message} -> {recipient}");
    }
}

public class GameSession
{
    public IPEndPoint Player1 { get; private set; }
    public IPEndPoint Player2 { get; private set; }
    public bool IsFull => Player1 != null && Player2 != null;
    public string SessionId { get; private set; }
    private string _gameState;

    public GameSession(IPEndPoint player1)
    {
        Player1 = player1;
        SessionId = Guid.NewGuid().ToString();
    }

    public void AddPlayer(IPEndPoint player2)
    {
        Player2 = player2;
    }

    public void UpdateState(string moveData, IPEndPoint sender)
    {
        Console.WriteLine($"Игрок {sender} совершил ход: {moveData}");
        _gameState = moveData;
    }

    public bool IsGameOverFor(IPEndPoint sender)
    {
        Console.WriteLine($"Проверка завершения игры для {sender}");
        return false; // Логика завершения игры
    }

    public void EndSession(string reason, IPEndPoint senderEndPoint)
    {
        var opponent = Player1.Equals(senderEndPoint) ? Player2 : Player1;
        if (opponent != null)
        {
            // Логика для завершения сессии
            Console.WriteLine($"Сессия завершена. Причина: {reason}");
        }
    }
}
