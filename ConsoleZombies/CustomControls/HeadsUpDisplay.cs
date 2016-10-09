using PowerArgs.Cli;
using PowerArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs.Cli.Physics;

namespace ConsoleZombies
{
    public class HeadsUpDisplayViewModel : ObservableObject
    {
        public ConsoleApp Application { get; set; }

        public MainCharacter MainCharacter
        {
            get
            {
                return Get<MainCharacter>();
            }
            set
            {
                Set(value);
                value.SynchronizeForLifetime(nameof(value.HealthPoints), ()=> 
                {
                    Application.QueueAction(() => 
                    {
                        this.HPValue = value.HealthPoints;
                    });
                }, value.LifetimeManager);

                value.SynchronizeForLifetime(nameof(value.AimMode), () =>
                 {
                     Application.QueueAction(() =>
                     {
                         this.AimMode = new ConsoleString(value.AimMode.ToString(), value.AimMode == ConsoleZombies.AimMode.Auto ? ConsoleColor.White : ConsoleColor.Cyan);
                     });
                 }, value.LifetimeManager);

                value.Inventory.SynchronizeForLifetime(nameof(value.Inventory.PrimaryWeapon), () =>
                {
                    Application.QueueAction(() =>
                    {
                        this.PrimaryWeaponName = value.Inventory.PrimaryWeapon.GetType().Name.ToWhite();
                    });

                    value.Inventory.PrimaryWeapon.SynchronizeForLifetime(nameof(value.Inventory.PrimaryWeapon.AmmoAmount), () =>
                    {
                        Application.QueueAction(() =>
                        {
                            this.PrimaryWeaponAmount = value.Inventory.PrimaryWeapon.AmmoAmount;
                        });
                    }, value.Inventory.GetPropertyValueLifetime(nameof(value.Inventory.PrimaryWeapon)).LifetimeManager);

                  
                }, value.LifetimeManager);

                value.Inventory.SynchronizeForLifetime(nameof(value.Inventory.ExplosiveWeapon), () =>
                {
                    Application.QueueAction(() =>
                    {
                        this.ExplosiveWeaponName = value.Inventory.ExplosiveWeapon.GetType().Name.ToWhite();
                    });

                    value.Inventory.ExplosiveWeapon.SynchronizeForLifetime(nameof(value.Inventory.ExplosiveWeapon.AmmoAmount), () =>
                    {
                        Application.QueueAction(() =>
                        {
                            this.ExplosiveAmmount = value.Inventory.ExplosiveWeapon.AmmoAmount;
                        });
                    }, value.Inventory.GetPropertyValueLifetime(nameof(value.Inventory.ExplosiveWeapon)).LifetimeManager);


                }, value.LifetimeManager);
            }
        }

        public ConsoleString HPDisplayValue{ get { return FormatHPValue(HPValue); } }

        public float HPValue { get { return Get<float>(); } set { Set(value); FirePropertyChanged(nameof(HPDisplayValue)); } }
        public ConsoleString PrimaryWeaponName { get { return Get<ConsoleString>(); } set { Set(value);} }

        public ConsoleString ExplosiveWeaponName { get { return Get<ConsoleString>(); } set { Set(value);} }

        public int PrimaryWeaponAmount { get { return Get<int>(); } set { Set(value); } }

        public int ExplosiveAmmount { get { return Get<int>(); } set { Set(value); } }

        public ConsoleString AimMode { get { return Get<ConsoleString>(); } set { Set(value); } }


        private ConsoleString FormatHPValue(float hp)
        {
            hp = (int)Math.Ceiling(hp);

            if (hp >= 60)
            {
                return (hp + "").ToGreen();
            }
            else if (hp >= 30)
            {
                return (hp + "").ToYellow();
            }
            else
            {
                return (hp + "").ToRed();
            }
        }
    }

    public class WeaponRow
    {
        public ConsoleString Weapon { get; set; }
        public ConsoleString Trigger { get; set; }
        public ConsoleString Amount { get; set; }
    }

    public class HeadsUpDisplay : ConsolePanel
    {
        public HeadsUpDisplayViewModel ViewModel { get; private set; }

