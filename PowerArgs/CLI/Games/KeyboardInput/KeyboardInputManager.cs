using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;

namespace PowerArgs.Games
{
    public class KeyMap
    {
        public Dictionary<ConsoleKey, Action> KeyboardMap { get; set; } = new Dictionary<ConsoleKey, Action>();
        public Dictionary<ConsoleKey, Action> ShiftKeyboardMap { get; set; } = new Dictionary<ConsoleKey, Action>();
        public Dictionary<ConsoleKey, Action> AltKeyboardMap { get; set; } = new Dictionary<ConsoleKey, Action>();
    }

    public class KeyboardInputManager : ObservableObject
    {
        public ConsoleApp App { get; private set; }
        public KeyMap KeyMap { get => Get<KeyMap>(); set => Set(value); }

        private Lifetime currentMappingLifetime;
 

        public KeyboardInputManager(ConsoleApp app)
        {
            this.App = app;
            this.KeyMap = new KeyMap();
            this.SubscribeForLifetime(nameof(KeyMap), UpdateKeyboardMappings, this);
        }

        private void UpdateKeyboardMappings()
        {
            if (currentMappingLifetime != null)
            {
                currentMappingLifetime.Dispose();
            }

            currentMappingLifetime = new Lifetime();

            foreach (var key in KeyMap.KeyboardMap.Keys)
            {
                App.FocusManager.GlobalKeyHandlers.PushForLifetime(key, null, KeyMap.KeyboardMap[key], currentMappingLifetime);
            }

            foreach (var key in KeyMap.ShiftKeyboardMap.Keys)
            {
                App.FocusManager.GlobalKeyHandlers.PushForLifetime(key, ConsoleModifiers.Shift, KeyMap.ShiftKeyboardMap[key], currentMappingLifetime);
            }

            foreach (var key in KeyMap.AltKeyboardMap.Keys)
            {
                App.FocusManager.GlobalKeyHandlers.PushForLifetime(key, ConsoleModifiers.Alt, KeyMap.AltKeyboardMap[key], currentMappingLifetime);
            }
        }
    }
}
