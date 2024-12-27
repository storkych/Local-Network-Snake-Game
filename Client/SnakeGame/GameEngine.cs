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
        private const int MAP_WIDTH = 20;
        private const int MAP_HEIGHT = 15;
        private const int MAX_RECORDS = 10;
        private const int SCREEN_WIDTH = MAP_WIDTH * 3;
        private const int SCREEN_HEIGHT = MAP_HEIGHT * 3;
        private const int FRAME_MILLISECONDS = 100;

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

        public Direction opponentDirection = Direction.Right; // Направление по умолчанию
        public bool secondPlayerReady = false;

        private GameClient gameClient;
        public bool isClientHost = true;

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
                        HandleGameOver(records, match);
                        break;

                    case GameState.Paused:
                        PauseMenu(ref selectedItem);
                        break;
                }
                Clear();
            }
        }

        private void DisplayClientInfo()
        {
            WriteLine($"IS HOST: {isClientHost}");
            WriteLine($"ROLE: {gameClient.role}");
            WriteLine($"ID: {gameClient.playerID}");
            WriteLine($"SECOND PLAYER: {gameClient.secondPlayer}");
        }

        private void DisplayMainMenu(ref int selectedItem, ref bool exitRequested)
        {
            string[] menuItems = { "Новая игра", "Таблица рекордов", "Выход" };

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
                    exitRequested = true;
                }
                else if (selectedItem == 0)
                {
                    gameState = GameState.Lobby;
                }
            }
        }

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
                    gameClient.StartGame();
                }
            }
        }

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

                // Проверка на столкновения
                if (CheckCollision(snake1) || CheckCollision(snake2))
                {
                    isGameOver = true;
                }

                DisplayScores(score1, score2);

                Thread.Sleep(FRAME_MILLISECONDS - lagMs);
                sw.Restart();
                lagMs = (int)sw.ElapsedMilliseconds;
            }

            gameState = GameState.Lobby;
            return new GameStateData();
        }

        private void HandleUserInput(ref Direction dir1, ref Direction dir2)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true).Key;

                if (gameClient.playerID == 0) // Первого игрока
                {
                    UpdateDirection(ref dir1, key);
                    gameClient.SendDirection(dir1); // Отправка направления на сервер
                }
                else // Второй игрок
                {
                    UpdateDirection(ref dir2, key);
                    gameClient.SendDirection(dir2); // Отправка направления на сервер
                }
            }

            // Обновление направления противника с учетом текущего состояния
            if (gameClient.playerID == 0)
            {
                dir2 = opponentDirection; // Второго игрока
            }
            else
            {
                dir1 = opponentDirection; // Первого игрока
            }
        }

        private void UpdateDirection(ref Direction direction, ConsoleKey key)
        {
            if (key == ConsoleKey.W && direction != Direction.Down) direction = Direction.Up;
            else if (key == ConsoleKey.S && direction != Direction.Up) direction = Direction.Down;
            else if (key == ConsoleKey.A && direction != Direction.Right) direction = Direction.Left;
            else if (key == ConsoleKey.D && direction != Direction.Left) direction = Direction.Right;
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

        private void HandleGameOver(List<string> records, GameStateData match)
        {
            records.Add($"{match.PlayerName} {match.Score}");
            records.Sort((a, b) => int.Parse(b.Split(' ')[1]) - int.Parse(a.Split(' ')[1]));

            saveLoadManager.WriteRecords(records);
            printer.GameOverPrint();
            WriteLine($"Ваши очки: {match.Score}\n");
            WriteLine("Нажмите Enter чтобы вернуться в главное меню.\n");

            while (true)
            {
                var keyInfo = ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Enter)
                    gameState = GameState.MainMenu;
                break;
            }
        }

        private void PauseMenu(ref int selectedItem)
        {
            Clear();
            printer.PausePrint();

            string[] menuPItems = { "Продолжить игру", "Сохранить игру", "Выйти в меню" };

            for (var i = 0; i < menuPItems.Length; i++)
            {
                ForegroundColor = i == selectedItem ? ConsoleColor.White : ConsoleColor.Gray;
                WriteLine((i == selectedItem ? ">> " : "   ") + menuPItems[i]);
            }

            var keyInfo = ReadKey(true);

            if (keyInfo.Key == ConsoleKey.W && selectedItem > 0)
            {
                selectedItem--;
            }
            else if (keyInfo.Key == ConsoleKey.S && selectedItem < menuPItems.Length - 1)
            {
                selectedItem++;
            }
            else if (keyInfo.Key == ConsoleKey.Enter)
            {
                HandlePauseMenuSelection(selectedItem);
            }
        }

        private void HandlePauseMenuSelection(int selectedItem)
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
                WriteLine($"{i + 1}. {record[0]}: {record[1]}");
            }

            WriteLine("\nНажмите Enter, чтобы вернуться в главное меню.");
            ReadKey();
        }
    }
}
