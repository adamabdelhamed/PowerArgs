namespace PowerArgs.Cli.Physics
{
    public abstract class SpacialElementFunction : TimeFunction
    {
        public SpacialElement Element { get; set; }
        public SpacialElementFunction(SpacialElement target)
        {
            this.Element = target;

            if (target.Lifetime.IsExpired)
            {
                return;
            }

            if (target.IsAttached())
            {
                Time.CurrentTime.QueueAction(() => { Time.CurrentTime.Add(this); });
            }
            else
            {
                target.Added.SubscribeForLifetime(() => { Time.CurrentTime.Add(this); }, target.Lifetime.LifetimeManager);
            }


            Element.Lifetime.LifetimeManager.Manage(this.Lifetime.Dispose);
        }
    }
}