        public Scene GameScene { get; private set; }
        private GameApp gameApp;
        public HeadsUpDisplay(GameApp app, Scene gameScene)
        {
            this.gameApp = app;
            this.ViewModel = new HeadsUpDisplayViewModel() { Application = app};
            AddedToVisualTree.SubscribeForLifetime(() => { this.ViewModel.Application = this.Application; }, this.LifetimeManager);

            this.GameScene = gameScene;
            this.Height = 7;

            var topPanel = Add(new StackPanel() { Orientation = Orientation.Horizontal, Height = 6 }).FillHoriontally();
            var leftPanel = topPanel.Add(new StackPanel() { Orientation = Orientation.Vertical, Width = 12 }).FillVertically();
            var middleGrid = topPanel.Add(new Grid(new List<object>()
            {
                new WeaponRow { Weapon = "".ToWhite(), Trigger= "".ToWhite(), Amount="".ToWhite() },
                new WeaponRow { Weapon = "".ToWhite(), Trigger= "".ToWhite(), Amount="".ToWhite() },
            }) { Gutter=0, ShowEndIfComplete=false, CanFocus=false, Width = 35 }).FillVertically();
            var rightPanel = topPanel.Add(new ConsolePanel() { IsVisible=false, Width = 31, Background = ConsoleColor.White }).FillVertically(padding: new Thickness(0,0,1,1));
            var bottomPanel = Add(new StackPanel() { Orientation = Orientation.Horizontal, Margin=2, Height = 1 }).FillHoriontally().DockToBottom();

            var hpLabel = leftPanel.Add(new Label() { Text = "HP".ToGray() }).FillHoriontally();
            var hpValue = leftPanel.Add(new Label() { Text = ConsoleString.Empty }).FillHoriontally();
            var spacer = leftPanel.Add(new Label() { Text = ConsoleString.Empty });
            var aimLabel = leftPanel.Add(new Label() { Text = "AIM [A]".ToGray() }).FillHoriontally();
            var aimValue = leftPanel.Add(new Label() { Text = "Auto".ToWhite() }).FillHoriontally();

            middleGrid.VisibleColumns[0].ColumnDisplayName = new ConsoleString(middleGrid.VisibleColumns[0].ColumnDisplayName.ToString(), ConsoleColor.Gray);
            middleGrid.VisibleColumns[1].ColumnDisplayName = new ConsoleString(middleGrid.VisibleColumns[1].ColumnDisplayName.ToString(), ConsoleColor.Gray);
            middleGrid.VisibleColumns[2].ColumnDisplayName = new ConsoleString(middleGrid.VisibleColumns[2].ColumnDisplayName.ToString(), ConsoleColor.Gray);

            middleGrid.VisibleColumns[0].OverflowBehavior = new TruncateOverflowBehavior() { TruncationText = "", ColumnWidth = 15 };
            middleGrid.VisibleColumns[1].OverflowBehavior = new TruncateOverflowBehavior() { TruncationText = "", ColumnWidth = 10 };
            middleGrid.VisibleColumns[2].OverflowBehavior = new TruncateOverflowBehavior() { TruncationText = "", ColumnWidth = 10};
            var menuLabel = bottomPanel.Add(new Label() { Text = "Menu [M]".ToYellow() });
            var pauseLabel = bottomPanel.Add(new Label() { Text = "Pause [P]".ToYellow() });
            var quitLabel = bottomPanel.Add(new Label() { Text = "Quit [ESC]".ToYellow() });

            var messageLabel = rightPanel.Add(new Label() { Mode = LabelRenderMode.MultiLineSmartWrap, Background = ConsoleColor.White, Text = "Here is a message that can be a few words long".ToBlack(bg: ConsoleColor.White)}).Fill(padding: new Thickness(1, 1, 1, 1));

            new ViewModelBinding(hpValue, hpValue.GetType().GetProperty(nameof(hpValue.Text)), ViewModel, nameof(ViewModel.HPDisplayValue));

            ViewModel.SynchronizeForLifetime(nameof(ViewModel.PrimaryWeaponName), () =>
            {
                var row = ((WeaponRow)(middleGrid.DataSource as MemoryDataSource).Items[0]);
                row.Weapon = ViewModel.PrimaryWeaponName;
            }, this.LifetimeManager);

            ViewModel.SynchronizeForLifetime(nameof(ViewModel.PrimaryWeaponAmount), () =>
            {
                var row = ((WeaponRow)(middleGrid.DataSource as MemoryDataSource).Items[0]);
                row.Amount = FormatAmmoAmmount(ViewModel.PrimaryWeaponAmount);
            }, this.LifetimeManager);


            ViewModel.SynchronizeForLifetime(nameof(ViewModel.ExplosiveWeaponName), () =>
            {
                var row = ((WeaponRow)(middleGrid.DataSource as MemoryDataSource).Items[1]);
                row.Weapon = ViewModel.ExplosiveWeaponName;
            }, this.LifetimeManager);

            ViewModel.SynchronizeForLifetime(nameof(ViewModel.ExplosiveAmmount), () =>
            {
                var row = ((WeaponRow)(middleGrid.DataSource as MemoryDataSource).Items[1]);
                row.Amount = FormatAmmoAmmount(ViewModel.ExplosiveAmmount);
            }, this.LifetimeManager);

            ViewModel.SynchronizeForLifetime(nameof(ViewModel.AimMode), () =>
             {
                 aimValue.Text = ViewModel.AimMode;
             }, this.LifetimeManager);

            BindToScene();
        }

        private ConsoleString FormatAmmoAmmount(int amount)
        {
            if(amount > 10)
            {
                return (amount + "").ToGreen();
            }
            else if(amount > 0 )
            {
                return (amount + "").ToYellow();
            }
            else
            {
                return "empty".ToRed();
            }
        }

        private void BindToScene()
        {
 
        }
    }
}
