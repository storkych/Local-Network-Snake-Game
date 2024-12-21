using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static System.Console;

namespace SnakeGame
{
    /// <summary>
    /// Класс, отвечающий за логику игры.
    /// </summary>
    internal class GameEngine
    {
        private const int MAP_WIDTH = 30;
        private const int MAP_HEIGHT = 20;
        private const int MAX_RECORDS = 10;

        private const int SCREEN_WIDTH = MAP_WIDTH * 3;
        private const int SCREEN_HEIGHT = MAP_HEIGHT * 3;

        private const int FRAME_MILLISECONDS = 200;

        private const ConsoleColor BORDER_COLOR = ConsoleColor.White;
        private const ConsoleColor FOOD_COLOR = ConsoleColor.Green;
        private const ConsoleColor BODY_COLOR = ConsoleColor.White;
        private const ConsoleColor HEAD_COLOR = ConsoleColor.DarkGray;

        private static readonly Random Random = new Random();

        private GameState gameState;
        private GameStateData gameStateData;
        private readonly TitlePrinter printer;
        private readonly SaveLoadManager saveLoadManager;

        static bool pauseRequested = false;

        /// <summary>
        /// Конструктор класса GameEngine.
        /// </summary>
        public GameEngine()
        {
            gameState = GameState.MainMenu;
            gameStateData = new GameStateData();
            printer = new TitlePrinter();
            saveLoadManager = new SaveLoadManager();
            SetWindowSize(SCREEN_WIDTH, SCREEN_HEIGHT + 5);
            SetBufferSize(SCREEN_WIDTH, SCREEN_HEIGHT + 5);
        }

        /// <summary>
        /// Логика работы игры (главное меню, меню паузы).
        /// </summary>
        public void Run()
        {
            GameStateData match = new GameStateData();
            
            bool exitRequested = false;
            gameStateData = saveLoadManager.LoadData();
            CursorVisible = false;
            List<string> records = saveLoadManager.ReadRecords();

            records.Sort((a, b) => int.Parse(b.Split(' ')[1]) - int.Parse(a.Split(' ')[1]));

            ConsoleKeyInfo keyInfo;
            int selectedItem = 0;

            while (!exitRequested)
                {
                    switch (gameState)
                    {
                    // Отображение главного меню.
                    // Обработка клавиш для выбора опций главного меню.
                    case GameState.MainMenu:

                            Clear();
                            printer.TitlePrint();

                            string[] menuItems = new string[] { "Продолжить игру", "Новая игра", "Таблица рекордов", "Выход" };

                            if (!gameStateData.IsSavedGame)
                            {
                                // Если сохраненной игры нет, "Продолжить игру" будет неактивным (серого цвета).
                                menuItems[0] = "[Недоступно] Продолжить игру";
                            }

                            for (var i = 0; i < menuItems.Length; i++)
                            {
                                if (i == selectedItem)
                                {
                                    ForegroundColor = ConsoleColor.White;
                                }
                                else
                                {
                                    ForegroundColor = i == 0 && !gameStateData.IsSavedGame ? ConsoleColor.Gray : ConsoleColor.White;
                                }

                                WriteLine((i == selectedItem ? ">> " : "   ") + menuItems[i]);
                            }

                            keyInfo = ReadKey(true);

                            if ((keyInfo.Key == ConsoleKey.W) && (selectedItem > 0))
                            {
                                selectedItem--;
                            }
                            else if ((keyInfo.Key == ConsoleKey.S) && (selectedItem < menuItems.Length - 1))
                            {
                                selectedItem++;
                            }
                            else if (keyInfo.Key == ConsoleKey.Enter)
                            {
                                if (selectedItem == menuItems.Length - 1)
                                {
                                    // Устанавливаем флаг выхода.
                                    exitRequested = true;
                                }
                                else if ((selectedItem == 0) && (gameStateData.IsSavedGame))
                                {
                                    // Обработка продолжения игры.
                                    gameState = GameState.InGame;
                                }
                                else if (selectedItem == 1)
                                {
                                    // Обработка начала новой игры.
                                    gameState = GameState.InGame;
                                    gameStateData = new GameStateData();
                                    saveLoadManager.QuietSave(gameStateData);
                                }
                                else if (selectedItem == 2)
                                {
                                    ShowRecords(records);
                                    selectedItem = 0;
                                }
                            }                            
                            break;
                    // Запуск самой игры со змейкой.                    
                    case GameState.InGame:

                            Clear();
                            match = StartGame();
                                                    
                            break;
                    // Отображение экрана смерти.
                    // Обработка клавиш для перезапуска игры или возврата в главное меню.
                    case GameState.GameOver:

                            Clear();

                            records.Add($"{match.PlayerName} {match.Score}");
                            records.Sort((a, b) => int.Parse(b.Split(' ')[1]) - int.Parse(a.Split(' ')[1]));

                            saveLoadManager.WriteRecords(records);

                            printer.GameOverPrint();
                            WriteLine($"Ваши очки: {match.Score}\n");
                            WriteLine("Нажмите Enter чтобы вернуться в главное меню.\n");

                            while (true)
                            {
                                var keyInafo = ReadKey(true);
                                if (keyInafo.Key == ConsoleKey.Enter)
                                    gameState = GameState.MainMenu;
                                break;
                            }                            
                            break;
                        case GameState.Paused:

                            Clear();
                            printer.PausePrint();

                            string[] menuPItems = { "Продолжить игру", "Сохранить игру", "Выйти в меню" };

                            for (var i = 0; i < menuPItems.Length; i++)
                            {
                                if (i == selectedItem)
                                {
                                    ForegroundColor = ConsoleColor.White;
                                }
                                else
                                {
                                    ForegroundColor = ConsoleColor.Gray;
                                }

                                WriteLine((i == selectedItem ? ">> " : "   ") + menuPItems[i]);
                            }

                            keyInfo = ReadKey(true);

                            if ((keyInfo.Key == ConsoleKey.W) && (selectedItem > 0))
                            {
                                selectedItem--;
                            }
                            else if ((keyInfo.Key == ConsoleKey.S) && (selectedItem < menuPItems.Length - 1))
                            {
                                selectedItem++;
                            }
                            else if (keyInfo.Key == ConsoleKey.Enter)
                            {
                                if (selectedItem == 2)
                                {
                                    gameState = GameState.MainMenu;
                                }
                                else if (selectedItem == 0)
                                {
                                    gameState = GameState.InGame;
                                }
                                else if (selectedItem == 1)
                                {
                                    saveLoadManager.SaveData(gameStateData);
                                }
                            }
                            break;
                    }
                    Clear();
            }
        }

