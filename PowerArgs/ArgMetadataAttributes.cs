using System;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Linq;
namespace PowerArgs
{
    /// <summary>
    /// Enum used to specify how user errors (ArgExceptions) should be handled by the parser.
    /// </summary>
    public enum ArgExceptionPolicy
    {
        /// <summary>
        /// The default, PowerArgs will throw these exceptions for your program to handle.
        /// </summary>
        DontHandleExceptions,
        /// <summary>
        /// PowerArgs will print the user friendly error message as well as the auto-generated usage documentation
        /// for the program.
        /// </summary>
        StandardExceptionHandling,
    }

    /// <summary>
    /// Use this attrbiute to opt into standard error handling of user input errors.  
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ArgExceptionBehavior : Attribute
    {
        /// <summary>
        /// The policy to use, defaults to DontHandleExceptions.
        /// </summary>
        public ArgExceptionPolicy Policy { get; private set; }

        /// <summary>
        /// Optionally show the TYPE column in the auto generated usage.  Defaults to true.
        /// </summary>
        public bool ShowTypeColumn { get; set; }

        /// <summary>
        /// Optionally show the POSITION column in the auto generated usage.  Defaults to true.
        /// </summary>
        public bool ShowPositionColumn { get; set; }

        /// <summary>
        /// Optionally override the ExeName.  You need to do this in unit tests.  In a real console app the
        /// value will be detected automatically if you leave this as null.
        /// </summary>
        public string ExeName { get; set; }

        /// <summary>
        /// Creates a new ArgExceptionBehavior attributes with the given policy.
        /// </summary>
        /// <param name="policy">The policy to use, defaults to DontHandleExceptions.</param>
        public ArgExceptionBehavior(ArgExceptionPolicy policy = ArgExceptionPolicy.DontHandleExceptions)
        {
            this.Policy = policy;
            this.ShowTypeColumn = true;
            this.ShowPositionColumn = true;
            this.ExeName = null;
        }
    }

    /// <summary>
    /// The attribute used when you want to create an arg reviver. You should put this on public static methods 
    /// that take 2 string parameters (the first represents the name of the property, the second represents the string value
    /// and the return type is the type that you are reviving (or converting) the string into.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ArgReviverAttribute : Attribute
    {
        /// <summary>
        /// Creates a new ArgReviverAttribute
        /// </summary>
        public ArgReviverAttribute()
        {
        }
    }

    /// <summary>
    /// Use this attribute to annotate methods that represent your program's actions.  
    /// </summary>
    public class ArgActionMethod : Attribute { }

    /// <summary>
    /// Use this attribute if your action implementation methods are defined in a type other than the 
    /// type being passed to Args.ParseAction() or Args.InvokeAction().
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ArgActionType : Attribute
    {
        /// <summary>
        /// The type that implements your action methods.
        /// </summary>
        public Type ActionType { get; private set; }

        /// <summary>
        /// Creates a new ArgActionType attribute given the type that contains the action implementation.
        /// </summary>
        /// <param name="t">The type that implements your action methods.</param>
        public ArgActionType(Type t)
        {
            this.ActionType = t;
        }
    }

    /// <summary>
    /// Obsolete - Don't use this.  Both the -name value and /name:value styles are now both supported automatically.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    [Obsolete("The ArgStyle attribute is obsolete.  Both styles are now supported automatically")]
    public class ArgStyleAttribute : Attribute
    {
        /// <summary>
        /// Obsolete - Don't use this.  Both the -name value and /name:value styles are now both supported automatically.
        /// </summary>
        public ArgStyle Style { get; set; }

        /// <summary>
        /// Obsolete - Don't use this.  Both the -name value and /name:value styles are now both supported automatically.
        /// </summary>
        /// <param name="style">obsolete</param>
        public ArgStyleAttribute(ArgStyle style = ArgStyle.PowerShell)
        {
            this.Style = style;
        }
    }

