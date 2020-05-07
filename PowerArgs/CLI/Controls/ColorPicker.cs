using System;
using System.Linq;

namespace PowerArgs.Cli
{
    public class ColorPicker : ProtectedConsolePanel
    {
        public RGB Value { get => Get<RGB>(); set => Set(value); }

        public ColorPicker()
        {
            var dropdown = ProtectedPanel.Add(new Dropdown(Enums.GetEnumValues<ConsoleColor>().Select(c => new DialogOption
            {
                DisplayText = c.ToString().ToConsoleString((RGB)c),
                Value = (RGB)c,
                Id = c.ToString()
            }))).Fill();

            dropdown.SubscribeForLifetime(nameof(dropdown.Value), () => this.Value = (RGB)dropdown.Value.Value, this);
            this.SubscribeForLifetime(nameof(Value), () => dropdown.Value = dropdown.Options.Where(o => o.Value.Equals(Value)).Single(), this);
        }
    }
}
