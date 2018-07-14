using PowerArgs.Games;
using Newtonsoft.Json;
using System;
using System.Text;

namespace LevelEditor
{
    public static class LevelExporter
    {
        private const string CodeGenPrefix = "// REQUIRED FOR CODE-GEN - ";

        public static string ToCSharp(Level level)
        {
            var builder = new StringBuilder();
            var indent = "";

            // json comment
            builder.AppendLine(CodeGenPrefix + JsonConvert.SerializeObject(level));

            // usings
            builder.AppendLine($"using System;");
            builder.AppendLine($"using {nameof(PowerArgs)}.{nameof(PowerArgs.Games)};");
            builder.AppendLine($"using System.Collections.Generic;");
            builder.AppendLine();

            // start namespace
            builder.AppendLine("namespace GeneratedLevels\n{");

            // start class
            IncrementIndent(ref indent);
            builder.AppendLine($"{indent}public class {level.Name} : Level");
            builder.AppendLine(indent + "{");
            IncrementIndent(ref indent);

            // start constructor
            builder.AppendLine($"{indent}public {level.Name}()");
            builder.AppendLine(indent + "{");
            IncrementIndent(ref indent);

            //constructor body
            builder.AppendLine($"{indent}this.{nameof(Level.Name)} = \"{level.Name}\";");
            builder.AppendLine($"{indent}this.{nameof(Level.Width)} = {level.Width};");
            builder.AppendLine($"{indent}this.{nameof(Level.Height)} = {level.Height};");

            foreach(var item in level.Items)
            {
                builder.AppendLine($"{indent}this.{nameof(Level.Items)}.Add({CreateItemLiteral(item)});");
            }

            // finish constructor
            DecrementIndent(ref indent);
            builder.AppendLine(indent + "}");

            // finish class
            DecrementIndent(ref indent);
            builder.AppendLine(indent + "}");

            // finish namespace
            DecrementIndent(ref indent);
            builder.AppendLine(indent + "}");

            return builder.ToString();
        }

        private static void IncrementIndent(ref string currentIndent) => currentIndent = currentIndent + "    ";
        private static void DecrementIndent(ref string currentIndent) => currentIndent = currentIndent.Substring(0, currentIndent.Length - "    ".Length);

        internal static Level FromCSharp(string text)
        {
            if(text.StartsWith(CodeGenPrefix) == false)
            {
                throw new FormatException("The given text does not have the required code gen comment at the beginning of the file");
            }

            var json = "";
            for(var i = CodeGenPrefix.Length; i <  text.Length; i++)
            {
                if(text[i] == '\n' || text[i] == '\r')
                {
                    break;
                }
                else
                {
                    json += text[i];
                }
            }

            return JsonConvert.DeserializeObject<Level>(json);
        }

        private static string CreateItemLiteral(LevelItem item)
        {
            var ret = $"new {nameof(LevelItem)}()";
            ret += " { ";

            ret += $"{nameof(LevelItem.X)} = {item.X}, ";
            ret += $"{nameof(LevelItem.Y)} = {item.Y}, ";
            ret += $"{nameof(LevelItem.Width)} = {item.Width}, ";
            ret += $"{nameof(LevelItem.Height)} = {item.Height}, ";


            var symbol = item.Symbol == '\\' ? "\\\\" : item.Symbol == '\'' ? "\\'" : item.Symbol.ToString();
            ret += $"{nameof(LevelItem.Symbol)} = '{symbol}', ";

            if(item.FG.HasValue)
            {
                ret += $"{nameof(LevelItem.FG)} = {nameof(ConsoleColor)}.{item.FG}, ";
            }

            if (item.BG.HasValue)
            {
                ret += $"{nameof(LevelItem.BG)} = {nameof(ConsoleColor)}.{item.BG}, ";
            }

            ret += $"{nameof(LevelItem.Tags)} = new List<string>()" + " {";
            foreach(var tag in item.Tags)
            {
                ret += '"' + tag.Replace("\"", "\\\"") + '"' + ", ";
            }
            ret += " }";

            ret += " }";
            return ret;
        }
    }
}
