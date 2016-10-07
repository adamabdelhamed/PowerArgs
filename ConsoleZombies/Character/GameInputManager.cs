using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;

namespace ConsoleZombies
{
    public class GameInputManager
    {
        public Scene Scene { get; private set; }
        public ConsoleApp App { get; private set; }
        public Dictionary<ConsoleKey, Action> KeyboardMap { get; private set; }
        public Dictionary<ConsoleKey, Action> ShiftKeyboardMap { get; private set; }
        public Dictionary<ConsoleKey, Action> AltKeyboardMap { get; private set; }

        private Lifetime currentMappingLifetime;

        public GameInputManager(Scene scene, ConsoleApp app)
        {
            this.Scene = scene;
            this.App = app;
            this.KeyboardMap = new Dictionary<ConsoleKey, Action>();
            this.ShiftKeyboardMap = new Dictionary<ConsoleKey, Action>();
            this.AltKeyboardMap = new Dictionary<ConsoleKey, Action>();
        }

        public void InitializeDefaultControls()
        {
            KeyboardMap.Clear();
            AltKeyboardMap.Clear();
            ShiftKeyboardMap.Clear();

            // move and aim
            KeyboardMap.Add(ConsoleKey.UpArrow, MainCharacter.Current.MoveUp);
            KeyboardMap.Add(ConsoleKey.DownArrow, MainCharacter.Current.MoveDown);
            KeyboardMap.Add(ConsoleKey.LeftArrow, MainCharacter.Current.MoveLeft);
            KeyboardMap.Add(ConsoleKey.RightArrow, MainCharacter.Current.MoveRight);
            KeyboardMap.Add(ConsoleKey.A, MainCharacter.Current.StartFreeAim);

            // fire weapons
            KeyboardMap.Add(ConsoleKey.D, MainCharacter.Current.Inventory.Gun.TryFire);
            KeyboardMap.Add(ConsoleKey.F, MainCharacter.Current.Inventory.Gun.TryFire);
            KeyboardMap.Add(ConsoleKey.R, MainCharacter.Current.Inventory.RemoteMineDropper.TryFire);
            KeyboardMap.Add(ConsoleKey.T, MainCharacter.Current.Inventory.TimedMineDropper.TryFire);
            KeyboardMap.Add(ConsoleKey.G, MainCharacter.Current.Inventory.RPGLauncher.TryFire);

            // doors
            KeyboardMap.Add(ConsoleKey.Enter, MainCharacter.Current.TryOpenCloseDoor);

            UpdateKeyboardMappings();
        }

        public void UpdateKeyboardMappings()
        {
            if (currentMappingLifetime != null)
            {
                currentMappingLifetime.Dispose();
            }

            currentMappingLifetime = new Lifetime();

            foreach(var key in KeyboardMap.Keys)
            {
                App.FocusManager.GlobalKeyHandlers.PushForLifetime(key, null, QueueToScene(KeyboardMap[key]), currentMappingLifetime.LifetimeManager);
            }

            foreach (var key in ShiftKeyboardMap.Keys)
            {
                App.FocusManager.GlobalKeyHandlers.PushForLifetime(key, ConsoleModifiers.Shift, QueueToScene(KeyboardMap[key]), currentMappingLifetime.LifetimeManager);
            }

            foreach (var key in AltKeyboardMap.Keys)
            {
                App.FocusManager.GlobalKeyHandlers.PushForLifetime(key, ConsoleModifiers.Alt, QueueToScene(KeyboardMap[key]), currentMappingLifetime.LifetimeManager);
            }
        }


        private Action QueueToScene(Action a)
        {
            return () => { Scene.QueueAction(() =>  {  a(); }); };
        }
    }
}
