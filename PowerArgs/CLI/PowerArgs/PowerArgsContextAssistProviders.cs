using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerArgs.Cli
{
    internal class PowerArgsMultiContextAssistProvider : MultiContextAssistProvider
    {
        public CommandLineArgumentsDefinition Definition { get; private set; }

        private List<IContextAssistProvider> standardProviders;

        public PowerArgsMultiContextAssistProvider(CommandLineArgumentsDefinition definition)
        {
            this.Definition = definition;
            standardProviders = new List<IContextAssistProvider>();
            standardProviders.Add(new EnumAssistant(definition));
            standardProviders.Add(new ActionAssistant(definition));
        }

        public override bool CanAssist(RichCommandLineContext context)
        {
            Providers.Clear();
            Providers.AddRange(standardProviders);

            CommandLineArgument targetArgument = null;

            if (context.PreviousNonWhitespaceToken != null && context.PreviousNonWhitespaceToken.Value.StartsWith("-"))
            {
                var candidate = context.PreviousNonWhitespaceToken.Value.Substring(1);
                targetArgument = (from a in Definition.AllGlobalAndActionArguments where a.IsMatch(candidate) select a).SingleOrDefault();
            }

            if (targetArgument != null)
            {
                foreach (var assistant in targetArgument.Metadata.Metas<ArgContextualAssistant>())
                {
                    var dynamicProvider = assistant.GetContextAssistProvider(Definition);
                    Providers.Add(dynamicProvider);
                }
            }

            foreach(var provider in Providers)
            {
                if (provider is PowerArgsContextAwareAssistant)
                {
                    (provider as PowerArgsContextAwareAssistant).TargetArgument = targetArgument;
                }
            }

            var ret = base.CanAssist(context);

            return ret;
        }
    }

    internal abstract class PowerArgsContextAwareAssistant : ContextAssistPicker
    {
        public CommandLineArgumentsDefinition Definition { get; private set; }
        public CommandLineArgument TargetArgument { get; set; }

        public PowerArgsContextAwareAssistant(CommandLineArgumentsDefinition definition)
        {
            this.Definition = definition;
        }
    }

    internal class EnumAssistant : PowerArgsContextAwareAssistant
    {
        public EnumAssistant(CommandLineArgumentsDefinition definition) : base(definition) { }

        public override bool CanAssist(RichCommandLineContext context)
        {
            if (TargetArgument != null && TargetArgument.ArgumentType.IsEnum)
            {
                Options.Clear();
                Options.AddRange(Enum.GetNames(TargetArgument.ArgumentType).Select(name => ContextAssistSearchResult.FromString(name)));
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    internal class ActionAssistant : PowerArgsContextAwareAssistant
    {
        public ActionAssistant(CommandLineArgumentsDefinition definition) : base(definition) { }

        public override bool CanAssist(RichCommandLineContext context)
        {
            if ( context.CurrentTokenIndex == 0 && Definition.Actions.Count > 0)
            {
                Options.Clear();
                Options.AddRange(Definition.Actions.Select(a => ContextAssistSearchResult.FromString(a.DefaultAlias)));
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