        /// <summary>
        /// Начинает игру или продолжает сохраненную игру.
        /// </summary>
        /// <returns></returns>
        private GameStateData StartGame()
        {
            string playerName = "";
            bool isGameOver = false;

            Snake snake;
            Pixel food;
            int score = 0;
            GameStateData matchData = new GameStateData();
            Direction SnakeDir;

            if (!gameStateData.IsSavedGame)
            {
                playerName = GetPlayerName();

                snake = new Snake(10, 5, HEAD_COLOR, BODY_COLOR);
                food = GenFood(snake);
                SnakeDir = Direction.Right;
            }
            else
            {
                playerName = gameStateData.PlayerName;
                snake = new Snake(gameStateData.Head, gameStateData.Body);
                food = gameStateData.Food;
                score = gameStateData.Score;
                SnakeDir = gameStateData.SnakeDir;
            }
            Clear();
            DrawBoard();
            food.Draw();

            Direction currentMovement = Direction.Right;

            int lagMs = 0;
            var sw = new Stopwatch();

            string infoText = $"| Текущий счёт: {score} |";
            int x = (SCREEN_WIDTH - infoText.Length) / 2;
            SetCursorPosition((SCREEN_WIDTH - playerName.Length) / 2, SCREEN_HEIGHT + 1);
            Write($"{playerName}");
            SetCursorPosition((SCREEN_WIDTH - 15) / 2, SCREEN_HEIGHT + 3);
            Write($"| ESC - Пауза |");
            while (!isGameOver && !pauseRequested)
            {
                // Метод, считывающий управление.
                HandleUserInput(ref SnakeDir); 
                SetCursorPosition(0, 0);
                // Очистка предыдущей строки.
                Write(new string(' ', SCREEN_WIDTH));
                SetCursorPosition(x, 1);
                // Вывод строки с информацией.
                Write($"| Текущий счёт: {score} |");

                sw.Restart();
                Direction oldMovement = currentMovement;

                while (sw.ElapsedMilliseconds <= FRAME_MILLISECONDS - lagMs)
                {
                    if (currentMovement == oldMovement)
                    {
                        currentMovement = SnakeDir;
                    }
                }

                sw.Restart();
                                
                if ((snake.Head.X == food.X) && (snake.Head.Y == food.Y))
                {
                    snake.Move(currentMovement, true);
                    food = GenFood(snake);
                    food.Draw();

                    score++;
                }
                else
                {
                    snake.Move(currentMovement);
                }
                // Условие смерти змейки.
                if ((snake.Head.X == MAP_WIDTH - 1)
                    || (snake.Head.X == 0)
                    || (snake.Head.Y == MAP_HEIGHT - 1)
                    || (snake.Head.Y == 0)
                    || (snake.Body.Any(b => b.X == snake.Head.X && b.Y == snake.Head.Y)))
                {
                    isGameOver = true;
                }

                lagMs = (int)sw.ElapsedMilliseconds;
            }

            matchData.Food = food;
            matchData.Score = score;
            matchData.PlayerName = playerName;
            matchData.Head = snake.Head;
            matchData.Body = snake.Body;
            matchData.SnakeDir = SnakeDir;

            if (pauseRequested)
            {
                pauseRequested = false;
                gameStateData = matchData;
                gameStateData.IsSavedGame = true;
                gameState = GameState.Paused;
            }
            else
            {
                gameStateData = new GameStateData();
                saveLoadManager.QuietSave(gameStateData);
                for (int i = 1; i < MAP_WIDTH - 1; i++)
                {
                    for (int j = 1; j < MAP_HEIGHT - 1; j++)
                    {
                        SetCursorPosition(i * 3, j);
                        Write(" ");
                    }
                }
                gameState = GameState.GameOver;
            }

            snake.Clear();
            food.Clear();
            return matchData;
        }

