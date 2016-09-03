using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A class that provides context to code that is building a visual tree from markup and binding it to a view model
    /// </summary>
    public class ParserContext
    {
        /// <summary>
        /// The current xml element being parsed
        /// </summary>
        public XmlElement CurrentElement { get; set; }

        /// <summary>
        /// The root xml element being parsed
        /// </summary>
        public XmlElement RootElement { get; set; }

        /// <summary>
        /// The root view model
        /// </summary>
        public object RootViewModel { get; set; }

        /// <summary>
        /// the view model to apply to the current control
        /// </summary>
        public object CurrentViewModel { get; set; }

        /// <summary>
        /// The current control being processed in the visual treee
        /// </summary>
        public ConsoleControl CurrentControl { get; set; }
    }

    internal class MarkupParser
    {
        private class ViewModelBinding
        {
            private object latestValue;

            public ViewModelBinding(ConsoleControl control, PropertyInfo controlProperty, ObservableObject viewModel, string observablePropertyName)
            {
                var viewModelObservableProperty = viewModel.GetType().GetProperty(observablePropertyName);

                if (viewModelObservableProperty.PropertyType != controlProperty.PropertyType &&
                    viewModelObservableProperty.PropertyType.IsSubclassOf(controlProperty.PropertyType) == false &&
                    viewModelObservableProperty.PropertyType.GetInterfaces().Contains(controlProperty.PropertyType) == false)
                {
                    throw new InvalidOperationException($"ViewModel property '{viewModel.GetType().FullName}.{observablePropertyName}' of type {viewModelObservableProperty.PropertyType.FullName} is not compatible with control property '{controlProperty.DeclaringType.FullName}.{controlProperty.Name}' of type {controlProperty.PropertyType.FullName} ");
                }

                viewModel.SynchronizeForLifetime(observablePropertyName, () =>
                {
                    var newValue = viewModelObservableProperty.GetValue(viewModel);
                    if (newValue == latestValue) return;
                    latestValue = newValue;
                    controlProperty.SetValue(control, newValue);
                }, control.LifetimeManager);

                control.SubscribeForLifetime(controlProperty.Name, () =>
                {
                    var newValue = controlProperty.GetValue(control);
                    if (newValue == latestValue) return;
                    latestValue = newValue;
                    viewModelObservableProperty.SetValue(viewModel, newValue);
                }, control.LifetimeManager);
            }
        }
        
        public static ConsoleApp Parse(string markup, object viewModel = null)
        {
            ParserContext context = new ParserContext()
            {
                RootElement = new XmlElement(markup),
                RootViewModel = viewModel,
                CurrentViewModel = viewModel,
            };

            var app = ParseApp(context);

            return app;
        }

        private static ConsoleApp ParseApp(ParserContext context)
        {
            context.CurrentElement = context.RootElement;

            var x = context.CurrentElement.Attribute<int>("X");
            var y = context.CurrentElement.Attribute<int>("Y");
            var w = context.CurrentElement.Attribute<int>("Width");
            var h = context.CurrentElement.Attribute<int>("Height");

            ConsoleApp app;
            if (x.HasValue | y.HasValue | w.HasValue | h.HasValue)
            {
                var xV = x.HasValue ? x.Value : 0;
                var yV = y.HasValue ? y.Value : 0;
                var wV = w.HasValue ? w.Value : ConsoleProvider.Current.BufferWidth - xV;
                var hV = h.HasValue ? h.Value : ConsoleProvider.Current.WindowHeight - yV;
                app = new ConsoleApp(xV, yV, wV, hV);
            }
            else
            {
                app = new ConsoleApp();
            }

            ParsePanel(context, app.LayoutRoot);
            return app;
        }

        public static ConsolePanel ParsePanel(string markup, object viewModel = null)
        {
            ParserContext context = new ParserContext()
            {
                RootElement = new XmlElement(markup),
                RootViewModel = viewModel,
                CurrentViewModel = viewModel,
            };

            var panel = new ConsolePanel();
            ParsePanel(context, panel);
            return panel;
        }

        private static void ParsePanel(ParserContext context, ConsolePanel panel)
        {
            var myElement = context.CurrentElement;
            context.CurrentControl = panel;
            ParseControlAttributes(panel, context);

            foreach (var childElement in context.CurrentElement.Elements)
            {
                context.CurrentElement = childElement;
                var childControl = CreateControl(context);
                context.CurrentControl = childControl;
                panel.Add(childControl);

                if (childControl is ConsolePanel)
                {
                    ParsePanel(context, childControl as ConsolePanel);
                }
                else
                {
                    ParseControlAttributes(childControl, context);
                }
            }

            context.CurrentElement = myElement;
            context.CurrentControl = panel;
        }

        private static ConsoleControl CreateControl(ParserContext context)
        {
            var controlTypeName = context.CurrentElement.Name;
            var controlFullTypeName = $"PowerArgs.Cli.{controlTypeName}";
            var controlType = typeof(Args).Assembly.GetType(controlFullTypeName);
            var control = (ConsoleControl)Activator.CreateInstance(controlType);
            return control;
        }

        private static void ParseControlAttributes(ConsoleControl control, ParserContext context)
        {
            foreach (var attribute in context.CurrentElement.Attributes)
            {
                var extensions = control.GetType().Attrs<MarkupExtensionAttribute>().Where(a => a.AttributeName == attribute.Name);

                if(extensions.Count() > 1)
                {
                    throw new InvalidOperationException("More than 1 extension registered for property "+attribute.Name);
                }
                else if(extensions.Count() == 1)
                {
                    extensions.First().Processor.Process(context);
                }
                else
                {
                    var propertyInfo = control.GetType().GetProperty(attribute.Name);
                    var methodInfo = control.GetType().GetMethod(attribute.Name);
                    if (propertyInfo.GetGetMethod() == null || propertyInfo.GetSetMethod() == null)
                    {
                        if (propertyInfo.PropertyType == typeof(Event))
                        {
                            // there is special handling for events downstream so let this through
                        }
                        else
                        {
                            throw new InvalidOperationException($"Property {control.GetType().FullName}.{attribute.Name} does not have a public getter and setter");
                        }
                    }

                    SetPropertyFromTextValue(context, control, propertyInfo, attribute.Value);
                }
            }
        }

        private static void SetPropertyFromTextValue(ParserContext context, ConsoleControl control, PropertyInfo property, string textValue)
        {
            bool isObservable = textValue.StartsWith("{") && textValue.EndsWith("}");

            if (isObservable)
            {
                var observablePropertyName = textValue.Substring(1, textValue.Length - 2);
                var viewModelObservable = context.CurrentViewModel as ObservableObject;
                if (viewModelObservable == null) throw new InvalidOperationException("View model is not observable");
                new ViewModelBinding(control, property, viewModelObservable, observablePropertyName);
            }
            else if(property.HasAttr<MarkupPropertyAttribute>())
            {
                property.Attr<MarkupPropertyAttribute>().Processor.Process(context);
            }
            else if (property.PropertyType == typeof(string))
            {
                property.SetValue(control, textValue);
            }
            else if (property.PropertyType == typeof(ConsoleString))
            {
                property.SetValue(control, new ConsoleString(textValue));
            }
            else if (property.PropertyType.IsEnum)
            {
                var enumVal = Enum.Parse(property.PropertyType, textValue);
                property.SetValue(control, enumVal);
            }
            else if (property.PropertyType == typeof(Event))
            {
                Event ev = property.GetValue(control) as Event;
                var target = context.CurrentViewModel;
                Action handler = () =>
                {
                    var method = target.GetType().GetMethod(textValue, new Type[0]);
                    if (method != null)
                    {
                        method.Invoke(target, new object[0]);
                    }
                    else
                    {
                        var action = target.GetType().GetProperty(textValue);
                        if (action == null || action.PropertyType != typeof(Action))
                        {
                            throw new InvalidOperationException("Not a method or action");
                        }

                        ((Action)action.GetValue(target)).Invoke();
                    }
                };

                ev.SubscribeForLifetime(handler, control.LifetimeManager);
            }
            else
            {
                var parseMethod = property.PropertyType.GetMethod("Parse", new Type[] { typeof(string) });
                var parsed = parseMethod.Invoke(null, new object[] { textValue });
                property.SetValue(control, parsed);
            }
        }
    }
}
