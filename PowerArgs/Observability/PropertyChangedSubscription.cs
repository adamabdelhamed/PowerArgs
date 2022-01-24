using System;

namespace PowerArgs
{
    public class PropertyChangedSubscription : Lifetime
    {
        public Action ChangeListener { get; private set; }
        public string PropertyName { get; private set; }

        private Action<PropertyChangedSubscription> unsubscribeCallback;
        public PropertyChangedSubscription(string propertyName, Action changeListener, Action<PropertyChangedSubscription> unsubscribeCallback)
        {
            this.PropertyName = propertyName;
            this.ChangeListener = changeListener;
            this.unsubscribeCallback = unsubscribeCallback;
            this.OnDisposed(Unsubscribe);
        }

        private void Unsubscribe()
        {
            unsubscribeCallback(this);
        }
    }
}
