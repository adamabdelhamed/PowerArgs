using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ConsoleGames
{
    public class LevelEditor : ConsolePanel
    {
        public const string LevelFileExtension = ".lvl";
        private IEnumerable<string> LevelLibraryFilePaths => Directory.GetFiles(SavedLevelsDirectory).Where(f => f.ToLower().EndsWith(LevelFileExtension));
        private ConsoleBitmapEditor innerEditor;
        private Dictionary<Point, List<string>> tags = new Dictionary<Point, List<string>>();
        private static string SavedLevelsDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PowerArgsGames", Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location), "LevelsLibrary");
        private string currentLevelPath;
        private bool hasUnsavedChanges = false;
        private Button saveCommand;

        private Level currentLevel;

        public static Level LoadBySimpleName(string simpleName)
        {
            var ret = Level.Deserialize(File.ReadAllText(Path.Combine(SavedLevelsDirectory, simpleName + LevelFileExtension)));
            ret.Name = simpleName;
            return ret;
        }
        public LevelEditor()
        {
            if (Directory.Exists(SavedLevelsDirectory) == false)
            {
                Directory.CreateDirectory(SavedLevelsDirectory);
            }

            ConfigueEditor();
        }

        private void ConfigueEditor()
        {
            if(innerEditor != null)
            {
                this.Controls.Remove(innerEditor);
            }

            innerEditor = Add(new ConsoleBitmapEditor(currentLevel != null ? currentLevel.Width : Level.DefaultWidth, currentLevel != null ? currentLevel.Height : Level.DefaultHeight));
            innerEditor.BitmapChanged.SubscribeForLifetime(() => hasUnsavedChanges = true, innerEditor);
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
                    LoadLevel(null);
                    currentLevelPath = null;
                }
                else
                {
                    UnsavedChanges(() =>
                    {
                        LoadLevel(null);
                        currentLevelPath = null;
                    });
                }
            }, this);

            openCommand.Pressed.SubscribeForLifetime(() =>
            {
                if (hasUnsavedChanges == false)
                {
                    Open();
                }
                else
                {
                    UnsavedChanges(() =>
                    {
                        Open();
                    });
                }
            }, this);


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

            }, this);


            saveAsCommand.Pressed.SubscribeForLifetime(() =>
            {
                Dialog.ShowRichTextInput("Choose a name for this level".ToConsoleString(), (val) =>
                {
                    currentLevelPath = Path.Combine(SavedLevelsDirectory, val.ToString() + LevelFileExtension);
                    saveCommand.Pressed.Fire();
                }, initialValue: currentLevelPath != null ? Path.GetFileNameWithoutExtension(currentLevelPath).ToConsoleString() : ConsoleString.Empty);
            }, this);

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
                        ConfigueEditor();
                    }
                });
            }, this);

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
            }, this);
            tags.Clear();
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

        private void Load(string path)
        {
            try
            {
                var level = Level.Deserialize(File.ReadAllText(path));
                level.Name = Path.GetFileNameWithoutExtension(path);
                LoadLevel(level);
                currentLevelPath = path;
            }
            catch (Exception ex)
            {
                Dialog.ShowMessage($"Failed to open level file {path}\n\n{ex.ToString()}".ToRed());
            }
        }

        private void LoadLevel(Level l)
        {
            if(l != null)
            {
                LoadLevelInternal(l);
            }
            else
            {
                Dialog.ShowRichTextInput("Choose Width".ToConsoleString(), (val) =>
                {
                    if(int.TryParse(val.ToString(), out int w) == false)
                    {
                        Dialog.ShowMessage("Invalid width: "+val);
                    }
                    else
                    {
                        Dialog.ShowRichTextInput("Choose height".ToConsoleString(), (heightVal) =>
                        {
                            if (int.TryParse(heightVal.ToString(), out int h) == false)
                            {
                                Dialog.ShowMessage("Invalid height: " + heightVal);
                            }
                            else
                            {
                                LoadLevelInternal(new Level() { Width = w, Height = h });
                            }

                        }, initialValue: Level.DefaultWidth.ToString().ToConsoleString());
                    }

                }, initialValue: Level.DefaultWidth.ToString().ToConsoleString());
            }

        }

        private void LoadLevelInternal(Level l)
        {
            this.currentLevel = l;
            ConfigueEditor();
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
            var ret = currentLevel ?? new Level();
            ret.Items.Clear();

            for (var x = 0; x < innerEditor.Bitmap.Width; x++)
            {
                for (var y = 0; y < innerEditor.Bitmap.Height; y++)
                {
                    var pixel = innerEditor.Bitmap.GetPixel(x, y);
                    if (pixel.Value.HasValue == false)
                    {
                        continue;
                    }

                    if(pixel.Value.Value.Value == ' ' && pixel.Value.Value.BackgroundColor == ConsoleString.DefaultBackgroundColor)
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
