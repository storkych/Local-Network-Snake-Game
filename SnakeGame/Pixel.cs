using Newtonsoft.Json;
using System;
using static System.Console;

namespace SnakeGame
{
    /// <summary>
    /// Структура, представляющая пиксель игрового поля.
    /// </summary>
    public readonly struct Pixel
    {
        private const char PIXEL_CHAR = '█';

        /// <summary>
        /// Конструктор класса Pixel с параметрами.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        /// <param name="pixelSize"></param>
        public Pixel(int x, int y, ConsoleColor color, int pixelSize = 3)
        {
            X = x;
            Y = y;
            Color = color;
            PixelSize = pixelSize;
        }
       
        // Свойство для получения информации о координате Х.
        [JsonProperty]
        public int X { get; }

        // Свойство для получения информации о координате Y.
        [JsonProperty]
        public int Y { get; }

        // Свойство для получения информации о цвете пикселя.
        public ConsoleColor Color { get; }

        // Свойство для получения информации о размере пикселя.
        public int PixelSize { get; }

        /// <summary>
        /// Отрисовывает пиксель на экране.
        /// </summary>
        public void Draw()
        {
            ForegroundColor = Color;
            for (var x = 0; x < PixelSize; x++)
            {
                for (var y = 0; y < PixelSize; y++)
                {
                    SetCursorPosition(X * PixelSize + x, Y * PixelSize + y);
                    Write(PIXEL_CHAR);
                }
            }
        }

        /// <summary>
        /// Убирает пиксель с экрана.
        /// </summary>
        public void Clear()
        {
            for (var x = 0; x < PixelSize; x++)
            {
                for (var y = 0; y < PixelSize; y++)
                {
                    SetCursorPosition(X * PixelSize + x, Y * PixelSize + y);
                    Write(' ');
                }
            }
        }
    }
}
