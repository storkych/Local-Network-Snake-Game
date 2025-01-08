using System.Collections.Generic;
using UnityEngine;

public class SnakeMovement : MonoBehaviour
{
    public float moveSpeed = 0.2f; // Интервал между перемещениями (меньше = быстрее)
    public Vector2 direction = Vector2.right; // Начальное направление движения
    public Transform segmentPrefab; // Префаб сегмента тела змейки
    public float gridSize = 0.5f;

    private List<Transform> segments = new List<Transform>(); // Список сегментов тела змейки
    private float moveTimer = 0f; // Таймер для контроля перемещения
    private bool gameStarted = false; // Флаг начала игры
    public int playerId = 0; // ID игрока (0 или 1)

    public bool isPlayer = false;

    public GameLobby gameLobby;

    void Start()
    {
        // Добавляем голову змейки как первый сегмент
        segments.Add(this.transform);
    }

    void Update()
    {
        // Если игра не началась, змейка не двигается
        if (!gameStarted) return;

        // Управление змейкой с помощью локальных команд игрока (для первого игрока)
        if (isPlayer)
        {
            if (Input.GetKeyDown(KeyCode.W) && direction != Vector2.down)
            {
                direction = Vector2.up;
                gameLobby.SendDirection("MOVE_UP");
            }
            if (Input.GetKeyDown(KeyCode.S) && direction != Vector2.up)
            {
                direction = Vector2.down;
                gameLobby.SendDirection("MOVE_DOWN");
            }
            if (Input.GetKeyDown(KeyCode.A) && direction != Vector2.right)
            {
                direction = Vector2.left;
                gameLobby.SendDirection("MOVE_LEFT");
            }
            if (Input.GetKeyDown(KeyCode.D) && direction != Vector2.left)
            {
                direction = Vector2.right;
                gameLobby.SendDirection("MOVE_RIGHT");
            }
        }
    }

    private void FixedUpdate()
    {
        // Если игра не началась, змейка не двигается
        if (!gameStarted) return;

        // Обновляем таймер
        moveTimer += Time.fixedDeltaTime;

        if (moveTimer >= moveSpeed)
        {
            // Перемещаем тело змейки
            for (int i = segments.Count - 1; i > 0; i--)
            {
                segments[i].position = segments[i - 1].position;
            }

            // Перемещаем голову
            transform.position = new Vector3(
            Mathf.Round(transform.position.x / gridSize) * gridSize + direction.x * gridSize,
            Mathf.Round(transform.position.y / gridSize) * gridSize + direction.y * gridSize,
                0.0f
            );

            // Сбрасываем таймер
            moveTimer = 0f;
        }
    }

    public void Grow()
    {
        // Добавляем новый сегмент к змейке
        Transform segment = Instantiate(segmentPrefab);
        segment.position = segments[segments.Count - 1].position;
        segments.Add(segment);
    }

    public void ResetState()
    {
        // Удаляем все сегменты кроме головы
        for (int i = 1; i < segments.Count; i++)
        {
            Destroy(segments[i].gameObject);
        }
        segments.Clear();
        segments.Add(this.transform);

        // Сбрасываем начальную позицию и направление
        transform.position = Vector3.zero;
        direction = Vector2.right;
    }

    public void StartGame()
    {
        //ResetState();
        gameStarted = true;
        // Инициализация начальных позиций и направления для каждой змейки
        if (playerId == 0)
        {
            transform.position = new Vector3(-5, 1, 0); // Пример для первого игрока
        }
        else if (playerId == 1)
        {
            transform.position = new Vector3(-5, -1, 0); // Пример для второго игрока
        }
    }

    public void FinishGame()
    {
        gameStarted = false;
    }

    // Метод для изменения направления змейки (будет вызываться сервером)
    public void ChangeDirection(Vector2 newDirection)
    {
        if (gameStarted && direction != -newDirection) // Проверка, чтобы нельзя было двигаться в противоположную сторону
        {
            direction = newDirection;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Food")
        {
            Grow(); // Растем при съедении еды
        }
        else if (other.tag == "Obstacle")
        {
            gameLobby.GameOverCall();
        }
    }
}
