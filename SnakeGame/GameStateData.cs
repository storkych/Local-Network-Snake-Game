using System;
using System.Collections.Generic;

namespace SnakeGame
{
    /// <summary>
    /// Класс, представляющий данные о текущем состоянии игры.
    /// </summary>
    [Serializable]
    public class GameStateData
    {
        public bool IsSavedGame = false;

        // Свойство для получения и изменения пикселя еды.
        public Pixel Food { get; set; }

        // Свойство для получения и изменения количества набранных очков.
        public int Score { get; set; }

        // Свойство для получения и изменения имени игрока.
        public string PlayerName { get; set; }

        // Свойство для получения и изменения пикселя головы.
        public Pixel Head { get; set; }

        // Свойство для получения и изменения пикселей тела.
        public Queue<Pixel> Body { get; set; }

        // Свойство для получения и изменения направления движения змейки.
        public Direction SnakeDir { get; set; }

    }
}
