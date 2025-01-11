using System.Collections.Generic; // Подключение пространства имен для работы с коллекциями.
using TMPro; // Подключение пространства имен для использования текстового компонента TMP.
using UnityEngine; // Подключение пространства имен для работы с Unity.
using UnityEngine.UI; // Подключение пространства имен для работы с UI-элементами.


/// <summary>
/// Класс, представляющий игровое лобби.
/// </summary>
public class GameLobby : MonoBehaviour
{
    private int playerId = 0; // ID игрока.

    public GameObject mainMenu; // Основное меню.
    public GameObject lobbyMenu; // Меню лобби игрока.
    public TMP_Text lobbyIdText; // Текстовый элемент для отображения ID игрока.
    public GameObject finishScreen; // Экран завершения игры.
    public GameObject sessionClosedMenu; // Меню закрытой сессии.

    public GameObject startButton; // Кнопка для начала игры.
    public GameObject waitingForHost; // Сообщение о ожидании хоста.

    public SnakeClient snakeClient; // Ссылка на объект SnakeClient для общения с сервером.

    public List<SnakeMovement> snakeMovementList; // Список объектов змей.
    public Food food; // Ссылка на объект еды.

    void Start()
    {
        // Установка разрешения экрана при запуске.
        Screen.SetResolution(800, 450, false); 
    }

    /// <summary>
    /// Метод, вызываемый при завершении игры.
    /// </summary>
    public void GameOverCall()
    {
        // Отправка команды об окончании игры на сервер.
        snakeClient.SendCommandAsync("GAMESTATE", "GAME_OVER");
    }

    /// <summary>
    /// Завершение игры и отображение экрана завершения.
    /// </summary>
    public void FinishGame()
    {
        foreach (var snake in snakeMovementList) // Для каждой змеи в списке.
        {
            snake.FinishGame(); // Завершение игры для змеи.
            finishScreen.SetActive(true); // Отображение экрана завершения.
        }
    }

    /// <summary>
    /// Перезапуск игры.
    /// </summary>
    public void RestartGame()
    {
        foreach (var snake in snakeMovementList) // Для каждой змеи в списке.
        {
            snake.StartGame(); // Запуск игры для змеи.
            finishScreen.SetActive(false); // Скрытие экрана завершения.
        }
    }

    /// <summary>
    /// Начало игры.
    /// </summary>
    public void StartGame()
    {
        // Скрытие меню лобби и перезапуск игры.
        lobbyMenu.SetActive(false);
        RestartGame();
    }

    /// <summary>
    /// Установка ID для клиента.
    /// </summary>
    /// <param name="id">ID игрока.</param>
    public void SetClientID(int id)
    {
        playerId = id; // Установка ID игрока.
        lobbyIdText.text = playerId.ToString(); // Отображение ID игрока.
        mainMenu.SetActive(false); // Скрытие основного меню.
        lobbyMenu.SetActive(true); // Показ меню лобби.
        snakeMovementList[playerId].isPlayer = true; // Установка текущего игрока.
        if (id == 0) 
        { 
            // Активируем объект первой змеи.
            snakeMovementList[0].gameObject.SetActive(true); 
        }
    }

    /// <summary>
    /// Включение кнопки "Старт" в зависимости от ID игрока.
    /// </summary>
    public void EnableStartButton()
    {
        if (playerId == 0)
        {
            startButton.SetActive(true); // Включаем кнопку старта для хоста.
        }
        if (playerId == 1)
        {
            waitingForHost.SetActive(true); // Показываем сообщение "Ожидание хоста" для второго игрока.
            food.SendPosToServer();
        }
        foreach (var snake in snakeMovementList)
        {
            snake.gameObject.SetActive(true); // Активируем всех змей.
        }
    }

    /// <summary>
    /// Метод, вызываемый при нажатии кнопки "Начать игру".
    /// </summary>
    public void PressedStartGame()
    {
        // Отправка команды на сервер о начале игры.
        snakeClient.SendCommandAsync("GAMESTATE", "PRESS_START");
    }

    /// <summary>
    /// Метод, вызываемый при нажатии кнопки "Перезапустить игру".
    /// </summary>
    public void PressedRestartGame()
    {
        // Отправка команды на сервер о перезапуске игры.
        snakeClient.SendCommandAsync("GAMESTATE", "PRESS_RESTART");
    }

    /// <summary>
    /// Отправка направления игрока на сервер.
    /// </summary>
    /// <param name="direction">Направление движения.</param>
    public void SendDirection(string direction)
    {
        // Отправка команды на сервер о направлении.
        snakeClient.SendCommandAsync("DIRECTION", direction);
    }

    /// <summary>
    /// Отправка позиции еды на сервер.
    /// </summary>
    /// <param name="x">Координата x.</param>
    /// <param name="y">Координата y.</param>
    public void SendFoodPosition(float x, float y)
    {
        // Формирование сообщения с позицией еды и отправка его на сервер.
        snakeClient.SendCommandAsync("FOOD_POSITION", x + "_" + y);
    }

    /// <summary>
    /// Установка позиции еды в игре.
    /// </summary>
    /// <param name="data">Данные о позиции еды.</param>
    public void SetFoodPosition(string data)
    {
        var parts = data.Split('_'); // Разделение данных по символу '_'.

        // Получение координат x и y.
        var x = parts[0]; 
        var y = parts[1];

        // Преобразование строки в числовые значения.
        float xPos = int.Parse(x);
        float yPos = int.Parse(y);

        // Установка позиции еды в игре.
        food.SetPosition(xPos, yPos);
    }

    /// <summary>
    /// Установка направления противника.
    /// </summary>
    /// <param name="direction">Направление движения противника.</param>
    public void SetOpponentDirection(string direction)
    {
        Vector2 newDirection = Vector2.zero; // Новое направление.
        int opponentId = (playerId == 0) ? 1 : 0; // Получение ID противника.

        // Определение нового направления.
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

        // Изменение направления движения противника.
        snakeMovementList[opponentId].ChangeDirection(newDirection);
    }

    /// <summary>
    /// Сброс состояния змей.
    /// </summary>
    public void ResetSnakes()
    {
        foreach (var snake in snakeMovementList) // Для каждой змеи в списке.
        {
            snake.ResetState(); // Сброс состояния змеи.
            snake.isPlayer = false; // Установка флага игрока в false.
            snake.gameObject.SetActive(false); // Деактивация объекта змеи.
        }
    }

    /// <summary>
    /// Остановка игры и отображение меню закрытой сессии.
    /// </summary>
    public void StopSession()
    {
        FinishGame(); // Завершение игры.

        finishScreen.SetActive(false); // Скрытие экрана завершения игры.
        mainMenu.SetActive(false); // Скрытие основного меню.
        lobbyMenu.SetActive(false); // Скрытие меню лобби.

        sessionClosedMenu.SetActive(true); // Показ меню закрытой сессии.

        startButton.SetActive(false); // Скрытие кнопки старта.
        ResetSnakes(); // Сброс состояния змей.
    }

    /// <summary>
    /// Метод, вызываемый при выходе из сессии.
    /// </summary>
    public void LeaveSession()
    {
        // Отправка команды на сервер об отключении.
        snakeClient.SendCommandAsync("DISCONNECT", "_");
        StopSession(); // Остановка сессии.
    }
}
