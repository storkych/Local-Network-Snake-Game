using System.Collections.Generic; // Подключение пространства имен для работы с коллекциями.
using UnityEngine; // Подключение пространства имен для работы с Unity.

/// <summary>
/// Класс для управления движением змейки.
/// </summary>
public class SnakeMovement : MonoBehaviour
{
    public float moveSpeed = 0.2f; // Скорость движения змейки (время между движениями).
    public Vector2 direction = Vector2.right; // Начальное направление движения змейки.
    public Transform segmentPrefab; // Префаб сегмента тела змейки.
    public float gridSize = 0.5f; // Размер сетки для движения.

    private List<Transform> segments = new List<Transform>(); // Список сегментов тела змейки.
    private float moveTimer = 0f; // Таймер для контроля времени между движениями.
    private bool gameStarted = false; // Флаг, указывающий, началась ли игра.
    public int playerId = 0; // ID змейки (0 или 1).

    public bool isPlayer = false; // Флаг, указывающий, является ли данная змейка игроком.

    public GameLobby gameLobby; // Ссылка на объект GameLobby для взаимодействия с игрой.

    void Start()
    {
        // Добавление головы змейки в список сегментов.
        segments.Add(this.transform);
    }

    void Update()
    {
        // Если игра не началась, выход из метода.
        if (!gameStarted) return;

        // Обработка ввода игрока для изменения направления движения.
        if (isPlayer)
        {
            if (Input.GetKeyDown(KeyCode.W) && direction != Vector2.down)
            {
                direction = Vector2.up; // Изменение направления вверх.
                gameLobby.SendDirection("MOVE_UP"); // Отправка нового направления на сервер.
            }
            if (Input.GetKeyDown(KeyCode.S) && direction != Vector2.up)
            {
                direction = Vector2.down; // Изменение направления вниз.
                gameLobby.SendDirection("MOVE_DOWN"); // Отправка нового направления на сервер.
            }
            if (Input.GetKeyDown(KeyCode.A) && direction != Vector2.right)
            {
                direction = Vector2.left; // Изменение направления влево.
                gameLobby.SendDirection("MOVE_LEFT"); // Отправка нового направления на сервер.
            }
            if (Input.GetKeyDown(KeyCode.D) && direction != Vector2.left)
            {
                direction = Vector2.right; // Изменение направления вправо.
                gameLobby.SendDirection("MOVE_RIGHT"); // Отправка нового направления на сервер.
            }
        }
    }

    private void FixedUpdate()
    {
        // Если игра не началась, выход из метода.
        if (!gameStarted) return;

        // Увеличение таймера.
        moveTimer += Time.fixedDeltaTime;

        if (moveTimer >= moveSpeed) // Проверка, достиг ли таймер времени движения.
        {
            // Передвижение сегментов тела змеи.
            for (int i = segments.Count - 1; i > 0; i--)
            {
                segments[i].position = segments[i - 1].position; // Перемещение сегмента на позицию предыдущего.
            }

            // Передвижение головы змейки.
            transform.position = new Vector3(
                Mathf.Round(transform.position.x / gridSize) * gridSize + direction.x * gridSize,
                Mathf.Round(transform.position.y / gridSize) * gridSize + direction.y * gridSize,
                0.0f
            );

            // Сброс таймера движения.
            moveTimer = 0f;
        }
    }

    /// <summary>
    /// Метод для увеличения длины змейки.
    /// </summary>
    public void Grow()
    {
        // Создание нового сегмента и добавление его в список.
        Transform segment = Instantiate(segmentPrefab);
        segment.position = segments[segments.Count - 1].position; // Позиция нового сегмента - позиция последнего сегмента.
        segments.Add(segment); // Добавление сегмента в список.
    }

    /// <summary>
    /// Сброс состояния змейки.
    /// </summary>
    public void ResetState()
    {
        // Уничтожение всех сегментов кроме головы.
        for (int i = 1; i < segments.Count; i++)
        {
            Destroy(segments[i].gameObject);
        }
        segments.Clear(); // Очистка списка сегментов.
        segments.Add(this.transform); // Добавление головы змейки обратно в список.

        // Возврат змейки на начальную позицию в зависимости от ID игрока.
        if (playerId == 0)
        {
            transform.position = new Vector3(-5, 1, 0); // Позиция для первой змейки.
        }
        else if (playerId == 1)
        {
            transform.position = new Vector3(-5, -1, 0); // Позиция для второй змейки.
        }
        direction = Vector2.right; // Установка начального направления.
    }

    /// <summary>
    /// Запуск игры.
    /// </summary>
    public void StartGame()
    {
        ResetState(); // Сброс состояния перед началом.
        gameStarted = true; // Установка флага, что игра началась.
    }

    /// <summary>
    /// Завершение игры.
    /// </summary>
    public void FinishGame()
    {
        gameStarted = false; // Установка флага, что игра завершена.
    }

    /// <summary>
    /// Метод для изменения направления движения змейки (принимается с сервера).
    /// </summary>
    /// <param name="newDirection">Новое направление движения.</param>
    public void ChangeDirection(Vector2 newDirection)
    {
        if (gameStarted && direction != -newDirection) // Проверка, что игра началась и не происходит обратное направление.
        {
            direction = newDirection; // Изменение направления.
        }
    }

    /// <summary>
    /// Метод, вызываемый при столкновении с другими объектами.
    /// </summary>
    /// <param name="other">Другой объект, с которым произошло столкновение.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Food")
        {
            Grow(); // Увеличение длины змейки при столкновении с едой.
        }
        else if (other.tag == "Obstacle" || other.tag == "Player")
        {
            gameLobby.GameOverCall(); // Вызов окончания игры при столкновении с препятствием или другой змейкой.
        }
    }
}
