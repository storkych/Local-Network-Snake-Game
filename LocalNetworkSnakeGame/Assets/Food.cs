using UnityEngine;

/// <summary>
/// Класс, представляющий еду в игре.
/// </summary>
public class Food : MonoBehaviour
{
    public BoxCollider2D gridArea; // Ограничивающая область для размещения еды.
    public GameLobby gameLobby; // Ссылка на объект GameLobby для управления игрой.

    /// <summary>
    /// Генерирует случайную позицию для размещения еды внутри ограничивающей области.
    /// </summary>
    /// <returns>Случайная позиция в виде вектора.</returns>
    private Vector3 RandomizePosition()
    {
        // Получение границ ограничивающей области.
        Bounds bounds = gridArea.bounds;

        // Генерация случайного положения внутри границ ограничивающей области.
        float x = Mathf.Round(Random.Range(bounds.min.x, bounds.max.x));
        float y = Mathf.Round(Random.Range(bounds.min.y, bounds.max.y));

        return new Vector3(x, y, 0.0f); // Возврат случайной позиции.
    }

    /// <summary>
    /// Устанавливает позицию еды.
    /// </summary>
    /// <param name="x">Координата x для установки позиции.</param>
    /// <param name="y">Координата y для установки позиции.</param>
    public void SetPosition(float x, float y)
    {
        // Установка позиции объекта еды.
        transform.position = new Vector3(x, y, 0.0f);
    }

    /// <summary>
    /// Отправляет позицию еды на сервер.
    /// </summary>
    public void SendPosToServer()
    {
        // Генерация случайной позиции для еды.
        Vector3 pos = RandomizePosition();
        // Отправка позиции на сервер через GameLobby.
        gameLobby.SendFoodPosition(pos.x, pos.y);
    }

    /// <summary>
    /// Метод, вызываемый при столкновении с другим объектом.
    /// </summary>
    /// <param name="other">Другой объект, с которым произошло столкновение.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Проверка на столкновение с объектом игрока.
        if (other.tag == "Player")
        {
            // Отправка позиции еды на сервер, если столкновение произошло с игроком.
            SendPosToServer();
        }
    }
}
