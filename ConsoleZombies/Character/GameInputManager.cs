using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;

namespace ConsoleZombies
{
    public class KeyMap : ObservableObject
    {
        public ConsoleKey PrimaryWeaponKey { get { return Get<ConsoleKey>(); } set { Set(value); } }
        public ConsoleKey PrimaryWeaponAlternateKey { get { return Get<ConsoleKey>(); } set { Set(value); } } 
        public ConsoleKey ExplosiveWeaponKey { get { return Get<ConsoleKey>(); } set { Set(value); } } 
        

        public ConsoleKey MoveUpKey { get { return Get<ConsoleKey>(); } set { Set(value); } } 
        public ConsoleKey MoveDownKey { get { return Get<ConsoleKey>(); } set { Set(value); } } 
        public ConsoleKey MoveLeftKey { get { return Get<ConsoleKey>(); } set { Set(value); } } 
        public ConsoleKey MoveRightKey { get { return Get<ConsoleKey>(); } set { Set(value); } }
         


        public ConsoleKey AimToggleKey { get { return Get<ConsoleKey>(); } set { Set(value); } }

        public ConsoleKey TogglePauseKey { get { return Get<ConsoleKey>(); } set { Set(value); } }
        public ConsoleKey MenuKey { get { return Get<ConsoleKey>(); } set { Set(value); } }
        public ConsoleKey InteractKey { get { return Get<ConsoleKey>(); } set { Set(value); } } 

        public KeyMap()
        {
            this.PrimaryWeaponKey = ConsoleKey.D;
            this.PrimaryWeaponAlternateKey = ConsoleKey.F;
            this.ExplosiveWeaponKey = ConsoleKey.G;

            this.MoveUpKey = ConsoleKey.UpArrow;
            this.MoveDownKey = ConsoleKey.DownArrow;
            this.MoveLeftKey = ConsoleKey.LeftArrow;
            this.MoveRightKey = ConsoleKey.RightArrow;

            this.AimToggleKey = ConsoleKey.A;
            this.InteractKey = ConsoleKey.Enter;

            this.MenuKey = ConsoleKey.M;
            this.TogglePauseKey = ConsoleKey.P;
        }  
    }

    public class GameInputManager : ObservableObject
    {
        public Event ReWired { get; private set; } = new Event();
        public Scene Scene { get; private set; }
        public ConsoleApp App { get; private set; }

        private Dictionary<ConsoleKey, Action> keyboardMap;
        private Dictionary<ConsoleKey, Action> shiftKeyboardMap;
        private Dictionary<ConsoleKey, Action> altKeyboardMap;

        private Lifetime currentMappingLifetime;
        public KeyMap KeyMap { get { return Get<KeyMap>(); } private set { Set(value); } }

        public GameInputManager(Scene scene, ConsoleApp app)
        {
            this.Scene = scene;
            this.App = app;
            this.keyboardMap = new Dictionary<ConsoleKey, Action>();
            this.shiftKeyboardMap = new Dictionary<ConsoleKey, Action>();
            this.altKeyboardMap = new Dictionary<ConsoleKey, Action>();
            this.KeyMap = new KeyMap();
        }

        public void SetKeyMap(KeyMap map = null)
        {
            Scene.AssertSceneThread(this.Scene);
            map = map ?? new KeyMap();
            this.KeyMap = map;
            this.KeyMap.SubscribeForLifetime(ObservableObject.AnyProperty, () => { Scene.QueueAction(() => { SetKeyMap(map); }); }, GetPropertyValueLifetime(nameof(this.KeyMap)).LifetimeManager);
            keyboardMap.Clear();
            altKeyboardMap.Clear();
            shiftKeyboardMap.Clear();

            // manage
            keyboardMap.Add(map.TogglePauseKey, ()=> { Scene.TogglePause(); });

            // move and aim
            keyboardMap.Add(map.MoveUpKey, () => { MainCharacter.Current.MoveUp(); });
            keyboardMap.Add(map.MoveDownKey, () => { MainCharacter.Current.MoveDown(); });
            keyboardMap.Add(map.MoveLeftKey, () => { MainCharacter.Current.MoveLeft(); });
            keyboardMap.Add(map.MoveRightKey, () => { MainCharacter.Current.MoveRight(); });
            keyboardMap.Add(map.AimToggleKey, () => { MainCharacter.Current.ToggleFreeAim(); });

            // fire weapons
            keyboardMap.Add(map.PrimaryWeaponKey,()=> { MainCharacter.Current?.Inventory?.PrimaryWeapon?.TryFire(); });
            keyboardMap.Add(map.PrimaryWeaponAlternateKey, () => { MainCharacter.Current?.Inventory?.PrimaryWeapon?.TryFire(); });
            keyboardMap.Add(map.ExplosiveWeaponKey, () => { MainCharacter.Current?.Inventory?.ExplosiveWeapon?.TryFire(); });

            // doors
            keyboardMap.Add(map.InteractKey, () => { MainCharacter.Current.TryInteract(); });

            UpdateKeyboardMappings();
        }

        public void UpdateKeyboardMappings()
        {
            if (currentMappingLifetime != null)
            {
                currentMappingLifetime.Dispose();
            }

            currentMappingLifetime = new Lifetime();

            foreach(var key in keyboardMap.Keys)
            {
                App.FocusManager.GlobalKeyHandlers.PushForLifetime(key, null, key == ConsoleKey.P ? keyboardMap[key] : QueueToScene(keyboardMap[key]), currentMappingLifetime.LifetimeManager);
            }

            foreach (var key in shiftKeyboardMap.Keys)
            {
                App.FocusManager.GlobalKeyHandlers.PushForLifetime(key, ConsoleModifiers.Shift, QueueToScene(shiftKeyboardMap[key]), currentMappingLifetime.LifetimeManager);
            }

            foreach (var key in altKeyboardMap.Keys)
            {
                App.FocusManager.GlobalKeyHandlers.PushForLifetime(key, ConsoleModifiers.Alt, QueueToScene(altKeyboardMap[key]), currentMappingLifetime.LifetimeManager);
            }

            ReWired.Fire();
        }


        private Action QueueToScene(Action a)
        {
            return () => 
            {
                Scene.QueueAction(() =>  {  a(); });
            };
        }
    }
}
