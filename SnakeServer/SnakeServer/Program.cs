using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Основной класс сервера, отвечающий за управление игровыми сессиями и подключениями игроков.
/// </summary>
class Server
{
    private static Socket serverSocket; // Сокет сервера для обработки входящих подключений.
    private static readonly int port = 7777; // Порт для подключения игроков.
    private static readonly string ipAddress = "192.168.1.77"; // IP-адрес сервера.
    private static readonly int maxPlayersPerSession = 2; // Максимальное количество игроков в одной сессии.
    private static readonly List<GameSession> gameSessions = new List<GameSession>(); // Список активных игровых сессий.

    /// <summary>
    /// Главный метод, запускающий сервер и начальное подключение клиентов.
    /// </summary>
    static async Task Main(string[] args)
    {
        await SetupServer(); // Настройка сервера.
        await StartAcceptingClients(); // Начало приема клиентов.
    }

    /// <summary>
    /// Настройка сервера: создание сокета и привязка его к IP и порту.
    /// </summary>
    private static async Task SetupServer()
    {
        // Создание сокета.
        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        // Привязка сокета.
        serverSocket.Bind(new IPEndPoint(IPAddress.Parse(ipAddress), port));
        
        // Максимум 10 ожиданий подключения.
        serverSocket.Listen(10); 
        
        // Сообщение о запуске сервера.
        Console.WriteLine("Server started...");
    }

    /// <summary>
    /// Начинает процесс ожидания клиентов и обработки их подключений.
    /// </summary>
    private static async Task StartAcceptingClients()
    {
        while (true)
        {
            // Ожидание подключения клиента.
            Socket clientSocket = await Task.Factory.FromAsync<Socket>(serverSocket.BeginAccept, serverSocket.EndAccept, null);
            // Сообщение о подключении игрока.
            Console.WriteLine("Player connected.");
            // Создание потока для работы с сокетом.
            NetworkStream networkStream = new NetworkStream(clientSocket);
            // Буфер для получения сообщений.
            byte[] buffer = new byte[1024]; 
            // Чтение сообщения от клиента.
            int receivedBytes = await networkStream.ReadAsync(buffer, 0, buffer.Length);
            // Перекодировка байтов в строку.
            string receivedMessage = Encoding.ASCII.GetString(buffer, 0, receivedBytes);
            // Сообщение о полученном сообщении.
            Console.WriteLine($"Received: {receivedMessage}");

            bool sessionFound = false; // Флаг обнаружения сессии.
            GameSession sessionToAssign = null; // Сессия, к которой будет присоединен игрок.
            int playerId = -1;
            int sessionId = -1;

            // Попытка найти сессию, которая не полна.
            foreach (var session in gameSessions)
            {
                if (session.Players.Count < maxPlayersPerSession) // Если в сессии есть место.
                {
                    // Добавление клиента в сессию.
                    session.Players.Add(clientSocket);
                    // Присвоение сессии.
                    sessionToAssign = session;
                    // Установка флага о том, что сессия найдена.
                    sessionFound = true;
                    // Получение ID сессии.
                    sessionId = gameSessions.IndexOf(session);
                    // Сообщение о подключении к сессии.
                    Console.WriteLine($"Session {sessionId} found, Player connected.");

                    // Получение ID игрока.
                    playerId = session.Players.Count - 1;
                    // Отправка игроку его ID.
                    await SendMessageAsync($"YOUR_ID|{playerId}", networkStream);

                    // Если сессия полна, начинаем игру.
                    if (session.Players.Count == maxPlayersPerSession) // Проверка на полноту сессии.
                    {
                        // Сообщение о начале сессии.
                        Console.WriteLine($"Session {sessionId} started, both players connected.");
                        
                        // Запуск игровой сессии.
                        StartGameSession(session);

                        // Уведомление всех игроков о начале игры.
                        foreach (var player in session.Players)
                        {
                            await SendMessageAsync("GAMESTATE|READY", new NetworkStream(player));
                        }
                    }
                    break; // Выход из цикла поиска.
                }
            }

            // Если сессии нет или все сессии полны, создаем новую сессию.
            if (!sessionFound)
            {
                // Создание новой сессии.
                var newSession = new GameSession();
                // Добавление клиента в новую сессию.
                newSession.Players.Add(clientSocket);
                // Добавление сессии в список.
                gameSessions.Add(newSession);
                // Присвоение новой сессии.
                sessionToAssign = newSession;
                // Получение ID новой сессии.
                sessionId = gameSessions.Count - 1;
                // Сообщение о создании новой сессии.
                Console.WriteLine($"New session {sessionId} created, player connected.");

                // ID первого игрока в новой сессии.
                playerId = 0;
                // Отправка игроку его ID.
                await SendMessageAsync($"YOUR_ID|{playerId}", networkStream);

                // Если сессия сразу полна, начинаем игру.
                if (newSession.Players.Count == maxPlayersPerSession) // Проверка на полноту новой сессии.
                {
                    // Сообщение о начале сессии.
                    Console.WriteLine($"Session {sessionId} started, both players connected.");
                    
                    // Запуск игровой сессии.
                    StartGameSession(newSession);

                    // Уведомление всех игроков о начале игры.
                    foreach (var player in newSession.Players)
                    {
                        await SendMessageAsync("GAMESTATE|READY", new NetworkStream(player));
                    }
                }
            }

            // Запуск задачи для прослушивания команд.
            _ = Task.Run(() => ListenForCommands(sessionToAssign, clientSocket, playerId, sessionId));
        }
    }

