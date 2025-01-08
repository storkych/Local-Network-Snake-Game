using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameLobby : MonoBehaviour
{
    private int playerId = 0;

    public GameObject mainMenu;
    public GameObject lobbyMenu;
    public TMP_Text lobbyIdText;
    public GameObject finishScreen;

    public GameObject startButton;
    public GameObject waitingForHost;

    public SnakeClient snakeClient;

    public List<SnakeMovement> snakeMovementList;
    public Food food;

    void Start()
    {
        Screen.SetResolution(800, 450, false); 
    }

    public void GameOverCall()
    {
        snakeClient.SendCommandAsync("GAMESTATE", "GAME_OVER");
    }

    public void FinishGame()
    {
        foreach (var snake in snakeMovementList)
        {
            snake.FinishGame();
            finishScreen.SetActive(true);
        }
    }

    public void RestartGame()
    {
        foreach (var snake in snakeMovementList)
        {
            snake.StartGame();
            finishScreen.SetActive(false);
        }
    }

    public void StartGame()
    {
        lobbyMenu.SetActive(false);
        RestartGame();
    }

    public void SetClientID(int id)
    {
        playerId = id;
        lobbyIdText.text = playerId.ToString();
        mainMenu.SetActive(false);
        lobbyMenu.SetActive(true);
        snakeMovementList[playerId].isPlayer = true;
        if (id == 0) { snakeMovementList[0].gameObject.SetActive(true); }
    }

    public void EnableStartButton()
    {
        if (playerId == 0)
        {
            startButton.SetActive(true);
        }
        if (playerId == 1)
        {
            waitingForHost.SetActive(true);
        }
        foreach (var snake in snakeMovementList)
        {
            snake.gameObject.SetActive(true);
        }
    }

    public void PressedStartGame()
    {
        snakeClient.SendCommandAsync("GAMESTATE", "PRESS_START");
    }

    public void PressedRestartGame()
    {
        snakeClient.SendCommandAsync("GAMESTATE", "PRESS_RESTART");
    }

    public void SendDirection(string direction)
    {
        snakeClient.SendCommandAsync("DIRECTION", direction);
    }

    public void SendFoodPosition(float x, float y)
    {
        snakeClient.SendCommandAsync("FOOD_POSITION", x + "_" + y);
    }

    public void SetFoodPosition(string data)
    {
        var parts = data.Split('_'); // разделяем по символу '_'

        // Присваиваем значения переменным
        var x = parts[0];
        var y = parts[1];

        // Преобразуем x и y в нужные типы 
        float xPos = int.Parse(x);
        float yPos = int.Parse(y);

        food.SetPosition(xPos, yPos);
    }

    public void SetOpponentDirection(string direction)
    {
        Vector2 newDirection = Vector2.zero;
        int opponentId = (playerId == 0) ? 1 : 0;

        if (direction == "MOVE_UP")
        {
            newDirection = Vector2.up;
        }
        else if (direction == "MOVE_DOWN")
        {
            newDirection = Vector2.down;
        }
        else if (direction == "MOVE_LEFT")
        {
            newDirection = Vector2.left;
        }
        else if (direction == "MOVE_RIGHT")
        {
            newDirection = Vector2.right;
        }

        snakeMovementList[opponentId].ChangeDirection(newDirection);
    }
}
