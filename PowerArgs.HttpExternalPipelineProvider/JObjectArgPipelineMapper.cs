using Newtonsoft.Json.Linq;
using PowerArgs.Preview;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs.Preview
{
    public class JObjectArgPipelineMapper : IArgPipelineObjectMapper
    {
        public object MapIncompatibleDirectTargets(Type desiredType, object incompatibleObject)
        {
            if(incompatibleObject is JObject == false)
            {
                throw new ArgumentException("Type not supported, must be JObject: " + incompatibleObject.GetType().FullName);
            }

            var revivedValue = Activator.CreateInstance(desiredType);

            int mappedProps = 0;
            foreach (JProperty prop in ((JObject)(incompatibleObject)).Properties())
            {
                if (prop.Type == JTokenType.Object || prop.Type == JTokenType.Array || prop.Type == JTokenType.Bytes || prop.Type == JTokenType.Constructor || prop.Type == JTokenType.Comment)
                {
                    continue;
                }

                var targetProp = desiredType.GetProperty(prop.Name);

                if (targetProp == null || targetProp.GetSetMethod() == null)
                {
                    continue;
                }

                if (prop.Type == JTokenType.Null)
                {
                    targetProp.SetValue(revivedValue, null, null);
                    mappedProps++;
                }
                else if (ArgRevivers.CanRevive(targetProp.PropertyType))
                {
                    PowerLogger.LogLine("Mapped JObject property to " + desiredType.Name + "." + targetProp.Name);
                    var revivedPropValue = ArgRevivers.Revive(targetProp.PropertyType, prop.Name, prop.Value.ToString());
                    targetProp.SetValue(revivedValue, revivedPropValue, null);
                    mappedProps++;
                }
            }

            if (mappedProps == 0)
            {
                throw new ArgException("Could not map the given object to type: " + desiredType);
            }

            return revivedValue;
        }


        public bool TryExtractObjectPropertyIntoCommandLineArgument(object o, CommandLineArgument argument, string[] staticMappings, out string commandLineKey, out string commandLineValue)
        {
            if (o is JObject == false)
            {
                throw new ArgumentException("Type not supported, must be JObject: " + o.GetType().FullName);
            }

            var dynamicObject = (IDictionary<string,JProperty>)o;

            var mapCandidates = argument.Aliases.Union(staticMappings).Select(a => a.Replace("-", ""));

            var mapSuccessCandidate = (from member in dynamicObject.Keys
                                       where mapCandidates.Contains(member, StringComparer.Create(System.Globalization.CultureInfo.InvariantCulture, true))
                                       select member).FirstOrDefault();

            if (mapSuccessCandidate != null)
            {
                commandLineKey = "-" + argument.DefaultAlias;
                commandLineValue = dynamicObject[mapSuccessCandidate] + "";
                return true;
            }
            else
            {
                commandLineKey = null;
                commandLineValue = null;
                return false;
            }
        }
    }
}
