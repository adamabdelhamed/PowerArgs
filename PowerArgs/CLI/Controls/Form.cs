using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using PowerArgs;
using PowerArgs.Cli;

namespace PowerArgs.Cli
{
    [AttributeUsage(AttributeTargets.Property)]
    public class FormIgnoreAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class FormReadOnlyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class FormLabelAttribute : Attribute
    {
        public string Label { get; set; }
        public FormLabelAttribute(string label) { this.Label = label; }
    }

    public class FormElement
    {
        public ConsoleString Label { get; set; }
        public ConsoleControl ValueControl { get; set; }
    }

    public class FormOptions
    {
        public double LabelColumnPercentage { get; set; }
        public ObservableCollection<FormElement> Elements { get; set; }

        public static FormOptions FromObject(object o, double labelColumnPercentage = .25)
        {
            var properties = o.GetType().GetProperties().Where(p => p.HasAttr<FormIgnoreAttribute>() == false && p.GetSetMethod() != null && p.GetGetMethod() != null).ToList();

            var ret = new FormOptions()
            {
                Elements = new ObservableCollection<FormElement>(),
                LabelColumnPercentage = labelColumnPercentage,
            };

            foreach (var property in properties)
            {
                ConsoleControl editControl = null;
                if (property.HasAttr<FormReadOnlyAttribute>() == false && property.PropertyType == typeof(string))
                {
                    var value = (string)property.GetValue(o);
                    var textBox = new TextBox() { Foreground = ConsoleColor.White, Value = value == null ? ConsoleString.Empty : value.ToString().ToWhite() };
                    textBox.SynchronizeForLifetime(nameof(textBox.Value), () => property.SetValue(o, textBox.Value.ToString()), textBox);
                    editControl = textBox;
                }
                else if (property.HasAttr<FormReadOnlyAttribute>() == false && property.PropertyType == typeof(int))
                {
                    var value = (int)property.GetValue(o);
                    var textBox = new TextBox() { Foreground = ConsoleColor.White, Value = value.ToString().ToWhite() };
                    textBox.SynchronizeForLifetime(nameof(textBox.Value), () =>
                    {

                        if (textBox.Value.Length > 0 && int.TryParse(textBox.Value.ToString(), out int result))
                        {
                            property.SetValue(o, result);
                        }
                        else if (textBox.Value.Length > 0)
                        {
                            textBox.Value = property.GetValue(o).ToString().ToConsoleString();
                        }
                    }, textBox);

                    textBox.AddedToVisualTree.SubscribeForLifetime(() =>
                    {
                        var previouslyFocusedControl = textBox.Application.FocusManager.FocusedControl;

                        var emptyStringAction = new Action(() =>
                        {
                            if (previouslyFocusedControl == textBox && textBox.Application.FocusManager.FocusedControl != textBox)
                            {
                                if (textBox.Value.Length == 0)
                                {
                                    textBox.Value = "0".ToConsoleString();
                                    property.SetValue(o, 0);
                                }
                            }

                            previouslyFocusedControl = textBox.Application.FocusManager.FocusedControl;

                        });

                        textBox.Application.FocusManager.SubscribeForLifetime(nameof(FocusManager.FocusedControl), emptyStringAction, textBox);
                    }, textBox);

                    editControl = textBox;
                }
                else if (property.HasAttr<FormReadOnlyAttribute>() == false && property.PropertyType.IsEnum)
                {
                    var enumPicker = new PickerControl<object>(new PickerControlClassOptions<object>()
                    {
                        InitiallySelectedOption = property.GetValue(o),
                        Options = Enums.GetEnumValues(property.PropertyType)
                    });
                    enumPicker.SynchronizeForLifetime(nameof(enumPicker.SelectedItem), () => property.SetValue(o, enumPicker.SelectedItem), enumPicker);
                    editControl = enumPicker;
                }
                else
                {
                    var value = property.GetValue(o);
                    var valueString = value != null ? value.ToString().ToDarkGray() : "<null>".ToDarkGray();
                    var valueLabel = new Label() { CanFocus = true, Text = valueString + " (read only)".ToDarkGray() };
                    editControl = valueLabel;
                }

                ret.Elements.Add(new FormElement()
                {
                    Label = property.HasAttr<FormLabelAttribute>() ? property.Attr<FormLabelAttribute>().Label.ToYellow() : property.Name.ToYellow(),
                    ValueControl = editControl
                });
            }

            return ret;
        }
    }

    public class Form : ConsolePanel
    {
        public FormOptions Options { get; set; }
        public Form(FormOptions options)
        {
            this.Options = options;
            this.AddedToVisualTree.SubscribeForLifetime(InitializeForm, this);

        }

        private void InitializeForm()
        {
            var labelColumn = Add(new StackPanel() { Orientation = Orientation.Vertical });
            var valueColumn = Add(new StackPanel() { Orientation = Orientation.Vertical });

            Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.DownArrow, null, () =>
            {
                if (this.Descendents.Contains(Application.FocusManager.FocusedControl))
                {
                    Application.FocusManager.TryMoveFocus();
                }

            }, this);

            Application.FocusManager.GlobalKeyHandlers.PushForLifetime(ConsoleKey.UpArrow, null, () =>
            {
                if (this.Descendents.Contains(Application.FocusManager.FocusedControl))
                {
                    Application.FocusManager.TryMoveFocus(false);
                }

            }, this);

            this.SynchronizeForLifetime(nameof(this.Bounds), () =>
            {
                var labelColumnWidth = (int)Math.Round(this.Width * this.Options.LabelColumnPercentage);
                var valueColumnWidth = (int)Math.Round(this.Width * (1 - this.Options.LabelColumnPercentage));

                while (labelColumnWidth + valueColumnWidth > this.Width)
                {
                    labelColumnWidth--;
                }

                while (labelColumnWidth + valueColumnWidth < this.Width)
                {
                    valueColumnWidth++;
                }

                labelColumn.Width = labelColumnWidth;
                valueColumn.Width = valueColumnWidth;

                labelColumn.Height = this.Height;
                valueColumn.Height = this.Height;

                valueColumn.X = labelColumnWidth;

            }, this);

            foreach (var element in this.Options.Elements)
            {
                labelColumn.Add(new Label() { Height = 1, Text = element.Label }).FillHorizontally();
                element.ValueControl.Height = 1;
                valueColumn.Add(element.ValueControl).FillHorizontally();
            }

            this.Options.Elements.Added.SubscribeForLifetime((addedElement) =>
            {
                var index = this.Options.Elements.IndexOf(addedElement);
                var label = new Label() { Height = 1, Text = addedElement.Label };
                addedElement.ValueControl.Height = 1;
                labelColumn.Controls.Insert(index, label);
                label.FillHorizontally();

                valueColumn.Controls.Insert(index, addedElement.ValueControl);
                addedElement.ValueControl.FillHorizontally();

            }, this);

            this.Options.Elements.Removed.SubscribeForLifetime((removedElement) =>
            {
                var index = valueColumn.Controls.IndexOf(removedElement.ValueControl);
                labelColumn.Controls.RemoveAt(index);
                valueColumn.Controls.RemoveAt(index);
            }, this);
        }
    }
}
