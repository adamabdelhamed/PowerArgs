namespace PowerArgs.Cli
{
    public class Border : ProtectedConsolePanel
    {
        private ConsolePanel Content { get; set; }

        public Border()
        {
            using (Unlock())
            {
                this.Content = this.Add(new ConsolePanel()).Fill(padding: new Thickness(2, 2, 1, 1));
            }
        }

        public T SetContent<T>(T content) where T : ConsoleControl
        {
            Content.Controls.Clear();
            Content.Add(content);
            return content;
        }
    }
}
