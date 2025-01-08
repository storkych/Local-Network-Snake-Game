using System.Collections.Generic;
using UnityEngine;

public class SnakeMovement : MonoBehaviour
{
    public float moveSpeed = 0.2f; // �������� ����� ������������� (������ = �������)
    public Vector2 direction = Vector2.right; // ��������� ����������� ��������
    public Transform segmentPrefab; // ������ �������� ���� ������
    public float gridSize = 0.5f;

    private List<Transform> segments = new List<Transform>(); // ������ ��������� ���� ������
    private float moveTimer = 0f; // ������ ��� �������� �����������
    private bool gameStarted = false; // ���� ������ ����
    public int playerId = 0; // ID ������ (0 ��� 1)

    public bool isPlayer = false;

    public GameLobby gameLobby;

    void Start()
    {
        // ��������� ������ ������ ��� ������ �������
        segments.Add(this.transform);
    }

    void Update()
    {
        // ���� ���� �� ��������, ������ �� ���������
        if (!gameStarted) return;

        // ���������� ������� � ������� ��������� ������ ������ (��� ������� ������)
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
        // ���� ���� �� ��������, ������ �� ���������
        if (!gameStarted) return;

        // ��������� ������
        moveTimer += Time.fixedDeltaTime;

        if (moveTimer >= moveSpeed)
        {
            // ���������� ���� ������
            for (int i = segments.Count - 1; i > 0; i--)
            {
                segments[i].position = segments[i - 1].position;
            }

            // ���������� ������
            transform.position = new Vector3(
            Mathf.Round(transform.position.x / gridSize) * gridSize + direction.x * gridSize,
            Mathf.Round(transform.position.y / gridSize) * gridSize + direction.y * gridSize,
                0.0f
            );

            // ���������� ������
            moveTimer = 0f;
        }
    }

    public void Grow()
    {
        // ��������� ����� ������� � ������
        Transform segment = Instantiate(segmentPrefab);
        segment.position = segments[segments.Count - 1].position;
        segments.Add(segment);
    }

    public void ResetState()
    {
        // ������� ��� �������� ����� ������
        for (int i = 1; i < segments.Count; i++)
        {
            Destroy(segments[i].gameObject);
        }
        segments.Clear();
        segments.Add(this.transform);

        // ���������� ��������� ������� � �����������
        transform.position = Vector3.zero;
        direction = Vector2.right;
    }

    public void StartGame()
    {
        //ResetState();
        gameStarted = true;
        // ������������� ��������� ������� � ����������� ��� ������ ������
        if (playerId == 0)
        {
            transform.position = new Vector3(-5, 1, 0); // ������ ��� ������� ������
        }
        else if (playerId == 1)
        {
            transform.position = new Vector3(-5, -1, 0); // ������ ��� ������� ������
        }
    }

    public void FinishGame()
    {
        gameStarted = false;
    }

    // ����� ��� ��������� ����������� ������ (����� ���������� ��������)
    public void ChangeDirection(Vector2 newDirection)
    {
        if (gameStarted && direction != -newDirection) // ��������, ����� ������ ���� ��������� � ��������������� �������
        {
            direction = newDirection;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Food")
        {
            Grow(); // ������ ��� �������� ���
        }
        else if (other.tag == "Obstacle")
        {
            gameLobby.GameOverCall();
        }
    }
}
