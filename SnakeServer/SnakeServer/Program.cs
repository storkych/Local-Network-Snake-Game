using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class Server
{
    private static TcpListener tcpListener;
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
        tcpListener = new TcpListener(IPAddress.Parse(ipAddress), port);
        tcpListener.Start();
        Console.WriteLine("Server started...");
    }

    private static async Task StartAcceptingClients()
    {
        while (true)
        {
            TcpClient client = await tcpListener.AcceptTcpClientAsync();
            NetworkStream networkStream = client.GetStream();
            Console.WriteLine("Player connected.");

            // Получаем сообщение от клиента
            byte[] buffer = new byte[1024];
            int receivedBytes = await networkStream.ReadAsync(buffer, 0, buffer.Length);
            string receivedMessage = Encoding.ASCII.GetString(buffer, 0, receivedBytes);
            Console.WriteLine($"Received: {receivedMessage}");

            bool sessionFound = false;
            GameSession sessionToAssign = null;
            int playerId = -1;

            // Попытаться найти сессию, которая не полна
            foreach (var session in gameSessions)
            {
                if (session.Players.Count < maxPlayersPerSession)
                {
                    session.Players.Add(client);
                    sessionToAssign = session;
                    sessionFound = true;
                    Console.WriteLine("Player connected to existing session.");

                    playerId = 1;
                    await SendMessageAsync($"YOUR_ID|{playerId}", networkStream);

                    // Если сессия полна, начинаем игру
                    if (session.Players.Count == maxPlayersPerSession)
                    {
                        Console.WriteLine("Session started, both players connected.");
                        StartGameSession(session);

                        foreach (var player in session.Players)
                        {
                            await SendMessageAsync("GAMESTATE|READY", player.GetStream());
                        }
                    }
                    break;
                }
            }

            // Если сессии нет или все сессии полны, создаем новую сессию
            if (!sessionFound)
            {
                var newSession = new GameSession();
                newSession.Players.Add(client);
                gameSessions.Add(newSession);
                sessionToAssign = newSession;
                Console.WriteLine("New session created, player connected.");

                playerId = 0;
                await SendMessageAsync($"YOUR_ID|{playerId}", networkStream);

                // Если сессия сразу полна, начинаем игру
                if (newSession.Players.Count == maxPlayersPerSession)
                {
                    Console.WriteLine("Session started, both players connected.");
                    StartGameSession(newSession);

                    foreach (var player in newSession.Players)
                    {
                        await SendMessageAsync("GAMESTATE|READY", player.GetStream());
                    }
                }
            }

            // Слушаем команды от игрока
            _ = Task.Run(() => ListenForCommands(sessionToAssign, client, playerId));
        }
    }

    private static async Task ListenForCommands(GameSession session, TcpClient client, int playerId)
    {
        NetworkStream networkStream = client.GetStream();
        byte[] buffer = new byte[1024];

        while (true)
        {
            int receivedBytes = await networkStream.ReadAsync(buffer, 0, buffer.Length);
            string receivedMessage = Encoding.ASCII.GetString(buffer, 0, receivedBytes);
            Console.WriteLine($"Received command: {receivedMessage}");

            string[] messageParts = receivedMessage.Split("|");
            if (messageParts.Length == 2)
            {
                string commandType = messageParts[0];
                string command = messageParts[1];

                if (commandType == "GAMESTATE")
                {
                    if (command == "READY")
                    {
                        Console.WriteLine($"Player {playerId} is ready.");
                    }
                    else if (command == "PRESS_START")
                    {
                        foreach (var player in session.Players)
                        {
                            await SendMessageAsync("GAMESTATE|START", player.GetStream());
                        }
                    }
                    else if (command == "PRESS_RESTART")
                    {
                        foreach (var player in session.Players)
                        {
                            await SendMessageAsync("GAMESTATE|RESTART", player.GetStream());
                        }
                    }
                    else if (command == "GAME_OVER")
                    {
                        foreach (var player in session.Players)
                        {
                            await SendMessageAsync("GAMESTATE|GAME_OVER", player.GetStream());
                        }
                    }
                }
                else if (commandType == "DIRECTION")
                {
                    foreach (var player in session.Players)
                    {
                        if (!player.Equals(client)) // Игнорируем отправителя
                        {
                            await SendMessageAsync($"DIRECTION|{command}", player.GetStream());
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Unknown command: {receivedMessage}");
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
    public List<TcpClient> Players { get; set; }

    public GameSession()
    {
        Players = new List<TcpClient>();
    }
}
