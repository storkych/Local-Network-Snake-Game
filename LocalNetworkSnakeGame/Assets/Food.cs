using UnityEngine;

public class Food : MonoBehaviour
{
    public BoxCollider2D gridArea; // Игровое поле
    public GameLobby gameLobby;

    private Vector3 RandomizePosition()
    {
        Bounds bounds = gridArea.bounds;

        // Генерируем случайную позицию внутри игрового поля
        float x = Mathf.Round(Random.Range(bounds.min.x, bounds.max.x));
        float y = Mathf.Round(Random.Range(bounds.min.y, bounds.max.y));

        return new Vector3(x, y, 0.0f);
    }

    public void SetPosition(float x, float y)
    {
        transform.position = new Vector3(x, y, 0.0f);
    }

    public void SendPosToServer()
    {
        Vector3 pos = RandomizePosition();
        gameLobby.SendFoodPosition(pos.x, pos.y);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            SendPosToServer();
        }
    }
}
