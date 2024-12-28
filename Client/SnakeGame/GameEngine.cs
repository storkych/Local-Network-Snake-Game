using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using static System.Console;

namespace SnakeGame
{
    internal class GameEngine
    {
        private const int MAP_WIDTH = 30;
        private const int MAP_HEIGHT = 20;
        // Ширина экрана, рассчитываемая на основании ширины карты.
        private const int SCREEN_WIDTH = MAP_WIDTH * 3;
        // Высота экрана, рассчитываемая на основании высоты карты.
        private const int SCREEN_HEIGHT = MAP_HEIGHT * 3;
        // Время обновления экрана в миллисекундах.
        private const int FRAME_MILLISECONDS = 200;
        private const ConsoleColor BORDER_COLOR = ConsoleColor.White;
        private const ConsoleColor FOOD_COLOR = ConsoleColor.Green;
        private const ConsoleColor BODY_COLOR = ConsoleColor.White;
        private const ConsoleColor HEAD_COLOR = ConsoleColor.DarkGray;
        private static readonly Random Random = new Random();
        // Текущее состояние игры.
        public GameState gameState;
        // Данные о состоянии игры.
        private GameStateData gameStateData;
        // Объект для отображения заголовка.
        private readonly TitlePrinter printer;
        // Объект для управления сохранением и загрузкой данных.
        private readonly SaveLoadManager saveLoadManager;
        // Направление противника по умолчанию.
        public Direction opponentDirection = Direction.Right;
        // Флаг готовности второго игрока.
        public bool secondPlayerReady = false;
        // Объект клиента для работы с сервером.
        private GameClient gameClient;
        // Флаг, указывающий, является ли клиент хостом.
        public bool isClientHost = true;

        /// <summary>
        /// Конструктор класса GameEngine.
        /// </summary>
        /// <param name="client">Клиент для взаимодействия с сервером.</param>
        public GameEngine(GameClient client)
        {
            gameState = GameState.MainMenu; // Инициализация состояния игры.
            gameStateData = new GameStateData();
            printer = new TitlePrinter();
            saveLoadManager = new SaveLoadManager();
            SetWindowSize(SCREEN_WIDTH, SCREEN_HEIGHT + 5);
            SetBufferSize(SCREEN_WIDTH, SCREEN_HEIGHT + 5);
            gameClient = client;
        }

        /// <summary>
        /// Основной метод логики работы игры (главное меню, меню паузы).
        /// </summary>
        public void Run()
        {
            GameStateData match = new GameStateData();
            bool exitRequested = false;
            gameStateData = saveLoadManager.LoadData();
            CursorVisible = false;

            int selectedItem = 0;

            while (!exitRequested)
            {
                switch (gameState)
                {
                    case GameState.MainMenu:
                        Clear();
                        printer.TitlePrint();
                        DisplayClientInfo();
                        DisplayMainMenu(ref selectedItem, ref exitRequested);
                        break;

                    case GameState.Lobby:
                        Clear();
                        if (gameClient.playerID == 0 && !gameClient.waitingGame)
                        {
                            ShowLobbyMenu(ref selectedItem);
                        }
                        break;

                    case GameState.InGame:
                        match = StartGame();
                        break;

                    case GameState.GameOver:
                        HandleGameOver(match);
                        break;

                }
                Clear();
            }
        }

        /// <summary>
        /// Отображает информацию о клиенте.
        /// </summary>
        private void DisplayClientInfo()
        {
            WriteLine($"IS HOST: {isClientHost}");
            WriteLine($"ROLE: {gameClient.role}");
            WriteLine($"ID: {gameClient.playerID}");
            WriteLine($"SECOND PLAYER: {gameClient.secondPlayer}");
        }

        /// <summary>
        /// Отображает главное меню игры и обрабатывает выбор пользователя.
        /// </summary>
        /// <param name="selectedItem">Выбранный пункт меню (передается по ссылке).</param>
        /// <param name="exitRequested">Флаг запроса выхода из игры (передается по ссылке).</param>
        private void DisplayMainMenu(ref int selectedItem, ref bool exitRequested)
        {
            string[] menuItems = { "Новая игра", "Выход" };

            for (var i = 0; i < menuItems.Length; i++)
            {
                ForegroundColor = i == selectedItem ? ConsoleColor.White : ConsoleColor.Gray;
                WriteLine((i == selectedItem ? ">> " : "   ") + menuItems[i]);
            }

            var keyInfo = ReadKey(true);

            if (keyInfo.Key == ConsoleKey.W && selectedItem > 0)
            {
                selectedItem--;
            }
            else if (keyInfo.Key == ConsoleKey.S && selectedItem < menuItems.Length - 1)
            {
                selectedItem++;
            }
            else if (keyInfo.Key == ConsoleKey.Enter)
            {
                if (selectedItem == menuItems.Length - 1)
                {
                    exitRequested = true; // Выход из игры.
                }
                else if (selectedItem == 0)
                {
                    gameState = GameState.Lobby; // Переход в лобби.
                }
            }
        }

