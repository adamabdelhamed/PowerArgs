using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;

namespace ConsoleGames
{
    public class KeyMap
    {
        public Dictionary<ConsoleKey, Action> KeyboardMap { get; set; } = new Dictionary<ConsoleKey, Action>();
        public Dictionary<ConsoleKey, Action> ShiftKeyboardMap { get; set; } = new Dictionary<ConsoleKey, Action>();
        public Dictionary<ConsoleKey, Action> AltKeyboardMap { get; set; } = new Dictionary<ConsoleKey, Action>();
    }

    public class KeyboardInputManager : ObservableObject
    {
        public Event ReWired { get; private set; } = new Event();
        public SpaceTime Scene { get; private set; }
        public GameApp App { get; private set; }

        public KeyMap KeyMap { get; set; } = new KeyMap();

        private Lifetime currentMappingLifetime;
 

        public KeyboardInputManager(SpaceTime scene, GameApp app)
        {
            this.Scene = scene;
            this.App = app;
            this.KeyMap = new KeyMap();
        }

        public void UpdateKeyboardMappings()
        {
            if (currentMappingLifetime != null)
            {
                currentMappingLifetime.Dispose();
            }

            currentMappingLifetime = new Lifetime();

            foreach (var key in KeyMap.KeyboardMap.Keys)
            {
                App.FocusManager.GlobalKeyHandlers.PushForLifetime(key, null, QueueToScene(KeyMap.KeyboardMap[key]), currentMappingLifetime.LifetimeManager);
            }

            foreach (var key in KeyMap.ShiftKeyboardMap.Keys)
            {
                App.FocusManager.GlobalKeyHandlers.PushForLifetime(key, ConsoleModifiers.Shift, QueueToScene(KeyMap.ShiftKeyboardMap[key]), currentMappingLifetime.LifetimeManager);
            }

            foreach (var key in KeyMap.AltKeyboardMap.Keys)
            {
                App.FocusManager.GlobalKeyHandlers.PushForLifetime(key, ConsoleModifiers.Alt, QueueToScene(KeyMap.AltKeyboardMap[key]), currentMappingLifetime.LifetimeManager);
            }

            ReWired.Fire();
        }

        private Action QueueToScene(Action a)
        {
            return () =>
            {
                Scene.QueueAction(() => { a(); });
            };
        }
    }
}
