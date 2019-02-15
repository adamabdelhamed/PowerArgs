using PowerArgs.Cli;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace PowerArgs.Cli
{
    public abstract class PickerControlOptions<T>
    {
        public ConsoleString PromptMessage { get; set; } = "Choose from the following options".ToConsoleString();
        public List<T> Options { get; set; }

        public Func<T, ConsoleString> DisplayFormatter { get; set; }

        public Action<T> SelectionChanged { get; set; }

        internal abstract T DefaultSelection { get; }

        internal abstract bool HasDefaultSelection { get; }
    }

    public class PickerControlClassOptions<T> : PickerControlOptions<T> where T : class
    {
        public T InitiallySelectedOption { get; set; }

        internal override bool HasDefaultSelection => InitiallySelectedOption != null;

        internal override T DefaultSelection => InitiallySelectedOption;
    }

    public class PickerControlStructOptions<T> : PickerControlOptions<T> where T : struct
    {
        public T? InitiallySelectedOption { get; set; }

        internal override bool HasDefaultSelection => InitiallySelectedOption.HasValue;

        internal override T DefaultSelection => HasDefaultSelection ? InitiallySelectedOption.Value : throw new NotSupportedException($"Check {nameof(HasDefaultSelection)} first");
    }


    public class PickerControl<T> : ConsolePanel
    {
        private Label innerLabel;

        public T SelectedItem { get { return Get<T>(); } set { Set(value); } }

        public PickerControlOptions<T> Options { get; private set; }

        // hack because PowerArgs Pick function requires string Ids
        private Dictionary<string, T> idMap;
        private Dictionary<T, string> reverseIdMap;

        public PickerControl(PickerControlOptions<T> options)
        {
            this.Options = options;

            this.idMap = new Dictionary<string, T>();
            this.reverseIdMap = new Dictionary<T, string>();

            for (var i = 0; i < this.Options.Options.Count; i++)
            {
                idMap.Add(i.ToString(), this.Options.Options[i]);
                reverseIdMap.Add(this.Options.Options[i], i.ToString());
            }

            this.innerLabel = Add(new Label()).Fill();

            // When the selected item changes make sure we update the label
            this.SubscribeForLifetime(nameof(SelectedItem), () =>
            {
                this.innerLabel.Text = FormatItem(this.SelectedItem);
                this.Options.SelectionChanged?.Invoke(this.SelectedItem);

            }, this);

            this.innerLabel.CanFocus = true;

            this.innerLabel.KeyInputReceived.SubscribeForLifetime((key) =>
            {
                if (key.Key == ConsoleKey.Enter)
                {
                    Dialog.ShowMessage(new DialogButtonOptions()
                    {
                        Message = this.Options.PromptMessage,
                        Mode = DialogButtonsPresentationMode.Grid,
                        Options = this.Options.Options.Select(o => new DialogOption() { DisplayText = FormatItem(o), Id = reverseIdMap[o] }).ToList(),
                    }).Then((selectedOption) =>
                    {
                        if (selectedOption != null)
                        {
                            this.SelectedItem = idMap[selectedOption.Id];
                        }
                    });
                }
            }, this);

            if (options.HasDefaultSelection)
            {
                this.SelectedItem = options.DefaultSelection;
            }
        }

        private ConsoleString FormatItem(T item) => this.Options.DisplayFormatter != null ? this.Options.DisplayFormatter(item) : item.ToString().ToCyan();
    }
}