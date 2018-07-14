using PowerArgs;
using PowerArgs.Cli;
using PowerArgs.Games;

namespace LevelEditor
{
    class Program
    {
        [ArgDefaultValue(@"C:\Users\adamab\source\repos\PowerArgs\LevelEditor\bin\Debug\Level1.lvl")]
        [ArgPosition(0)]
        [ArgExistingFile]
        public string InitialFile { get; set; }

        static void Main(string[] args) => Args.InvokeMain<Program>(args);

        public void Main()
        {
            var app = new ConsoleApp();
            var editorControl = new EditorWrapper(InitialFile);

            app.LayoutRoot.Add(editorControl).Fill();
            app.Start().Wait();
        }
    }

    class EditorWrapper :  PowerArgs.Games.LevelEditor
    {
        public EditorWrapper(string initialFile) : base(initialFile) { }
        protected override Level Deserialize(string text) => LevelExporter.FromCSharp(text);
        protected override string Serialize(Level level) => LevelExporter.ToCSharp(level);
    }
}
