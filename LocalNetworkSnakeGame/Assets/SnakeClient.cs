using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Класс клиента игры в змейку, отвечающий за подключение к серверу и обработку команд.
/// </summary>
public class SnakeClient : MonoBehaviour
{
    private Socket clientSocket; // Сокет клиента для подключения к серверу.
    private NetworkStream networkStream; // Поток для работы с сокетом.
    private readonly string serverIp = "192.168.1.77"; // IP-адрес сервера.
    private readonly int serverPort = 7777; // Порт сервера.

    public GameLobby gameLobby; // Ссылка на объект GameLobby для управления игрой.

    private bool isListening = true; // Флаг, указывающий на прослушивание команд.

    void Start()
    {
        /*
        // Создание сокета и подключение к серверу.
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        clientSocket.Connect(serverIp, serverPort);
        networkStream = new NetworkStream(clientSocket);
        */
        // Подключаемся к серверу.
        // ConnectToServerAsync();
    }

    /// <summary>
    /// Асинхронный метод для подключения к серверу.
    /// </summary>
    public async void ConnectToServerAsync()
    {
        try
        {
            isListening = true; // Установка флага прослушивания.
            // Создание сокета.
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // Подключение к серверу.
            clientSocket.Connect(serverIp, serverPort);
            // Создание потока для работы с сокетом.
            networkStream = new NetworkStream(clientSocket);

            // Отправка приветственного сообщения на сервер.
            string message = "Hello, server!";
            await SendMessageAsync(message);

            // Начать прослушивание команд от сервера.
            StartListeningForCommands();
        }
        catch (Exception ex)
        {
            // Сообщение об ошибке подключения.
            Debug.LogError($"Error in ConnectToServer: {ex.Message}");
        }
    }

    /// <summary>
    /// Асинхронный метод для прослушивания команд от сервера.
    /// </summary>
    private async void StartListeningForCommands()
    {
        // Буфер для получения сообщений.
        byte[] buffer = new byte[1024];
        StringBuilder commandBuffer = new StringBuilder(); // Буфер для накопления команд.

        while (isListening) // Пока флаг прослушивания установлен.
        {
            try
            {
                // Чтение данных от сервера.
                int receivedBytes = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                // Перекодировка байтов в строку.
                string receivedMessage = Encoding.ASCII.GetString(buffer, 0, receivedBytes);
                commandBuffer.Append(receivedMessage); // Добавление полученного сообщения в буфер команд.

                // Разделение команд по символу новой строки.
                string[] commands = commandBuffer.ToString().Split('\n');

                // Очистка буфера, чтобы оставить только непосланные команды.
                commandBuffer.Clear();
                foreach (var command in commands)
                {
                    if (!string.IsNullOrEmpty(command)) // Если команда не пустая.
                    {
                        // Обработка команды.
                        ProcessCommand(command);
                    }
                }

                // Если последняя команда не завершена, сохраняем ее в буфер для следующей итерации.
                if (!commands[commands.Length - 1].EndsWith("\n"))
                {
                    commandBuffer.Append(commands[commands.Length - 1]);
                }
            }
            catch (Exception ex)
            {
                // Сообщение об ошибке при прослушивании команд.
                Debug.LogError($"Error in listening for commands: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Обработка полученной команды.
    /// </summary>
    /// <param name="command">Команда от сервера.</param>
    private void ProcessCommand(string command)
    {
        // Разделение команды на части.
        string[] parts = command.Split("|");
        
        // Проверка корректности формата команды.
        if (parts.Length < 2)
        {
            Debug.LogWarning($"Invalid command format: {command}");
            return; // Завершение метода, если формат некорректен.
        }

        string commandType = parts[0]; // Тип команды.
        string commandData = parts[1]; // Данные команды.

        // Обработка различных типов команд.
        switch (commandType)
        {
            case "YOUR_ID": // Команда на получение ID клиента.
                if (int.TryParse(commandData, out int clientId)) // Попытка преобразовать данные в числовой ID.
                {
                    gameLobby.SetClientID(clientId); // Установка ID клиента в игровом лобби.
                    Debug.Log($"Assigned ID from server: {commandData}"); // Сообщение о получении ID.
                }
                else
                {
                    Debug.LogError($"Invalid ID format: {commandData}"); // Сообщение об ошибке формата ID.
                }
                break;

            case "GAMESTATE": // Команда на изменение состояния игры.
                if (commandData == "READY") // Игра готова к началу.
                {
                    gameLobby.EnableStartButton(); // Включение кнопки старта.
                }
                else if (commandData == "START") // Начало игры.
                {
                    gameLobby.StartGame(); // Запуск игры.
                }
                else if (commandData == "RESTART") // Перезапуск игры.
                {
                    gameLobby.RestartGame(); // Перезапуск игры.
                }
                else if (commandData == "GAME_OVER") // Игра окончена.
                {
                    gameLobby.FinishGame(); // Завершение игры.
                }
                else if (commandData == "PLAYER_DISCONNECTED") // Игрок отключился.
                {
                    DoDisconnect(); // Отключение текущего игрока.
                    gameLobby.StopSession(); // Остановка сессии в игровом лобби.
                }
                break;

            case "DIRECTION": // Команда на направление.
                gameLobby.SetOpponentDirection(commandData); // Установка направления противника в игровом лобби.
                Debug.Log($"DIRECTION from server: {commandData}"); // Сообщение о получении направления.
                break;

            case "FOOD_POSITION": // Команда на положение еды.
                gameLobby.SetFoodPosition(commandData); // Установка положения еды в игровом лобби.
                break;

            default: // Неизвестный тип команды.
                Debug.LogWarning($"Unknown command type: {commandType}"); // Сообщение об ошибке неизвестного типа команды.
                break;
        }
    }

    /// <summary>
    /// Асинхронная отправка команды на сервер.
    /// </summary>
    /// <param name="commandType">Тип команды.</param>
    /// <param name="command">Данные команды.</param>
    public async void SendCommandAsync(string commandType, string command)
    {
        try
        {
            // Формирование сообщения команды.
            string commandMessage = $"{commandType}|{command}";
            await SendMessageAsync(commandMessage); // Отправка сообщения команды на сервер.
        }
        catch (Exception ex)
        {
            // Сообщение об ошибке отправки команды.
            Debug.LogError($"Error sending command: {ex.Message}");
        }
    }

    /// <summary>
    /// Асинхронная отправка сообщения на сервер.
    /// </summary>
    /// <param name="message">Сообщение для отправки.</param>
    private async Task SendMessageAsync(string message)
    {
        try
        {
            // Кодирование сообщения в байты.
            byte[] messageData = Encoding.ASCII.GetBytes(message);
            await networkStream.WriteAsync(messageData, 0, messageData.Length); // Отправка сообщения.
            Debug.Log($"Sent: {message}"); // Сообщение об отправленном сообщении.
        }
        catch (Exception ex)
        {
            // Сообщение об ошибке отправки сообщения.
            Debug.LogError($"Error sending message: {ex.Message}");
        }
    }

    /// <summary>
    /// Отключение клиента от сервера.
    /// </summary>
    private void DoDisconnect()
    {
        isListening = false; // Остановка прослушивания команд.
        networkStream.Close(); // Закрытие потока.
        clientSocket.Close(); // Закрытие сокета клиента.
    }

    /// <summary>
    /// Метод, вызываемый при выходе приложения.
    /// </summary>
    void OnApplicationQuit()
    {
        // Отправка команды отключения перед выходом.
        SendCommandAsync("DISCONNECT", "");

        // Отключение клиента.
        DoDisconnect();
    }
}