        /// <summary>
        /// Отображает меню лобби и обрабатывает выбор пользователя.
        /// </summary>
        /// <param name="selectedItem">Выбранный пункт меню (передается по ссылке).</param>
        private void ShowLobbyMenu(ref int selectedItem)
        {
            string[] menuItems = { "Запустить игру" };

            for (var i = 0; i < menuItems.Length; i++)
            {
                ForegroundColor = i == selectedItem ? ConsoleColor.White : ConsoleColor.Gray;
                WriteLine((i == selectedItem ? ">> " : "   ") + menuItems[i]);
            }

            var keyInfo = ReadKey(true);

            if (keyInfo.Key == ConsoleKey.W && selectedItem > 0)
            {
                selectedItem--;
            }
            else if (keyInfo.Key == ConsoleKey.S && selectedItem < menuItems.Length - 1)
            {
                selectedItem++;
            }
            else if (keyInfo.Key == ConsoleKey.Enter)
            {
                if (selectedItem == 0)
                {
                    gameClient.StartGame(); // Запуск игры.
                }
            }
        }

        /// <summary>
        /// Запускает основную игру.
        /// </summary>
        /// <returns>Данные о состоянии игры по окончании.</returns>
        private GameStateData StartGame()
        {
            bool isGameOver = false;
            Direction dir1 = Direction.Right, dir2 = Direction.Right;

            Snake snake1 = new Snake(10, 5, ConsoleColor.Blue, ConsoleColor.DarkBlue);
            Snake snake2 = new Snake(10, 15, ConsoleColor.Red, ConsoleColor.DarkRed);

            Pixel food1 = GenFood(snake1);
            Pixel food2 = GenFood(snake2);

            int score1 = 0, score2 = 0;

            Clear();
            DrawBoard();

            food1.Draw();
            food2.Draw();

            var sw = new Stopwatch();
            int lagMs = 0;

            // Игровой цикл.
            while (!isGameOver)
            {
                HandleUserInput(ref dir1, ref dir2);

                // Обновляем поле игрока 1.
                if (snake1.Head.X == food1.X && snake1.Head.Y == food1.Y)
                {
                    snake1.Move(dir1, true); // Увеличение змеи при поедании еды.
                    food1 = GenFood(snake1);
                    food1.Draw();
                    score1++;
                }
                else
                {
                    snake1.Move(dir1);
                }

                // Обновляем поле игрока 2.
                if (snake2.Head.X == food2.X && snake2.Head.Y == food2.Y)
                {
                    snake2.Move(dir2, true); // Увеличение змеи при поедании еды.
                    food2 = GenFood(snake2);
                    food2.Draw();
                    score2++;
                }
                else
                {
                    snake2.Move(dir2);
                }

                // Проверка на столкновения.
                if (CheckCollision(snake1) || CheckCollision(snake2))
                {
                    isGameOver = true; // Конец игры.
                }

                // Отображаем счёт.
                DisplayScores(score1, score2);

                Thread.Sleep(FRAME_MILLISECONDS - lagMs); // Задержка между кадрами.
                sw.Restart(); // Сбрасываем таймер.
                lagMs = (int)sw.ElapsedMilliseconds; // Задержка.
            }

            gameState = GameState.Lobby; // Возвращаемся в лобби по окончании игры.
            return new GameStateData();
        }

        /// <summary>
        /// Обрабатывает ввод пользователя для управления змеями.
        /// </summary>
        /// <param name="dir1">Направление первой змеи (передается по ссылке).</param>
        /// <param name="dir2">Направление второй змеи (передается по ссылке).</param>
        private void HandleUserInput(ref Direction dir1, ref Direction dir2)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true).Key;

