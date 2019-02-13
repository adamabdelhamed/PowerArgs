using PowerArgs;
using System;
using System.Linq;

namespace PowerArgs.Cli
{
    /// <summary>
    /// An attribute that tells the form generator to ignore this
    /// property
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class FormIgnoreAttribute : Attribute { }

    /// <summary>
    /// An attribute that tells the form generator to give this
    /// property a read only treatment
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class FormReadOnlyAttribute : Attribute { }

    /// <summary>
    /// An attribute that lets you override the display string 
    /// on a form element
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class FormLabelAttribute : Attribute
    {
        /// <summary>
        /// The label to display on the form element
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Initialized the attribute
        /// </summary>
        /// <param name="label">The label to display on the form element</param>
        public FormLabelAttribute(string label) { this.Label = label; }
    }

    /// <summary>
    /// A class that represents a form element
    /// </summary>
    public class FormElement
    {
        /// <summary>
        /// The label for the form element
        /// </summary>
        public ConsoleString Label { get; set; }
        /// <summary>
        /// The control that renders the form element's value
        /// </summary>
        public ConsoleControl ValueControl { get; set; }
    }

    /// <summary>
    /// Options for configuring a form
    /// </summary>
    public class FormOptions
    {
        /// <summary>
        /// The percentage of the available width to use for labels
        /// </summary>
        public double LabelColumnPercentage { get; set; }

        /// <summary>
        /// The form elements to render
        /// </summary>
        public ObservableCollection<FormElement> Elements { get; private set; } = new ObservableCollection<FormElement>();

        /// <summary>
        /// Autogenerates form options for the given object by reflecting on its properties. All public properties with getters 
        /// and setters will be included in the form unless it has the FormIgnore attribute on it. This method supports strings,
        /// ints, and enums.
        /// 
        /// The form will be configured to two way bind all the form elements to the property values.
        /// </summary>
        /// <param name="o">The object to create form options for</param>
        /// <param name="labelColumnPercentage">the label column percentage to use</param>
        /// <returns></returns>
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
                    (o as IObservableObject)?.SynchronizeForLifetime(property.Name, () =>
                    {
                        var valueRead = property.GetValue(o);
                        if (valueRead is ICanBeAConsoleString)
                        {
                            textBox.Value = (valueRead as ICanBeAConsoleString).ToConsoleString();
                        }
                        else
                        {
                            textBox.Value = (valueRead + "").ToWhite();
                        }
                    }, textBox);
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
                            textBox.Value = property.GetValue(o).ToString().ToWhite();
                        }
                    }, textBox);
                    (o as IObservableObject)?.SynchronizeForLifetime(property.Name, () =>
                    {
                        var valueRead = property.GetValue(o);
                        if (valueRead is ICanBeAConsoleString)
                        {
                            textBox.Value = (valueRead as ICanBeAConsoleString).ToConsoleString();
                        }
                        else
                        {
                            textBox.Value = (valueRead + "").ToConsoleString();
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
                    (o as IObservableObject)?.SynchronizeForLifetime(property.Name, () => enumPicker.SelectedItem = property.GetValue(o), enumPicker);
                    editControl = enumPicker;
                }
                else
                {
                    var value = property.GetValue(o);
                    var valueString = value != null ? value.ToString().ToDarkGray() : "<null>".ToDarkGray();
                    var valueLabel = new Label() { CanFocus = true, Text = valueString + " (read only)".ToDarkGray() };
                    (o as IObservableObject)?.SynchronizeForLifetime(property.Name, () => valueLabel.Text = (property.GetValue(o) + "").ToConsoleString()+" (read only)".ToDarkGray(), valueLabel);

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

    /// <summary>
    /// A control that lets users edit a set of values as in a form
    /// </summary>
    public class Form : ConsolePanel
    {
        /// <summary>
        /// The options that were provided
        /// </summary>
        public FormOptions Options { get; private set; }

        /// <summary>
        /// Creates a form using the given options
        /// </summary>
        /// <param name="options">form options</param>
        public Form(FormOptions options)
        {
            this.Options = options;
            this.AddedToVisualTree.SubscribeForLifetime(InitializeForm, this);

        }

        private void InitializeForm()
        {
            var labelColumn = Add(new StackPanel() { Orientation = Orientation.Vertical });
            var valueColumn = Add(new StackPanel() { Orientation = Orientation.Vertical });

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

            this.Options.Elements.AssignedToIndex.SubscribeForLifetime((assignment) => throw new NotSupportedException("Index assignments not supported in form elements"), this);
        }
    }
}
