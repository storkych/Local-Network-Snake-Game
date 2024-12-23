using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace SnakeGame
{
    /// <summary>
    /// Класс, отвечающий за сохранение и загрузку данных.
    /// </summary>
    internal class SaveLoadManager
    {
        private const string RECORDS_FILE_NAME = "records.txt";
        private const string FILE_NAME = "gameState.json";

        /// <summary>
        /// Загружает данные игры из файла, если такие данные существуют.
        /// </summary>
        public GameStateData LoadData()
        {
            GameStateData data;
            try
            {
                if (File.Exists(FILE_NAME))
                {
                    // Чтение JSON из файла.
                    string json = File.ReadAllText(FILE_NAME);
                    data = JsonConvert.DeserializeObject<GameStateData>(json) ?? new GameStateData();
                }
                else
                {
                    // Обработка ситуации, когда файл не существует.
                    data = new GameStateData();
                }
            }
            catch (Exception ex)
            {
                // Обработка исключения
                Console.WriteLine("Произошла ошибка при загрузке данных: " + ex.Message);
                data = new GameStateData();
            }
            return data;
        }


        /// <summary>
        /// Отвечает за сохранение данных текущей игры.
        /// </summary>
        /// <param name="gameStateData"> - передача текущего состояния игры. </param>
        public void SaveData(GameStateData gameStateData)
        {
            try
            {
                string gameStateJson = JsonConvert.SerializeObject(gameStateData);
                File.WriteAllText(FILE_NAME, gameStateJson);
                Console.Write("Сохранение произошло");
                System.Threading.Thread.Sleep(1000);
                // Очистить фразу после ожидания
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(new string(' ', "Сохранение произошло".Length));
                Console.SetCursorPosition(0, Console.CursorTop);
            }
            catch (Exception ex)
            {
                // Обработка исключения при сохранении данных.
                Console.WriteLine("Произошла ошибка при сохранении данных: " + ex.Message);

                // Попытка создания файла и повторной попытки сохранения данных
                try
                {
                    File.WriteAllText(FILE_NAME, JsonConvert.SerializeObject(gameStateData));
                    Console.WriteLine("Создан новый файл и данные успешно сохранены.");
                }
                catch (Exception createFileEx)
                {
                    Console.WriteLine("Не удалось создать файл: " + createFileEx.Message);
                }
            }
        }



        /// <summary>
        /// Отвечает за сохранение данных текущей игры (вызывается при создании новой игры).
        /// </summary>
        /// <param name="gameStateData"> - передача текущего состояния игры. </param>
        public void QuietSave(GameStateData gameStateData)
        {
            string gameStateJson = JsonConvert.SerializeObject(gameStateData);
            File.WriteAllText(FILE_NAME, gameStateJson);
        }

        /// <summary>
        /// Cчитывает записи рекордов из файла.
        /// </summary>
        /// <returns>Возвращает список рекордов. </returns>
        public List<string> ReadRecords()
        {
            List<string> records = new List<string>();

            if (File.Exists(RECORDS_FILE_NAME))
            {
                records = File.ReadAllLines(RECORDS_FILE_NAME).ToList();
            }

            return records;
        }

        /// <summary>
        /// Записываем рекорды в файл.
        /// </summary>
        /// <param name="records"> - список рекордов. </param>
        public void WriteRecords(List<string> records)
        {
            File.WriteAllLines(RECORDS_FILE_NAME, records);
        }
    }
}