    /// <summary>
    /// Use this argument on your class, property, or action method to enforce case sensitivity.  By default,
    /// case is ignored.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method)]
    public class ArgIgnoreCase : Attribute
    {
        /// <summary>
        /// Flag to set whether or not case is ignored.
        /// </summary>
        public bool IgnoreCase { get; set; }

        /// <summary>
        /// Create a new ArgIgnoreCase attribute.
        /// </summary>
        /// <param name="ignore">Whether or not to ignore case</param>
        public ArgIgnoreCase(bool ignore = true)
        {
            IgnoreCase = ignore;
        }
    }

    /// <summary>
    /// Use this argument on your class or property to enforce case sensitivity.  By default,
    /// case is ignored.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class ArgEnforceCase : ArgIgnoreCase
    {
        /// <summary>
        /// Create a new ArgEnforceCase attribute.
        /// </summary>
        public ArgEnforceCase() : base(false) { }
    }

    /// <summary>
    /// An enum to represent argument shortcut policies
    /// </summary>
    public enum ArgShortcutPolicy 
    {
        /// <summary>
        /// Pass this value to the ArgShortcut attribute's constructor to indicate that the given property
        /// does not support a shortcut.
        /// </summary>
        NoShortcut 
    }

    /// <summary>
    /// Use this attribute to override the shortcut that PowerArgs automatically assigns to each property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class ArgShortcut : Attribute
    {
        private static Dictionary<PropertyInfo, List<string>> KnownShortcuts = new Dictionary<PropertyInfo, List<string>>();
        private static List<Type> RegisteredTypes = new List<Type>();

        private ArgShortcutPolicy? policy;

        /// <summary>
        /// The shortcut for the given property
        /// </summary>
        public string Shortcut { get; set; }

        /// <summary>
        /// Creates a new ArgShortcut attribute with a specified value.
        /// </summary>
        /// <param name="shortcut">The value of the new shortcut.</param>
        public ArgShortcut(string shortcut)
        {
            this.Shortcut = shortcut;
        }

        /// <summary>
        /// Creates a new ArgShortcut using the given policy
        /// </summary>
        /// <param name="policy"></param>
        public ArgShortcut(ArgShortcutPolicy policy)
        {
            if (policy == ArgShortcutPolicy.NoShortcut)
            {
                this.Shortcut = null;
                this.policy = policy;
            }
            else
            {
                throw new InvalidOperationException("ShortcutAssignment '" + policy + "' is not supported in this context.");
            }
        }

        internal static List<string> GetShortcutsInternal(PropertyInfo info)
        {
            if (RegisteredTypes.Contains(info.DeclaringType) == false)
            {
                // Ensures that the shortcuts get registered
                try { Args.Parse(info.DeclaringType); }
                catch (Exception) { }
            }
            if (KnownShortcuts.ContainsKey(info)) return KnownShortcuts[info];
            else return new List<string>();
        }

        internal static void RegisterShortcuts(Type t, List<string> shortcutsSeenSoFar = null)
        {
            RegisteredTypes.Add(t);
            bool isNested = shortcutsSeenSoFar != null;

            shortcutsSeenSoFar = isNested ? shortcutsSeenSoFar : new List<string>();
            var actionProp = ArgAction.GetActionProperty(t);

            foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (prop.Attr<ArgIgnoreAttribute>() != null) continue;
                if (prop.IsActionArgProperty() && actionProp != null) continue;

                var shortcutsForProperty = ArgShortcut.FindShortcutsInternal(prop, shortcutsSeenSoFar);
                if (shortcutsForProperty.Count > 0)
                {
                    shortcutsSeenSoFar.AddRange(shortcutsForProperty);
                    if (KnownShortcuts.ContainsKey(prop) == false)
                    {
                        KnownShortcuts.Add(prop, shortcutsForProperty);
                    }
                    else
                    {
                        KnownShortcuts[prop] = shortcutsForProperty;
                    }
                }
            }

            if (actionProp != null)
            {
                foreach (PropertyInfo prop in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (prop.IsActionArgProperty())
                    {
                        RegisterShortcuts(prop.PropertyType, shortcutsSeenSoFar);
                    }
                }
            }
        }

