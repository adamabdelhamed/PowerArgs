using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ArgsTests
{
    public class TempFiles : IDisposable
    {
        private bool disposed = false;

        List<string> dirs = new List<string>();
        List<string> files = new List<string>();
        string cd;

        public TempFiles()
        {
            cd = Environment.CurrentDirectory;
            Environment.CurrentDirectory = Path.GetTempPath();
        }

        public FileStream CreateFile(string file)
        {
            files.Add(file);
            return File.Create(file);
        }

        public void CreateDirectory(string dir, params string[] emptyFiles)
        {
            Directory.CreateDirectory(dir);
            dirs.Add(dir);
            foreach (string empty in emptyFiles)
            {
                var full = Path.Combine(dir, empty);
                File.WriteAllText(full, "");
                files.Add(full);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    foreach (string file in files)
                    {
                        File.Delete(file);
                    }

                    foreach (string dir in dirs)
                    {
                        Directory.Delete(dir, true);
                    }

                    Environment.CurrentDirectory = cd;
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
