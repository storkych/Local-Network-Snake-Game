using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Server
{
    private static Socket serverSocket;
    private static readonly int port = 7777;
    private static readonly string ipAddress = "192.168.1.77";
    private static readonly int maxPlayersPerSession = 2;
    private static readonly List<GameSession> gameSessions = new List<GameSession>();

    static async Task Main(string[] args)
    {
        await SetupServer();
        await StartAcceptingClients();
    }

    private static async Task SetupServer()
    {
        // Создаем сокет для работы с TCP
        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        serverSocket.Bind(new IPEndPoint(IPAddress.Parse(ipAddress), port));
        serverSocket.Listen(10); // Максимум 10 ожиданий подключения
        Console.WriteLine("Server started...");
    }

    private static async Task StartAcceptingClients()
    {
        while (true)
        {
            // Ожидаем подключения клиента
            Socket clientSocket = await Task.Factory.FromAsync<Socket>(serverSocket.BeginAccept, serverSocket.EndAccept, null);
            Console.WriteLine("Player connected.");

            // Создаем объект NetworkStream для работы с сокетом
            NetworkStream networkStream = new NetworkStream(clientSocket);

            // Получаем сообщение от клиента
            byte[] buffer = new byte[1024];
            int receivedBytes = await networkStream.ReadAsync(buffer, 0, buffer.Length);
            string receivedMessage = Encoding.ASCII.GetString(buffer, 0, receivedBytes);
            Console.WriteLine($"Received: {receivedMessage}");

            bool sessionFound = false;
            GameSession sessionToAssign = null;
            int playerId = -1;
            int sessionId = -1;

            // Попытаться найти сессию, которая не полна
            foreach (var session in gameSessions)
            {
                if (session.Players.Count < maxPlayersPerSession)
                {
                    session.Players.Add(clientSocket);
                    sessionToAssign = session;
                    sessionFound = true;
                    sessionId = gameSessions.IndexOf(session);
                    Console.WriteLine($"Session {sessionId} found, Player connected.");

                    playerId = session.Players.Count - 1;
                    await SendMessageAsync($"YOUR_ID|{playerId}", networkStream);

                    // Если сессия полна, начинаем игру
                    if (session.Players.Count == maxPlayersPerSession)
                    {
                        Console.WriteLine($"Session {sessionId} started, both players connected.");
                        StartGameSession(session);

                        foreach (var player in session.Players)
                        {
                            await SendMessageAsync("GAMESTATE|READY", new NetworkStream(player));
                        }
                    }
                    break;
                }
            }

            // Если сессии нет или все сессии полны, создаем новую сессию
            if (!sessionFound)
            {
                var newSession = new GameSession();
                newSession.Players.Add(clientSocket);
                gameSessions.Add(newSession);
                sessionToAssign = newSession;
                sessionId = gameSessions.Count - 1;
                Console.WriteLine($"New session {sessionId} created, player connected.");

                playerId = 0;
                await SendMessageAsync($"YOUR_ID|{playerId}", networkStream);

                // Если сессия сразу полна, начинаем игру
                if (newSession.Players.Count == maxPlayersPerSession)
                {
                    Console.WriteLine($"Session {sessionId} started, both players connected.");
                    StartGameSession(newSession);

                    foreach (var player in newSession.Players)
                    {
                        await SendMessageAsync("GAMESTATE|READY", new NetworkStream(player));
                    }
                }
            }

            // Слушаем команды от игрока
            _ = Task.Run(() => ListenForCommands(sessionToAssign, clientSocket, playerId, sessionId));
        }
    }

    private static async Task ListenForCommands(GameSession session, Socket clientSocket, int playerId, int sessionId)
    {
        NetworkStream networkStream = new NetworkStream(clientSocket);
        byte[] buffer = new byte[1024];

        while (true)
        {
            int receivedBytes = await networkStream.ReadAsync(buffer, 0, buffer.Length);

            if (receivedBytes == 0)
            {
                // Если получено 0 байт, это означает, что клиент отключился
                Console.WriteLine($"[Session {sessionId}] Player {playerId} disconnected abruptly (connection closed).");
                break;
            }

            string receivedMessage = Encoding.ASCII.GetString(buffer, 0, receivedBytes);
            Console.WriteLine($"[Session {sessionId}] Player {playerId} - Received command: {receivedMessage}");

            string[] messageParts = receivedMessage.Split("|");
            if (messageParts.Length == 2)
            {
                string commandType = messageParts[0];
                string command = messageParts[1];

                if (commandType == "GAMESTATE")
                {
                    if (command == "READY")
                    {
                        Console.WriteLine($"[Session {sessionId}] Player {playerId} is ready.");
                    }
                    else if (command == "PRESS_START")
                    {
                        foreach (var player in session.Players)
                        {
                            await SendMessageAsync("GAMESTATE|START", new NetworkStream(player));
                        }
                    }
                    else if (command == "PRESS_RESTART")
                    {
                        foreach (var player in session.Players)
                        {
                            await SendMessageAsync("GAMESTATE|RESTART", new NetworkStream(player));
                        }
                    }
                    else if (command == "GAME_OVER")
                    {
                        foreach (var player in session.Players)
                        {
                            await SendMessageAsync("GAMESTATE|GAME_OVER", new NetworkStream(player));
                        }
                    }
                }
                else if (commandType == "DIRECTION")
                {
                    foreach (var player in session.Players)
                    {
                        if (!player.Equals(clientSocket)) // Игнорируем отправителя
                        {
                            await SendMessageAsync($"DIRECTION|{command}", new NetworkStream(player));
                        }
                    }
                }
                else if (commandType == "FOOD_POSITION")
                {
                    foreach (var player in session.Players)
                    {
                        await SendMessageAsync($"FOOD_POSITION|{command}", new NetworkStream(player));
                    }
                }
                else if (commandType == "DISCONNECT")
                {
                    // Отключаем игрока
                    Console.WriteLine($"[Session {sessionId}] Player {playerId} disconnected.");
                    session.Players.Remove(clientSocket);

                    // Отправляем уведомление оставшимся игрокам
                    foreach (var player in session.Players)
                    {
                        await SendMessageAsync("GAMESTATE|PLAYER_DISCONNECTED", new NetworkStream(player));
                    }

                    // Закрываем сокет
                    clientSocket.Close();

                    // Если остался только один игрок, завершаем сессию
                    if (session.Players.Count < 2)
                    {
                        Console.WriteLine($"[Session {sessionId}] Player left. Ending the game.");
                        foreach (var player in session.Players)
                        {
                            player.Close();
                        }
                        gameSessions.Remove(session);  // Удаляем сессию из списка
                    }
                    break; // Выходим из цикла, так как игрок отключился
                }
                else
                {
                    Console.WriteLine($"[Session {sessionId}] Unknown command: {receivedMessage}");
                }
            }
        }
    }

    private static void StartGameSession(GameSession session)
    {
        // Логика старта игры
    }

    private static async Task SendMessageAsync(string message, NetworkStream networkStream)
    {
        try
        {
            // Разделитель \n
            string messageWithSeparator = message + "\n";
            byte[] messageBytes = Encoding.ASCII.GetBytes(messageWithSeparator);
            await networkStream.WriteAsync(messageBytes, 0, messageBytes.Length);
            Console.WriteLine($"Sent: {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }
}

public class GameSession
{
    public List<Socket> Players { get; set; }

    public GameSession()
    {
        Players = new List<Socket>();
    }
}