        private static List<string> FindShortcutsInternal(PropertyInfo info, List<string> knownShortcuts)
        {
            var actionProperty = ArgAction.GetActionProperty(info.DeclaringType);
            if (actionProperty != null && actionProperty.Name == info.Name) return new List<string>();

            var attrs = info.Attrs<ArgShortcut>();

            if (attrs.Count == 0)
            {
                string shortcutVal = "";
                foreach (char c in info.GetArgumentName())
                {
                    shortcutVal += c;
                    if (knownShortcuts.Contains(shortcutVal) == false) return new List<string>{ shortcutVal };
                }
                return new List<string> { shortcutVal };
            }
            else
            {
                List<string> ret = new List<string>();
                bool noShortcut = false;
                foreach (var attr in attrs)
                {
                    if (attr.policy.HasValue && attr.policy.Value == ArgShortcutPolicy.NoShortcut)
                    {
                        noShortcut = true;
                    }

                    if (noShortcut && attr.Shortcut != null)
                    {
                        throw new InvalidArgDefinitionException("You cannot specify a shortcut value and an ArgShortcutPolicy of NoShortcut");
                    }

                    if (attr.Shortcut != null)
                    {
                        if (attr.Shortcut.StartsWith("-")) attr.Shortcut = attr.Shortcut.Substring(1);
                        else if (attr.Shortcut.StartsWith("/")) attr.Shortcut = attr.Shortcut.Substring(1);
                    }

                    if (attr.Shortcut != null)
                    {
                        ret.Add(attr.Shortcut);
                    }
                }

                return ret;
            }
        }
    }

    #region Usage

    /// <summary>
    /// Use this attribute if you want PowerArgs to ignore a property completely.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ArgIgnoreAttribute : Attribute { }

    /// <summary>
    /// Use this attribute if you want users to be able to specify an argument without specifying the name, 
    /// but rather by it's position on the command line.  All positioned arguments must come before any named arguments.
    /// Zero '0' represents the first position.  If you are using the Action framework then subcommands must start at
    /// position 1.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ArgPosition : Attribute
    {
        /// <summary>
        /// The expected position of this argument
        /// </summary>
        public int Position { get; private set; }

        /// <summary>
        /// Creates a new ArgPositionAttribute
        /// </summary>
        /// <param name="pos">The expected position of this argument</param>
        public ArgPosition(int pos)
        {
            if (pos < 0) throw new InvalidArgDefinitionException("Positioned args must be >= 0");
            this.Position = pos;
        }
    }

    /// <summary>
    /// Use this attribute to describe your argument property.  This will show up in the auto generated
    /// usage documentation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class ArgDescription : Attribute
    {
        /// <summary>
        /// A brief description of your argument property.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Creates a new ArgDescription attribute.
        /// </summary>
        /// <param name="description">A brief description of your argument property.</param>
        public ArgDescription(string description)
        {
            this.Description = description;
        }
    }

    /// <summary>
    /// Use this attribute to provide an example of how to use your program.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = true)]
    public class ArgExample : Attribute
    {
        /// <summary>
        /// The example command line.
        /// </summary>
        public string Example { get; private set; }

        /// <summary>
        /// A brief description of what this example does.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Creates a new ArgExample class
        /// </summary>
        /// <param name="example">The example command line.</param>
        /// <param name="description">A brief description of what this example does.</param>
        public ArgExample(string example, string description)
        {
            this.Example = example;
            this.Description = description;
        }
    } 

    #endregion

    #region Hooks

    /// <summary>
    /// An abstract class that you can implement if you want to hook into various parts of the
    /// parsing pipeline.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class)]
    public abstract class ArgHook : Attribute
    {
        /// <summary>
        /// Context that is passed to your hook.  Different parts of the context will be available
        /// depending on which part of the pipeline you're hooking into.
        /// </summary>
        public class HookContext
        {
            /// <summary>
            /// The current property being operating on.  This is not available during BeforePopulateProperties or
            /// AfterPopulateProperties.
            /// </summary>
            public PropertyInfo Property { get; set; }

            /// <summary>
            /// The command line arguments that were passed to the Args class.  This is always available and you
            /// can modify it in the BeforeParse hook at your own risk.
            /// </summary>
            public string[] CmdLineArgs;

            /// <summary>
            /// The string value that was specified for the current property.  This will align with the value of ArgHook.Property.
            /// 
            /// This is not available during BeforePopulateProperties or
            /// AfterPopulateProperties.
            /// 
            /// </summary>
            public string ArgumentValue;

            /// <summary>
            /// This is the instance of your argument class.  The amount that it is populated will depend on how far along in the pipeline
            /// the parser is.
            /// </summary>
            public object Args { get; set; }

            /// <summary>
            /// This is the value of the current property after it has been converted into its proper .NET type.  It is only available
            /// to the AfterPopulateProperty hook.
            /// </summary>
            public object RevivedProperty;

            /// <summary>
            /// The raw parser data.  This is not available to the BeforeParse hook.  It may be useful for you to look at, but you should rarely have to change the values.  
            /// Modify the content of this at your own risk.
            /// </summary>
            public ParseResult ParserData { get; set; }
        }

        /// <summary>
        /// The priority of the BeforeParse hook.  Higher numbers execute first.
        /// </summary>
        public int BeforeParsePriority { get; set; }

        /// <summary>
        /// The priority of the BeforePopulateProperties hook.  Higher numbers execute first.
        /// </summary>
        public int BeforePopulatePropertiesPriority { get; set; }

        /// <summary>
        /// The priority of the BeforePopulateProperty hook.  Higher numbers execute first.
        /// </summary>
        public int BeforePopulatePropertyPriority { get; set; }

        /// <summary>
        /// The priority of the AfterPopulateProperty hook.  Higher numbers execute first.
        /// </summary>
        public int AfterPopulatePropertyPriority { get; set; }

        /// <summary>
        /// The priority of the AfterPopulateProperties hook.  Higher numbers execute first.
        /// </summary>
        public int AfterPopulatePropertiesPriority { get; set; }


        /// <summary>
        /// This hook is called before the parser ever looks at the command line.  You can do some preprocessing of the 
        /// raw string arguments here.
        /// </summary>
        /// <param name="context">An object that has useful context.  See the documentation of each property for information about when those properties are populated.</param>
        public virtual void BeforeParse(HookContext context) { }

        /// <summary>
        /// This hook is called before the arguments defined in a class are populated.  For actions (or sub commands) this hook will
        /// get called once for the main class and once for the specified action.
        /// </summary>
        /// <param name="context">An object that has useful context.  See the documentation of each property for information about when those properties are populated.</param>
        public virtual void BeforePopulateProperties(HookContext context) { }

        /// <summary>
        /// This hook is called before an argument is transformed from a string into a proper type and validated.
        /// </summary>
        /// <param name="context">An object that has useful context.  See the documentation of each property for information about when those properties are populated.</param>
        public virtual void BeforePopulateProperty(HookContext context) { }

        /// <summary>
        /// This hook is called after an argument is transformed from a string into a proper type and validated.
        /// </summary>
        /// <param name="context">An object that has useful context.  See the documentation of each property for information about when those properties are populated.</param>
        public virtual void AfterPopulateProperty(HookContext context) { }


        /// <summary>
        /// This hook is called after the arguments defined in a class are populated.  For actions (or sub commands) this hook will
        /// get called once for the main class and once for the specified action.
        /// </summary>
        /// <param name="context">An object that has useful context.  See the documentation of each property for information about when those properties are populated.</param>
        public virtual void AfterPopulateProperties(HookContext context) { }
    }

    /// <summary>
    /// An interface used to implement custom saving and loading of persistent (sticky) args.
    /// </summary>
    public interface IStickyArgPersistenceProvider
    {
        /// <summary>
        /// This method is called when it is time to save the sticky args.
        /// </summary>
        /// <param name="stickyArgs">The names and values of the arguments to save.</param>
        /// <param name="pathInfo">The string that was passed to the StickyArg attribue (usually a file path).</param>
        void Save(Dictionary<string, string> stickyArgs, string pathInfo);
        /// <summary>
        /// This method is called when it is time to load the sticky args.
        /// </summary>
        /// <param name="pathInfo">The string that was passed to the StickyArg attribue (usually a file path).</param>
        /// <returns>The loaded sticky args.</returns>
        Dictionary<string, string> Load(string pathInfo);
    }

    /// <summary>
    /// An attribute you can put on a type in order to override how StickyArg properties are saved and loaded.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class StickyArgPersistence : Attribute
    {
        private Type persistenceProviderType;
        private IStickyArgPersistenceProvider _persistenceProvider;

        /// <summary>
        /// Gets the provider that will be used to save and load sticky args.
        /// </summary>
        public IStickyArgPersistenceProvider PersistenceProvider
        {
            get
            {
                if (_persistenceProvider != null) return _persistenceProvider;

                if (persistenceProviderType.GetInterfaces().Contains(typeof(IStickyArgPersistenceProvider)) == false)
                {
                    throw new InvalidArgDefinitionException("The given type does not implement '" + typeof(IStickyArgPersistenceProvider).Name + "'");
                }

                _persistenceProvider = (IStickyArgPersistenceProvider)Activator.CreateInstance(persistenceProviderType);
                return _persistenceProvider;
            }
        }

        /// <summary>
        /// Creates a new StickyArgPersistence attribute given the type of the persistence provider.
        /// </summary>
        /// <param name="persistenceProviderType">The type that implements IStickyArgPersistenceProvider and defines a default constructor.</param>
        public StickyArgPersistence(Type persistenceProviderType)
        {
            this.persistenceProviderType = persistenceProviderType;
        }
    }

    internal class DefaultStickyArgPersistenceProvider : IStickyArgPersistenceProvider
    {
        public void Save(Dictionary<string, string> stickyArgs, string pathInfo)
        {
            pathInfo = pathInfo ?? DefaultFilePath;

            if (Directory.Exists(Path.GetDirectoryName(pathInfo)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(pathInfo));
            }

            var lines = (from k in stickyArgs.Keys select k + "=" + stickyArgs[k]).ToArray();
            File.WriteAllLines(pathInfo, lines);
        }

        public Dictionary<string, string> Load(string pathInfo)
        {
            pathInfo = pathInfo ?? DefaultFilePath;

            if (Directory.Exists(Path.GetDirectoryName(pathInfo)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(pathInfo));
            }

            Dictionary<string, string> ret = new Dictionary<string, string>();
            if (File.Exists(pathInfo) == false) return ret;

            foreach (var line in File.ReadAllLines(pathInfo))
            {
                int separator = line.IndexOf("=");
                if (separator < 0 || line.Trim().StartsWith("#")) continue;

                string key = line.Substring(0, separator).Trim();
                string val = separator == line.Length - 1 ? "" : line.Substring(separator + 1).Trim();

                ret.Add(key, val);
            }

            return ret;
        }

        private string DefaultFilePath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                   "PowerArgs",
                   Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location)) + ".txt";
            }
        }
    }

    /// <summary>
    /// A useful arg hook that will store the last used value for an argument and repeat it the next time.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class StickyArg : ArgHook
    {
        private static Lazy<IStickyArgPersistenceProvider> defaultPersistenceProvider = new Lazy<IStickyArgPersistenceProvider>(() => { return new DefaultStickyArgPersistenceProvider(); });

        private string file;
        private Dictionary<string, string> stickyArgs;
        private IStickyArgPersistenceProvider userSpecifiedPersistenceProvider;

        /// <summary>
        /// Marks a property as a sticky arg.  Use the default location to store sticky arguments (AppData/Roaming/PowerArgs/EXE_NAME.txt)
        /// </summary>
        public StickyArg()
        {
            Init(null);
        }

        /// <summary>
        /// Marks a property as a sticky arg.  Use the provided location to store sticky arguments (AppData/Roaming/PowerArgs/EXE_NAME.txt)
        /// </summary>
        public StickyArg(string file)
        {
            Init(file);
        }

        private void Init(string file)
        {
            BeforePopulatePropertyPriority = 10;
            stickyArgs = new Dictionary<string, string>();
            this.file = file;
        }

        /// <summary>
        /// If the user didn't specify a value on the command line then the StickyArg will try to load the last used
        /// value.
        /// </summary>
        /// <param name="Context">Used to see if the property was specified.</param>
        public override void BeforePopulateProperty(HookContext Context)
        {
            if (Context.ArgumentValue == null)
            {
                if (userSpecifiedPersistenceProvider == null && Context.Property.DeclaringType.HasAttr<StickyArgPersistence>())
                {
                    userSpecifiedPersistenceProvider = Context.Property.DeclaringType.Attr<StickyArgPersistence>().PersistenceProvider;
                }

                Context.ArgumentValue = GetStickyArg(Context.Property.GetArgumentName());
            }
        }

        /// <summary>
        /// If the given property has a non null value then that value is persisted for the next use.
        /// </summary>
        /// <param name="Context">Used to see if the property was specified.</param>
        public override void AfterPopulateProperty(HookContext Context)
        {
            if (Context.ArgumentValue != null)
            {
                if (userSpecifiedPersistenceProvider == null && Context.Property.DeclaringType.HasAttr<StickyArgPersistence>())
                {
                    userSpecifiedPersistenceProvider = Context.Property.DeclaringType.Attr<StickyArgPersistence>().PersistenceProvider;
                }

                SetStickyArg(Context.Property.GetArgumentName(), Context.ArgumentValue);
            }
        }

        private string GetStickyArg(string name)
        {
            Load();
            string ret = null;
            if (stickyArgs.TryGetValue(name, out ret) == false) return null;
            return ret;
        }

        private void SetStickyArg(string name, string value)
        {
            Load();
            if (stickyArgs.ContainsKey(name))
            {
                stickyArgs[name] = value;
            }
            else
            {
                stickyArgs.Add(name, value);
            }
            Save();
        }

        private void Load()
        {
            var provider = userSpecifiedPersistenceProvider ?? defaultPersistenceProvider.Value;
            stickyArgs = provider.Load(file);
        }

        private void Save()
        {
            var provider = userSpecifiedPersistenceProvider ?? defaultPersistenceProvider.Value;
            provider.Save(stickyArgs, file);
        }
    }

    /// <summary>
    /// Use this attribute to set the default value for a parameter.  Note that this only
    /// works for simple types since only compile time constants can be passed to an attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultValueAttribute : ArgHook
    {
        /// <summary>
        /// The default value that was specified on the attribute.  Note that the value will get
        /// converted to a string and then fed into the parser to be revived.
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// Creates a new DefaultValueAttribute with the given value.  Note that the value will get
        /// converted to a string and then fed into the parser to be revived.
        /// </summary>
        /// <param name="value">The default value for the property</param>
        public DefaultValueAttribute(object value)
        {
            Value = value;
        }

        /// <summary>
        /// Before the property is revived and validated, if the user didn't specify a value, 
        /// then substitue the default value.
        /// 
        /// </summary>
        /// <param name="Context"></param>
        public override void BeforePopulateProperty(HookContext Context)
        {
            if (Context.ArgumentValue == null) Context.ArgumentValue = Value.ToString();
        }
    }

    #endregion
}