    /// <summary>
    /// Прослушивает команды от подключенного игрока.
    /// </summary>
    /// <param name="session">Текущая игровая сессия.</param>
    /// <param name="clientSocket">Сокет подключенного клиента.</param>
    /// <param name="playerId">ID игрока в сессии.</param>
    /// <param name="sessionId">ID игровой сессии.</param>
    private static async Task ListenForCommands(GameSession session, Socket clientSocket, int playerId, int sessionId)
    {
        // Создание потока для работы с сокетом.
        NetworkStream networkStream = new NetworkStream(clientSocket);
        // Буфер для получения сообщений.
        byte[] buffer = new byte[1024];

        while (true)
        {
            // Чтение команды от игрока.
            int receivedBytes = await networkStream.ReadAsync(buffer, 0, buffer.Length); 

            if (receivedBytes == 0) // Если получено 0 байт, клиент отключился.
            {
                // Сообщение об отключении игрока.
                Console.WriteLine($"[Session {sessionId}] Player {playerId} disconnected abruptly (connection closed).");
                break; // Выход из цикла.
            }

            // Перекодировка байтов в строку.
            string receivedMessage = Encoding.ASCII.GetString(buffer, 0, receivedBytes);
            // Сообщение о полученной команде.
            Console.WriteLine($"[Session {sessionId}] Player {playerId} - Received command: {receivedMessage}");

            // Разделение команды на части.
            string[] messageParts = receivedMessage.Split("|");
            if (messageParts.Length == 2) // Проверка на наличие двух частей в команде.
            {
                string commandType = messageParts[0]; // Тип команды.
                string command = messageParts[1]; // Содержимое команды.

                if (commandType == "GAMESTATE") // Если команда связана с состоянием игры.
                {
                    if (command == "READY") // Игрок готов.
                    {
                        // Сообщение о готовности игрока.
                        Console.WriteLine($"[Session {sessionId}] Player {playerId} is ready.");
                    }
                    else if (command == "PRESS_START") // Начать игру.
                    {
                        // Уведомление всех игроков о начале игры.
                        foreach (var player in session.Players)
                        {
                            await SendMessageAsync("GAMESTATE|START", new NetworkStream(player));
                        }
                    }
                    else if (command == "PRESS_RESTART")
                    {
                        // Уведомление всех игроков о перезапуске.
                        foreach (var player in session.Players)
                        {
                            await SendMessageAsync("GAMESTATE|RESTART", new NetworkStream(player));
                        }
                    }
                    else if (command == "GAME_OVER") // Игра окончена.
                    {
                        // Уведомление всех игроков о завершении игры.
                        foreach (var player in session.Players)
                        {
                            await SendMessageAsync("GAMESTATE|GAME_OVER", new NetworkStream(player));
                        }
                    }
                }
                else if (commandType == "DIRECTION") // Если команда связана с направлением.
                {
                    // Отправка команды направления другим игрокам.
                    foreach (var player in session.Players)
                    {
                        if (!player.Equals(clientSocket)) // Игнорируем отправителя.
                        {
                            await SendMessageAsync($"DIRECTION|{command}", new NetworkStream(player));
                        }
                    }
                }
                else if (commandType == "FOOD_POSITION") // Если команда связана с положением еды.
                {
                    // Отправка позиции еды всем игрокам.
                    foreach (var player in session.Players)
                    {
                        await SendMessageAsync($"FOOD_POSITION|{command}", new NetworkStream(player));
                    }
                }
                else if (commandType == "DISCONNECT") // Если команда на отключение.
                {
                    // Сообщение об отключении игрока.
                    Console.WriteLine($"[Session {sessionId}] Player {playerId} disconnected.");
                    // Удаление игрока из сессии.
                    session.Players.Remove(clientSocket);

                    // Уведомление оставшихся игроков.
                    foreach (var player in session.Players)
                    {
                        await SendMessageAsync("GAMESTATE|PLAYER_DISCONNECTED", new NetworkStream(player));
                    }

                    // Закрытие сокета клиента.
                    clientSocket.Close();

                    // Если остался только один игрок, завершаем сессию.
                    if (session.Players.Count < 2)
                    {
                        // Сообщение о завершении игры.
                        Console.WriteLine($"[Session {sessionId}] Player left. Ending the game.");
                        foreach (var player in session.Players)
                        {
                            player.Close(); // Закрытие сокетов оставшихся игроков.
                        }
                        // Удаление сессии из списка.
                        gameSessions.Remove(session); 
                    }
                    break; // Выход из цикла, так как игрок отключился.
                }
                else // Если команда неизвестна.
                {
                    // Сообщение о неизвестной команде.
                    Console.WriteLine($"[Session {sessionId}] Unknown command: {receivedMessage}");
                }
            }
        }
    }

