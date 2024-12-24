using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using static System.Console;
using System.Text;

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

        public GameState gameState;
        private GameStateData gameStateData;
        private readonly TitlePrinter printer;
        private readonly SaveLoadManager saveLoadManager;

        static bool pauseRequested = false;

        private UdpClient client;
        private IPEndPoint serverEndPoint;

        public Direction opponentDirection = Direction.Right; // Направление по умолчанию
        public bool secondPlayerReady = false;

        GameClient gameClient;
        public bool isClientHost = true;

        /// <summary>
        /// Конструктор класса GameEngine.
        /// </summary>
        public GameEngine(GameClient client)
        {
            gameState = GameState.MainMenu;
            gameStateData = new GameStateData();
            printer = new TitlePrinter();
            saveLoadManager = new SaveLoadManager();
            SetWindowSize(SCREEN_WIDTH, SCREEN_HEIGHT + 5);
            SetBufferSize(SCREEN_WIDTH, SCREEN_HEIGHT + 5);
            gameClient = client;
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
                            Console.WriteLine($"IS HOST: {isClientHost}");
                            Console.WriteLine($"ROLE: {gameClient.role}");
                            Console.WriteLine($"ID: {gameClient.playerID}");
                            Console.WriteLine($"SECOND PLAYER: {gameClient.secondPlayer}");
                            string[] menuItems = new string[] { "Новая игра", "Таблица рекордов", "Выход" };

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
                                else if (selectedItem == 0)
                                {
                                    gameState = GameState.Lobby;
                                    // Обработка начала новой игры.

                                }
                                else if (selectedItem == 1)
                                {
                                    ShowRecords(records);
                                    selectedItem = 0;
                                }
                            }                            
                            break;

                    case GameState.Lobby:
                        Clear();
                        while (true)
                        {
                            if (gameClient.playerID == 0)
                            {
                                if (gameClient.secondPlayer)
                                {
                                    WriteLine("\nВторой игрок присоединился.");
                                    WriteLine("\nНажмите любую кнопку чтобы начать!");
                                    ReadKey();
                                }
                                else
                                {
                                    WriteLine("\nЖдём второго игрока!.");
                                    ReadKey();
                                    return;
                                }
                            }
                            else
                            {
                                if (!gameClient.secondPlayer)
                                {
                                    WriteLine("\nЖдём, когда хост начнёт игру!");
                                    ReadKey();
                                    return;
                                }
                            }


                            if (gameClient.secondPlayer)
                            {
                                gameState = GameState.InGame;
                                gameStateData = new GameStateData();
                            }
                        }
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

        private GameStateData StartGame()
        {
            bool isGameOver = false;
            Snake snake1 = new Snake(10, 5, ConsoleColor.Blue, ConsoleColor.DarkBlue);
            Snake snake2 = new Snake(10, 15, ConsoleColor.Red, ConsoleColor.DarkRed);

            Pixel food1 = GenFood(snake1);
            Pixel food2 = GenFood(snake2);

            int score1 = 0, score2 = 0;
            Direction dir1 = Direction.Right, dir2 = Direction.Right;

            Clear();
            DrawBoard();

            food1.Draw();
            food2.Draw();

            var sw = new Stopwatch();
            int lagMs = 0;

            while (!isGameOver)
            {
                HandleUserInput(ref dir1, ref dir2);

                // Обновляем поле игрока 1
                if (snake1.Head.X == food1.X && snake1.Head.Y == food1.Y)
                {
                    snake1.Move(dir1, true);
                    food1 = GenFood(snake1);
                    food1.Draw();
                    score1++;
                }
                else
                {
                    snake1.Move(dir1);
                }

                // Обновляем поле игрока 2
                if (snake2.Head.X == food2.X && snake2.Head.Y == food2.Y)
                {
                    snake2.Move(dir2, true);
                    food2 = GenFood(snake2);
                    food2.Draw();
                    score2++;
                }
                else
                {
                    snake2.Move(dir2);
                }

                // Проверяем столкновения для обеих змей
                if (CheckCollision(snake1) || CheckCollision(snake2))
                {
                    isGameOver = true;
                }

                // Отображаем счета
                SetCursorPosition(2, SCREEN_HEIGHT + 1);
                Write($"Игрок 1: {score1}");
                SetCursorPosition(SCREEN_WIDTH / 2 + 2, SCREEN_HEIGHT + 1);
                Write($"Игрок 2: {score2}");

                Thread.Sleep(FRAME_MILLISECONDS - lagMs);
                sw.Restart();
                lagMs = (int)sw.ElapsedMilliseconds;
            }

            // Возвращаем данные о состоянии игры
            return new GameStateData();
        }

        private void HandleUserInput(ref Direction dir1, ref Direction dir2)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true).Key;
                if (gameClient.playerID == 0)
                {
                    // Управление для змейки 1
                    if (key == ConsoleKey.W && dir1 != Direction.Down) dir1 = Direction.Up;
                    else if (key == ConsoleKey.S && dir1 != Direction.Up) dir1 = Direction.Down;
                    else if (key == ConsoleKey.A && dir1 != Direction.Right) dir1 = Direction.Left;
                    else if (key == ConsoleKey.D && dir1 != Direction.Left) dir1 = Direction.Right;
                    // Вторая по результатам с сервера
                    gameClient.SendDirection(dir1);
                }
                else
                {
                    // Управление для змейки 2
                    if (key == ConsoleKey.W && dir2 != Direction.Down) dir2 = Direction.Up;
                    else if (key == ConsoleKey.S && dir2 != Direction.Up) dir2 = Direction.Down;
                    else if (key == ConsoleKey.A && dir2 != Direction.Right) dir2 = Direction.Left;
                    else if (key == ConsoleKey.D && dir2 != Direction.Left) dir2 = Direction.Right;
                    // Первая по результатам с сервера
                    gameClient.SendDirection(dir2);
                }
            }
            if (gameClient.playerID == 0)
            {
                // Вторая по результатам с сервера
                dir2 = opponentDirection;
            }
            else
            {
                // Первая по результатам с сервера
                dir1 = opponentDirection;
            }

        }

        private bool CheckCollision(Snake snake)
        {
            return snake.Head.X == 0 || snake.Head.X == MAP_WIDTH - 1 ||
                   snake.Head.Y == 0 || snake.Head.Y == MAP_HEIGHT - 1 ||
                   snake.Body.Any(b => b.X == snake.Head.X && b.Y == snake.Head.Y);
        }


        private void DisplayScores(int score1, int score2)
        {
            SetCursorPosition(2, SCREEN_HEIGHT + 1);
            Write($"Игрок 1: {score1}");
            SetCursorPosition(SCREEN_WIDTH / 2 + 2, SCREEN_HEIGHT + 1);
            Write($"Игрок 2: {score2}");
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
