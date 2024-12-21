using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SnakeGame
{
    /// <summary>
    /// Класс, который отвечает за создание и движение змейки.
    /// </summary>
    public class Snake
    {
        private readonly ConsoleColor _headColor;
        private readonly ConsoleColor _bodyColor;
        
        /// <summary>
        /// Конструктор класса Snake без параметров.
        /// </summary>
        public Snake() {
            _headColor = ConsoleColor.DarkGray;
            _bodyColor = ConsoleColor.White;

            Head = new Pixel(5, 5, _headColor);

            for (var i = 3; i >= 0; i--)
            {
                Body.Enqueue(new Pixel(Head.X - i - 1, 5, _bodyColor));
            }
        }

        /// <summary>
        /// Конструктор класса Snake с параметрами.
        /// </summary>
        /// <param name="head"> - пиксель головы змейки.</param>
        /// <param name="body"> - пиксель тела змейки.</param>
        public Snake(Pixel head, Queue<Pixel> body)
        {
            _headColor = ConsoleColor.DarkGray;
            _bodyColor = ConsoleColor.White;
            Head = head;
            Body = body;
        }

        /// <summary>
        /// Конструктор класса Snake с параметрами.
        /// </summary>
        /// <param name="initialX"> - координата положения головы. </param>
        /// <param name="initialY"> - координата положения головы. </param>
        /// <param name="headColor"> - цвет головы змейки. </param>
        /// <param name="bodyColor"> - цвет тела змейки. </param>
        /// <param name="bodyLength"> - длина тела змейки. </param>
        public Snake(int initialX,
            int initialY,
            ConsoleColor headColor,
            ConsoleColor bodyColor,
            int bodyLength = 3)
        {
            _headColor = headColor;
            _bodyColor = bodyColor;

            Head = new Pixel(initialX, initialY, headColor);

            for (var i = bodyLength; i >= 0; i--)
            {
                Body.Enqueue(new Pixel(Head.X - i - 1, initialY, _bodyColor));
            }

            Draw();
        }
       
        // Свойство для получения и установления значений пикселя головы.
        [JsonProperty]
        public Pixel Head { get; private set; }
        
        // Свойство для получения очереди пикселей тела змейки.
        [JsonProperty]
        public Queue<Pixel> Body { get; } = new Queue<Pixel>();

        /// <summary>
        /// Перемещает змейку в заданном направлении и обновляет ее позицию.
        /// </summary>
        /// <param name="direction"> - направление змейки. </param>
        /// <param name="eat"> - съела ли змейка что-то за этот ход (true/false). </param>
        public void Move(Direction direction, bool eat = false)
        {
            Clear();

            Body.Enqueue(new Pixel(Head.X, Head.Y, _bodyColor));
            if (!eat)
                Body.Dequeue();

            if (direction == Direction.Right)
            {
                Head = new Pixel(Head.X + 1, Head.Y, _headColor);
            }
            else if (direction == Direction.Left)
            {
                Head = new Pixel(Head.X - 1, Head.Y, _headColor);
            }
            else if (direction == Direction.Up)
            {
                Head = new Pixel(Head.X, Head.Y - 1, _headColor);
            }
            else if (direction == Direction.Down)
            {
                Head = new Pixel(Head.X, Head.Y + 1, _headColor);
            }

            Draw();
        }

        /// <summary>
        /// Отрисовывает змейку на экране.
        /// </summary>
        public void Draw()
        {
            Head.Draw();

            foreach (Pixel pixel in Body)
            {
                pixel.Draw();
            }
        }

        /// <summary>
        /// Убирает змейку с экрана.
        /// </summary>
        public void Clear()
        {
            Head.Clear();

            foreach (Pixel pixel in Body)
            {
                pixel.Clear();
            }
        }
    }
}
