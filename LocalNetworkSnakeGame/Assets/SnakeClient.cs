using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class SnakeClient : MonoBehaviour
{
    private Socket clientSocket;
    private NetworkStream networkStream;
    private readonly string serverIp = "192.168.1.77";
    private readonly int serverPort = 7777;

    public GameLobby gameLobby;

    private bool isListening = true;

    void Start()
    {
        /*
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        clientSocket.Connect(serverIp, serverPort);
        networkStream = new NetworkStream(clientSocket);
        */
        // Отправляем сообщение на сервер
        //ConnectToServerAsync();
    }

    public async void ConnectToServerAsync()
    {
        try
        {
            isListening = true;
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(serverIp, serverPort);
            networkStream = new NetworkStream(clientSocket);

            // Отправить сообщение для подключения
            string message = "Hello, server!";
            await SendMessageAsync(message);

            // Начать слушать команды
            StartListeningForCommands();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in ConnectToServer: {ex.Message}");
        }
    }

    private async void StartListeningForCommands()
    {
        byte[] buffer = new byte[1024];
        StringBuilder commandBuffer = new StringBuilder();

        while (isListening)
        {
            try
            {
                int receivedBytes = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                string receivedMessage = Encoding.ASCII.GetString(buffer, 0, receivedBytes);
                commandBuffer.Append(receivedMessage);

                // Разделяем команды по символу нового строки
                string[] commands = commandBuffer.ToString().Split('\n');

                // Оставляем только полные команды
                commandBuffer.Clear();
                foreach (var command in commands)
                {
                    if (!string.IsNullOrEmpty(command))
                    {
                        // Обрабатываем команду
                        ProcessCommand(command);
                    }
                }

                // Если осталась неполная команда, сохраняем ее в buffer для следующего цикла
                if (!commands[commands.Length - 1].EndsWith("\n"))
                {
                    commandBuffer.Append(commands[commands.Length - 1]);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in listening for commands: {ex.Message}");
            }
        }
    }

    private void ProcessCommand(string command)
    {
        // Разбор сообщения
        string[] parts = command.Split("|");
        if (parts.Length < 2)
        {
            Debug.LogWarning($"Invalid command format: {command}");
            return;
        }

        string commandType = parts[0];
        string commandData = parts[1];

        switch (commandType)
        {
            case "YOUR_ID":
                if (int.TryParse(commandData, out int clientId))
                {
                    gameLobby.SetClientID(clientId);
                    Debug.Log($"Assigned ID from server: {commandData}");
                }
                else
                {
                    Debug.LogError($"Invalid ID format: {commandData}");
                }
                break;

            case "GAMESTATE":
                if (commandData == "READY")
                {
                    gameLobby.EnableStartButton();
                }
                else if (commandData == "START")
                {
                    gameLobby.StartGame();
                }
                else if (commandData == "RESTART")
                {
                    gameLobby.RestartGame();
                }
                else if (commandData == "GAME_OVER")
                {
                    gameLobby.FinishGame();
                }
                else if (commandData == "PLAYER_DISCONNECTED")
                {
                    DoDisconnect();
                    gameLobby.StopSession();
                }
                break;

            case "DIRECTION":
                gameLobby.SetOpponentDirection(commandData);
                Debug.Log($"DIRECTION from server: {commandData}");
                break;

            case "FOOD_POSITION":
                gameLobby.SetFoodPosition(commandData);
                break;
            default:
                Debug.LogWarning($"Unknown command type: {commandType}");
                break;
        }
    }

    public async void SendCommandAsync(string commandType, string command)
    {
        try
        {
            string commandMessage = $"{commandType}|{command}";
            await SendMessageAsync(commandMessage);  // Используем await для асинхронного вызова
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error sending command: {ex.Message}");
        }
    }

    private async Task SendMessageAsync(string message)
    {
        try
        {
            byte[] messageData = Encoding.ASCII.GetBytes(message);
            await networkStream.WriteAsync(messageData, 0, messageData.Length);
            Debug.Log($"Sent: {message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error sending message: {ex.Message}");
        }
    }

    private void DoDisconnect()
    {
        isListening = false;
        networkStream.Close();
        clientSocket.Close();
    }

    void OnApplicationQuit()
    {
        // Отправляем сообщение серверу, что клиент отключается
        SendCommandAsync("DISCONNECT", "");

        DoDisconnect();
    }
}