    /// <summary>
    /// Запуск логики старта игровой сессии.
    /// </summary>
    /// <param name="session">Игровая сессия для запуска.</param>
    private static void StartGameSession(GameSession session)
    {
    }

    /// <summary>
    /// Отправка сообщения клиенту.
    /// </summary>
    /// <param name="message">Сообщение для отправки.</param>
    /// <param name="networkStream">Поток для отправки сообщения.</param>
    private static async Task SendMessageAsync(string message, NetworkStream networkStream)
    {
        try
        {
            // Добавление разделителя \n.
            string messageWithSeparator = message + "\n"; 
            // Кодирование сообщения в байты.
            byte[] messageBytes = Encoding.ASCII.GetBytes(messageWithSeparator); 
            // Отправка сообщения.
            await networkStream.WriteAsync(messageBytes, 0, messageBytes.Length); 
            
            // Сообщение об отправленном сообщении.
            Console.WriteLine($"Sent: {message}"); 
        }
        catch (Exception ex) // Обработка исключений при отправке.
        {
            // Сообщение об ошибке.
            Console.WriteLine($"Error sending message: {ex.Message}"); 
        }
    }
}

/// <summary>
/// Класс, представляющий игровую сессию.
/// </summary>
public class GameSession
{
    public List<Socket> Players { get; set; } // Список игроков в сессии.

    /// <summary>
    /// Конструктор класса GameSession, инициализирующий список игроков.
    /// </summary>
    public GameSession()
    {
        // Создание списка игроков.
        Players = new List<Socket>(); 
    }
}