        /// <summary>
        /// Считывает нажатия клавиш пользователем и меняет направление змейки.
        /// </summary>
        /// <param name="snakeDir"> - направление змейки. </param>
        private void HandleUserInput(ref Direction snakeDir)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true).Key;
                if (key == ConsoleKey.Escape)
                {
                    pauseRequested = true;
                }
                else if (key == ConsoleKey.W)
                {
                    if (snakeDir != Direction.Down) { snakeDir = Direction.Up; }
                }
                else if (key == ConsoleKey.S)
                {
                    if (snakeDir != Direction.Up) { snakeDir = Direction.Down; }
                }
                else if (key == ConsoleKey.A)
                {
                    if (snakeDir != Direction.Right) { snakeDir = Direction.Left; }
                }
                else if (key == ConsoleKey.D)
                {
                    if (snakeDir != Direction.Left) { snakeDir = Direction.Right; }
                }
            }
        }

        /// <summary>
        /// Запрашивает у пользователя ввод ника.
        /// </summary>
        /// <returns></returns>
        private string GetPlayerName()
        {
            string playerName;
            do
            {
                Clear();
                Write("Введите ваш ник: ");
                // Если ReadLine() возвращает null, присвоим пустую строку.
                playerName = ReadLine() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(playerName))
                {
                    WriteLine("Имя не может быть пустым. Пожалуйста, введите ваш ник.");
                    ReadKey();
                }
                else if (playerName.Length > 15)
                {
                    WriteLine("Имя не может быть больше 15 символов. Пожалуйста, введите ваш ник.");
                    ReadKey();
                }
                else if (playerName.Contains(" "))
                {
                    WriteLine("Имя не может включать пробел. Пожалуйста, введите ваш ник.");
                    ReadKey();
                }
            } while (string.IsNullOrEmpty(playerName) || playerName.Length > 15 || playerName.Contains(" "));
            return playerName;
        }

        /// <summary>
        /// Отрисовывает границы игрового поля.
        /// </summary>
        private void DrawBoard()
        {
            for (var i = 0; i < MAP_WIDTH; i++)
            {
                new Pixel(i, 0, BORDER_COLOR).Draw();
                new Pixel(i, MAP_HEIGHT - 1, BORDER_COLOR).Draw();
            }

            for (var i = 0; i < MAP_HEIGHT; i++)
            {
                new Pixel(0, i, BORDER_COLOR).Draw();
                new Pixel(MAP_WIDTH - 1, i, BORDER_COLOR).Draw();
            }
        }

        /// <summary>
        /// Генерирует новую еду для змейки.
        /// </summary>
        /// <param name="snake"> - змея. </param>
        /// <returns></returns>
        private Pixel GenFood(Snake snake)
        {
            Pixel food;

            do
            {
                food = new Pixel(Random.Next(1, MAP_WIDTH - 2), Random.Next(1, MAP_HEIGHT - 2), FOOD_COLOR);
            } while (snake.Head.X == food.X && snake.Head.Y == food.Y ||
                     snake.Body.Any(b => b.X == food.X && b.Y == food.Y));

            return food;
        }

        /// <summary>
        /// Отображает таблицу рекордов.
        /// </summary>
        /// <param name="records"> - лист рекордов. </param>
        private void ShowRecords(List<string> records)
        {
            Clear();

            WriteLine("Таблица рекордов:");

            for (var i = 0; i < records.Count; i++)
            {
                if (i >= MAX_RECORDS)
                {
                    break;
                }

                string[] record = records[i].Split(' ');
                Debug.WriteLine(i);
                WriteLine($"{i + 1}. {record[0]}: {record[1]}");
            }

            WriteLine("\nНажмите Enter, чтобы вернуться в главное меню.");
            ReadKey();
        }

    }
}
