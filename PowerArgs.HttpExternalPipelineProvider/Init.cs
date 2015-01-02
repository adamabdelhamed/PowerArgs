using Newtonsoft.Json.Linq;
using PowerArgs.Preview;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs.Preview
{
    internal static class Init
    {
        private static object initLock = new object();
        private static bool done = false;
        public static void InitIfNotAlreadyDone()
        {
            lock(initLock)
            {
                if (done) return;
                InitImpl();
                done = true;
            }
        }

        private static void InitImpl()
        {
            ArgPipelineObjectMapper.CurrentMapper = new JObjectArgPipelineMapper();

            PipelineOutputFormatter.RegisterFormatter(typeof(JObject), FuncPipelineOutputFormatter.Create((obj) =>
            {
                JObject jObj = (JObject)obj;
                ConsoleTableBuilder builder = new ConsoleTableBuilder();
                List<ConsoleString> headers = new List<ConsoleString>() { new ConsoleString("PROPERTY", ConsoleColor.Yellow), new ConsoleString("VALUE", ConsoleColor.Yellow) };
                List<List<ConsoleString>> rows = new List<List<ConsoleString>>();
                foreach (var prop in jObj.Properties())
                {
                    rows.Add(new List<ConsoleString>() { new ConsoleString(prop.Name, ConsoleColor.Gray), new ConsoleString("" + prop.Value, ConsoleColor.Green) });
                }

                var jObjRet = builder.FormatAsTable(headers, rows);
                jObjRet = new ConsoleString("Pipeline output of type JObject: \n") + jObjRet;
                return jObjRet;
            }));
        }
    }
}
