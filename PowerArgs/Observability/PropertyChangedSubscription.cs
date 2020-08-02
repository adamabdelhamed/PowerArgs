using System;

namespace PowerArgs
{
    public class PropertyChangedSubscription : Lifetime
    {
        public Action ChangeListener { get; private set; }
        public string PropertyName { get; private set; }
        public PropertyChangedSubscription(string propertyName, Action changeListener, Action<PropertyChangedSubscription> unsubscribeCallback)
        {
            this.PropertyName = propertyName;
            this.ChangeListener = changeListener;

            this.OnDisposed(() =>
            {
                unsubscribeCallback(this);
            });
        }
    }
}
