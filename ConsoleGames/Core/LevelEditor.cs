using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConsoleGames
{
    public class LevelEditor : ConsolePanel
    {
        public const string LevelFileExtension = ".lvl";
        private IEnumerable<string> LevelLibraryFilePaths => Directory.GetFiles(SavedLevelsDirectory).Where(f => f.ToLower().EndsWith(LevelFileExtension));
        private ConsoleBitmapEditor innerEditor;
        private Dictionary<Point, List<string>> tags = new Dictionary<Point, List<string>>();
        private string SavedLevelsDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LevelsLibrary");
        private string currentLevelPath;
        private bool hasUnsavedChanges = false;
        private Button saveCommand;
        public LevelEditor(int levelWidth, int levelHeight, ConsoleCharacter? bg = null)
        {
            if (Directory.Exists(SavedLevelsDirectory) == false)
            {
                Directory.CreateDirectory(SavedLevelsDirectory);
            }

            innerEditor = Add(new ConsoleBitmapEditor(levelWidth, levelHeight, bg));

            innerEditor.BitmapChanged.SubscribeForLifetime(() => hasUnsavedChanges = true, this.LifetimeManager);

            this.Width = innerEditor.Width;
            this.Height = innerEditor.Height;

            var newCommand = innerEditor.AddCommand(new Button() { Text = "New".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.N, ConsoleModifiers.Alt) });
            var openCommand = innerEditor.AddCommand(new Button() { Text = "Open".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.O, ConsoleModifiers.Alt) });
            saveCommand = innerEditor.AddCommand(new Button() { Text = "Save".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.S, ConsoleModifiers.Alt) });
            var saveAsCommand = innerEditor.AddCommand(new Button() { Text = "Save as".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.A, ConsoleModifiers.Alt) });
            var discardCommand = innerEditor.AddCommand(new Button() { Text = "Discard".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.D, ConsoleModifiers.Alt) });
            var tagCommand = innerEditor.AddCommand(new Button() { Text = "Tag".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.T, ConsoleModifiers.Alt) });

            newCommand.Pressed.SubscribeForLifetime(() =>
            {
                if (hasUnsavedChanges == false)
                {
                    Load(new Level());
                    currentLevelPath = null;
                }
                else
                {
                    UnsavedChanges(()=>
                    {
                        Load(new Level());
                        currentLevelPath = null;
                    });
                }
            }, this.LifetimeManager);

            openCommand.Pressed.SubscribeForLifetime(() =>
            {
                if (hasUnsavedChanges == false)
                {
                    Open();
                }
                else
                {
                    UnsavedChanges(()=>
                    {
                        Open();
                    });
                }
            }, this.LifetimeManager);


            saveCommand.Pressed.SubscribeForLifetime(() =>
            {
                if (currentLevelPath != null)
                {
                    var level = ExtractLevel();
                    var json = level.Serialize();
                    File.WriteAllText(currentLevelPath, json);
                    hasUnsavedChanges = false;
                }
                else
                {
                    saveAsCommand.Pressed.Fire();
                }

            }, this.LifetimeManager);


            saveAsCommand.Pressed.SubscribeForLifetime(() =>
            {
                Dialog.ShowRichTextInput("Choose a name for this level".ToConsoleString(), (val) =>
                {
                    currentLevelPath = Path.Combine(SavedLevelsDirectory, val.ToString() + LevelFileExtension);
                    saveCommand.Pressed.Fire();
                }, initialValue: currentLevelPath != null ? Path.GetFileNameWithoutExtension(currentLevelPath).ToConsoleString() : ConsoleString.Empty);
            }, this.LifetimeManager);

            discardCommand.Pressed.SubscribeForLifetime(() =>
            {
                Dialog.ConfirmYesOrNo("Are you sure you want to discard your unsaved changed?", () =>
                {
                    if (currentLevelPath != null)
                    {
                        Load(currentLevelPath);
                    }
                    else
                    {
                        Clear();
                    }
                });
            }, this.LifetimeManager);

            tagCommand.Pressed.SubscribeForLifetime(() =>
            {
                if (tags.TryGetValue(innerEditor.CursorPosition, out List<string> tagsForPosition) == false)
                {
                    tagsForPosition = new List<string>();
                    tags.Add(innerEditor.CursorPosition, tagsForPosition);
                }

                var tagsString = string.Join(';', tagsForPosition);

                Dialog.ShowRichTextInput("Add/edit semi-colon delimited tags. Press enter to cmmmit".ToYellow(), (newString) =>
                {
                    var split = newString.ToString().Split(";").ToList();
                    tagsForPosition.Clear();
                    tagsForPosition.AddRange(split);
                },
                initialValue: tagsString.ToConsoleString());
            }, this.LifetimeManager);
        }

        private void Open()
        {
            Dialog.Pick("Choose a level to open".ToConsoleString(), LevelLibraryFilePaths.Select(p => new DialogOption()
            {
                Id = p,
                DisplayText = Path.GetFileNameWithoutExtension(p).ToConsoleString()
            }), maxHeight: 20).Then((o) =>
            {
                Load(o.Id);
            });
        }

        private void UnsavedChanges(Action discardAction)
        {
            Dialog.ShowMessage("You have unsaved changes".ToYellow(), (result) =>
            {
                if (result.Id == "save")
                {
                    saveCommand.Pressed.Fire();
                }
                else if (result.Id == "discard")
                {
                    discardAction();
                }
                else if (result.Id == "cancel")
                {
                    // do nothing
                }
                else
                {
                    throw new ArgumentOutOfRangeException("Unexpected result: " + result.Id);
                }

            }, buttons: new DialogButton[]
                    {
                        new DialogButton() { DisplayText = "Save".ToConsoleString(), Id = "save" },
                        new DialogButton() { DisplayText = "Discard changes".ToConsoleString(), Id="discard" },
                        new DialogButton() { DisplayText = "Cancel".ToConsoleString(), Id="cancel" },
                    });
        }
    
        private void Clear()
        {
            this.innerEditor.Bitmap.Pen = new ConsoleCharacter(' ');
            this.innerEditor.Bitmap.FillRect(0, 0, innerEditor.Bitmap.Width, innerEditor.Bitmap.Height);
            this.tags.Clear();
        }

        private void Load(string path)
        {
            try
            {
                var level = Level.Deserialize(File.ReadAllText(path));
                Load(level);
                currentLevelPath = path;
            }
            catch (Exception ex)
            {
                Dialog.ShowMessage($"Failed to open level file {path}\n\n{ex.ToString()}".ToRed());
            }
        }

        private void Load(Level l)
        {
            Clear();
            foreach (var item in l.Items)
            {
                this.innerEditor.Bitmap.Pen = new ConsoleCharacter(item.Symbol, item.FG, item.BG);
                this.innerEditor.Bitmap.DrawPoint(item.X, item.Y);
                this.tags.Add(new Point(item.X, item.Y), item.Tags);
            }
            hasUnsavedChanges = false;
        }

        private Level ExtractLevel()
        {
            var ret = new Level();
            for (var x = 0; x < innerEditor.Bitmap.Width; x++)
            {
                for (var y = 0; y < innerEditor.Bitmap.Height; y++)
                {
                    var pixel = innerEditor.Bitmap.GetPixel(x, y);
                    if (pixel.Value.HasValue == false)
                    {
                        continue;
                    }

                    ret.Items.Add(new LevelItem()
                    {
                        X = x,
                        Y = y,
                        Width = 1,
                        Height = 1,
                        FG = pixel.Value.Value.ForegroundColor,
                        BG = pixel.Value.Value.BackgroundColor,
                        Symbol = pixel.Value.Value.Value,
                        Tags = tags.ContainsKey(new Point(x, y)) ? tags[new Point(x, y)] : new List<string>(),
                    });
                }
            }

            return ret;
        }
    }
}
