using System;

namespace PowerArgs
{
    public class PropertyChangedSubscription : Lifetime
    {
        public ObservableObject Target { get; private set; }
        public Action ChangeListener { get; private set; }
        public string PropertyName { get; private set; }

        private Action<PropertyChangedSubscription, object> unsubscribeCallback;
        public PropertyChangedSubscription(string propertyName, Action changeListener, Action<PropertyChangedSubscription, object> unsubscribeCallback, ObservableObject target)
        {
            this.Target = target;
            this.PropertyName = propertyName;
            this.ChangeListener = changeListener;
            this.unsubscribeCallback = unsubscribeCallback;
        }

        protected override void AfterDispose()
        {
            base.AfterDispose();
            unsubscribeCallback(this, Target);
        }
    }

    public class PropertyChangedSubscriptionWithParam : Lifetime
    {
        public ObservableObject Target { get; private set; }
        public Action<object> ChangeListener { get; private set; }
        public string PropertyName { get; private set; }
        public object Param { get; private set; }
        private Action<PropertyChangedSubscriptionWithParam, object> unsubscribeCallback;
        public PropertyChangedSubscriptionWithParam(string propertyName, Action<object> changeListener, object param, Action<PropertyChangedSubscriptionWithParam, object> unsubscribeCallback, ObservableObject target)
        {
            this.Target = target;
            this.Param = param;
            this.PropertyName = propertyName;
            this.ChangeListener = changeListener;
            this.unsubscribeCallback = unsubscribeCallback;
        }

        protected override void AfterDispose()
        {
            base.AfterDispose();
            unsubscribeCallback(this, Target);
        }
    }
}
