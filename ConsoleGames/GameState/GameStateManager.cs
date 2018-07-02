using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ConsoleGames
{
    public class GameState
    {
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        public void SetValue(string key, object data)
        {
            if(Data.ContainsKey(key))
            {
                Data[key] = data;
            }
            else
            {
                Data.Add(key, data);
            }
        }
    }

    public class GameStateManager
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented
        };

        public const string SavedGameExtension = ".game";
        private static string SavedGamessDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PowerArgsGames", Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location), "SavedGames");
        public string DefaultSavedGameName { get; private set; } = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);

        public IEnumerable<string> KnownSavedGameNames => Directory
            .GetFiles(SavedGamessDirectory)
            .Where(f => f.ToLower().EndsWith(SavedGameExtension))
            .Select(f => Path.GetFileNameWithoutExtension(f));


        public GameStateManager()
        {
            if (Directory.Exists(SavedGamessDirectory) == false)
            {
                Directory.CreateDirectory(SavedGamessDirectory);
            }
        }

        public void DeleteSavedGame(string name)
        {
            var path = Path.Combine(SavedGamessDirectory, name + SavedGameExtension);
            if(File.Exists(path))
            {
                File.Delete(path);
            }
            else
            {
                throw new ArgumentException("There is no saved game with name: " + name);
            }
        }

        public void SaveGame(GameState state, string name)
        {
            var json = JsonConvert.SerializeObject(state, JsonSettings);
            var path = Path.Combine(SavedGamessDirectory, name + SavedGameExtension);
            File.WriteAllText(path, json);
        }

        public GameState LoadSavedGameOrDefault(string savedGameName)
        {
            if (TryLoadSavedGame(savedGameName, out GameState ret) == false)
            {
                return new GameState();
            }
            else
            {
                return ret;
            }
        }

        public GameState LoadSavedGame(string savedGameName)
        {
            if(TryLoadSavedGame(savedGameName, out GameState ret) == false)
            {
                throw new ArgumentException("There is no saved game with name: "+ savedGameName);
            }
            else
            {
                return ret;
            }
        }

        public bool TryLoadSavedGame(string savedGameName, out GameState state)
        {
            var file = Path.Combine(SavedGamessDirectory, savedGameName + SavedGameExtension);
            if(File.Exists(file) == false)
            {
                state = null;
                return false;
            }
            else
            {
                var json = File.ReadAllText(file);
                state = JsonConvert.DeserializeObject<GameState>(json, JsonSettings);
                return true;
            }
        }
    }
}
