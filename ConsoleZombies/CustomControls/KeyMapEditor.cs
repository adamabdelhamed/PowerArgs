using PowerArgs.Cli;
using System;
using PowerArgs;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleZombies
{
    public class KeyMapEditor : ConsolePanel
    {
        public KeyMapEditor(KeyMap map)
        {
            var stackPanel = Add(new StackPanel() { Margin = 1 }).Fill();
            this.Width = 50;
            foreach(var prop in map.GetType().GetProperties().Where(p => p.PropertyType == typeof(ConsoleKey)))
            {
                var myProp = prop;
                var fieldPanel = stackPanel.Add(new ConsolePanel() { Height=1 }).FillHoriontally();
                var fieldLabel = fieldPanel.Add(new Label() {  Text = prop.Name.ToYellow() });
                var fieldValue = fieldPanel.Add(new Label() { X = 30, CanFocus=true, Text = ((ConsoleKey)myProp.GetValue(map)).ToString().ToYellow() });

                fieldValue.KeyInputReceived.SubscribeForLifetime((key) =>
                {
                    myProp.SetValue(map, key.Key);
                    fieldValue.Text = ((ConsoleKey)myProp.GetValue(map)).ToString().ToYellow();
                }, this.LifetimeManager);

                this.Height += 2;
            }
        }
    }
}
