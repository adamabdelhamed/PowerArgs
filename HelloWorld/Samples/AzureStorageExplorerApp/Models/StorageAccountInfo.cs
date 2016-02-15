using Newtonsoft.Json;
using PowerArgs.Cli;
using System.Collections.Generic;
using System.IO;

namespace HelloWorld.Samples
{
    public class StorageAccountInfo
    {
        [Filterable]
        [Key]
        public string AccountName { get; set; }
        public string Key { get; set; }
        public bool UseHttps { get; set; }

        private static string SavePath
        {
            get
            {
                var path = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "AzureStorageAccounts.json");
                return path;
            }
        }

        public static List<StorageAccountInfo> Load()
        {
            if (File.Exists(SavePath) == false)
            {
                return new List<StorageAccountInfo>();
            }
            else
            {
                var fileContents = File.ReadAllText(SavePath);
                var ret = JsonConvert.DeserializeObject<List<StorageAccountInfo>>(fileContents);
                return ret;
            }
        }

        public static void Save(IEnumerable<object> info)
        {
            var json = JsonConvert.SerializeObject(info);
            File.WriteAllText(SavePath, json);
        }
    }
}