                if (gameClient.playerID == 0) // Управление для первого игрока.
                {
                    UpdateDirection(ref dir1, key);
                    gameClient.SendDirection(dir1); // Отправка направления на сервер.
                }
                else // Управление для второго игрока
                {
                    UpdateDirection(ref dir2, key);
                    gameClient.SendDirection(dir2); // Отправка направления на сервер.
                }
            }

            // Обновление направления противника.
            if (gameClient.playerID == 0)
            {
                dir2 = opponentDirection; // Для второго игрока.
            }
            else
            {
                dir1 = opponentDirection; // Для первого игрока.
            }
        }

        /// <summary>
        /// Обновляет направление змеи в зависимости от нажатой клавиши.
        /// </summary>
        /// <param name="direction">Направление змеи (передается по ссылке).</param>
        /// <param name="key">Нажатая клавиша.</param>
        private void UpdateDirection(ref Direction direction, ConsoleKey key)
        {
            if (key == ConsoleKey.W && direction != Direction.Down) direction = Direction.Up;
            else if (key == ConsoleKey.S && direction != Direction.Up) direction = Direction.Down;
            else if (key == ConsoleKey.A && direction != Direction.Right) direction = Direction.Left;
            else if (key == ConsoleKey.D && direction != Direction.Left) direction = Direction.Right;
        }

        /// <summary>
        /// Проверяет, не произошло ли столкновение змеи с границами карты или с самой собой.
        /// </summary>
        /// <param name="snake">Змея для проверки.</param>
        /// <returns>True, если произошло столкновение; иначе false.</returns>
        private bool CheckCollision(Snake snake)
        {
            return snake.Head.X == 0 || snake.Head.X == MAP_WIDTH - 1 ||
                   snake.Head.Y == 0 || snake.Head.Y == MAP_HEIGHT - 1 ||
                   snake.Body.Any(b => b.X == snake.Head.X && b.Y == snake.Head.Y);
        }

        /// <summary>
        /// Отображает счёта обоих игроков на экране.
        /// </summary>
        /// <param name="score1">Счёт первого игрока.</param>
        /// <param name="score2">Счёт второго игрока.</param>
        private void DisplayScores(int score1, int score2)
        {
            SetCursorPosition(2, SCREEN_HEIGHT + 1);
            Write($"Игрок 1: {score1}");
            SetCursorPosition(SCREEN_WIDTH / 2 + 2, SCREEN_HEIGHT + 1);
            Write($"Игрок 2: {score2}");
        }

        /// <summary>
        /// Обрабатывает окончание игры и обновляет таблицу рекордов.
        /// </summary>
        /// <param name="records">Список рекордов.</param>
        /// <param name="match">Данные об окончании текущей игры.</param>
        private void HandleGameOver(GameStateData match)
        {
            printer.GameOverPrint();
            WriteLine($"Ваши очки: {match.Score}\n");
            WriteLine("Нажмите Enter чтобы вернуться в главное меню.\n");

            while (true)
            {
                var keyInfo = ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Enter)
                    gameState = GameState.MainMenu; // Возврат в главное меню.
                break;
            }
        }

  
        /// <summary>
        /// Отрисовывает границы игрового поля.
        /// </summary>
        private void DrawBoard()
        {
            for (var i = 0; i < MAP_WIDTH; i++)
            {
                new Pixel(i, 0, BORDER_COLOR).Draw(); // Верхняя граница
                new Pixel(i, MAP_HEIGHT - 1, BORDER_COLOR).Draw(); // Нижняя граница
            }

            for (var i = 0; i < MAP_HEIGHT; i++)
            {
                new Pixel(0, i, BORDER_COLOR).Draw(); // Левая граница
                new Pixel(MAP_WIDTH - 1, i, BORDER_COLOR).Draw(); // Правая граница
            }
        }

        /// <summary>
        /// Генерирует новую еду для змейки.
        /// </summary>
        /// <param name="snake">Змея, для которой генерируется еда.</param>
        /// <returns>Созданный объект Pixel, представляющий еду.</returns>
        private Pixel GenFood(Snake snake)
        {
            Pixel food;

            do
            {
                food = new Pixel(Random.Next(1, MAP_WIDTH - 2), Random.Next(1, MAP_HEIGHT - 2), FOOD_COLOR);
            } while (snake.Head.X == food.X && snake.Head.Y == food.Y ||
                     snake.Body.Any(b => b.X == food.X && b.Y == food.Y));

            return food; // Возвращаем сгенерированную еду
        }

    }
}
