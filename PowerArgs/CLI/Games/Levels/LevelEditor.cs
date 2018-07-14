using PowerArgs;
using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PowerArgs.Games
{
    public abstract class LevelEditor : ConsolePanel
    {
        public const string LevelFileExtension = ".cs";
        private ConsoleBitmapEditor innerEditor;
        private Dictionary<Point, List<string>> tags = new Dictionary<Point, List<string>>();
        private string currentLevelPath;
        private bool hasUnsavedChanges = false;
        private Button saveCommand;

        private Level currentLevel;
        
        public LevelEditor(string initialFile = null)
        {
            currentLevelPath = initialFile;
            ConfigueEditor();
        }

        private void ConfigueEditor()
        {
            if (innerEditor != null)
            {
                this.Controls.Remove(innerEditor);
            }

            innerEditor = Add(new ConsoleBitmapEditor(currentLevel != null ? currentLevel.Width : Level.DefaultWidth, currentLevel != null ? currentLevel.Height : Level.DefaultHeight)).CenterBoth();
            innerEditor.BitmapChanged.SubscribeForLifetime(() => hasUnsavedChanges = true, innerEditor);

            var commandBar = Add(new StackPanel() { Height = 1, Orientation = Orientation.Horizontal }).FillHorizontally().DockToTop();
            var tagBar = Add(new Label()).FillHorizontally().DockToTop(padding: 1);

            innerEditor.CursorMoved.SubscribeForLifetime(() => tagBar.Text = FormatTags(), this);

            innerEditor.CreateStandardButtons().ForEach(b => commandBar.Add(b));
            var newCommand = commandBar.Add(new Button() { Text = "New".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.N, ConsoleModifiers.Alt) });
            var openCommand = commandBar.Add(new Button() { Text = "Open".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.O, ConsoleModifiers.Alt) });
            saveCommand = commandBar.Add(new Button() { Text = "Save".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.S, ConsoleModifiers.Alt) });
            var saveAsCommand = commandBar.Add(new Button() { Text = "Save as".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.A, ConsoleModifiers.Alt) });
            var discardCommand = commandBar.Add(new Button() { Text = "Discard".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.D, ConsoleModifiers.Alt) });
            var tagCommand = commandBar.Add(new Button() { Text = "Tag".ToConsoleString(), Shortcut = new KeyboardShortcut(ConsoleKey.T, ConsoleModifiers.Alt) });

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


            saveCommand.Pressed.SubscribeForLifetime(() =>
            {
                if (currentLevelPath != null)
                {
                    var level = ExtractLevel();
                    var text = Serialize(level);
                    File.WriteAllText(currentLevelPath, text);
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
                    currentLevelPath = val.ToString();
                    saveCommand.Pressed.Fire();
                }, initialValue: currentLevelPath != null ? currentLevelPath.ToConsoleString() : Path.Combine(Environment.CurrentDirectory, "Level1" + LevelFileExtension).ToConsoleString());
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

                var tagsString = string.Join(";", tagsForPosition);

                Dialog.ShowRichTextInput("Add/edit semi-colon delimited tags. Press enter to cmmmit".ToYellow(), (newString) =>
                {
                    var split = newString.ToString().Split(';').ToList();
                    tagsForPosition.Clear();
                    tagsForPosition.AddRange(split);
                },
                initialValue: tagsString.ToConsoleString());
            }, this);
            tags.Clear();
            if (currentLevelPath == null)
            {
                AddedToVisualTree.SubscribeOnce(() => Application.QueueAction(newCommand.Pressed.Fire));
            }
            else
            {
                AddedToVisualTree.SubscribeOnce(() => Application.QueueAction(()=> Load(currentLevelPath)));
            }
        }

        private ConsoleString FormatTags()
        {
            if(tags.ContainsKey(innerEditor.CursorPosition)  == false || tags[innerEditor.CursorPosition] == null || tags[innerEditor.CursorPosition].Count == 0)
            {
                return "No tags".ToRed();
            }
                
            var currentTags = string.Join(";", tags[innerEditor.CursorPosition]);
            var tokenizer = new Tokenizer<Token>();
            tokenizer.Delimiters.Add(";");
            tokenizer.Delimiters.Add(":");
            var reader = new TokenReader<Token>(tokenizer.Tokenize(currentTags));
            var chars = new List<ConsoleCharacter>();
            var expectVal = false;
            while(reader.TryAdvance(out Token t))
            {
                if(t.Value == ";")
                {
                    chars.Add(new ConsoleCharacter(';', ConsoleColor.DarkGray));
                    expectVal = false;
                }
                else if(t.Value == ":")
                {
                    chars.Add(new ConsoleCharacter(':', ConsoleColor.White));
                    expectVal = true;
                }
                else if(expectVal)
                {
                    chars.AddRange(t.Value.ToCyan());
                    expectVal = false;
                }
                else
                {
                    chars.AddRange(t.Value.ToYellow());
                }
            }

            return new ConsoleString(chars);
        }

        protected abstract string Serialize(Level level);
        protected abstract Level Deserialize(string text);

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
                var level = Deserialize(File.ReadAllText(path));
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

                        }, initialValue: Level.DefaultHeight.ToString().ToConsoleString());
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
