using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs.Cli;

namespace ArgsTests.CLI.Observability
{
    [TestClass]
    public class ObservabilityTests
    {
        public class SomeObservable : ObservableObject
        {
            public Event SomeEvent { get; private set; } = new Event();
            public Event<string> SomeEventWithAString { get; private set; } = new Event<string>();

            public ObservableCollection<string> Strings { get; private set; } = new ObservableCollection<string>();

            public string Name { get { return Get<string>(); } set { Set(value); } }
        }

        [TestMethod]
        public void SubscribeUnmanagedToProperty()
        {
            var observable = new SomeObservable();

            var triggerCount = 0;

            using (var subscription = observable.SubscribeUnmanaged(nameof(SomeObservable.Name), () => { triggerCount++;  }))
            {
                Assert.AreEqual(0, triggerCount);
                observable.Name = "Some value";
                Assert.AreEqual(1, triggerCount);
            }

            observable.Name = "Some new value";
            Assert.AreEqual(1, triggerCount);
        }

        [TestMethod]
        public void SubscribeForLifetimeToProperty()
        {
            var observable = new SomeObservable();

            var triggerCount = 0;

            using (Lifetime lifetime = new Lifetime())
            {
                observable.SubscribeForLifetime(nameof(SomeObservable.Name), () =>
                {
                    triggerCount++;
                }, lifetime.LifetimeManager);

                Assert.AreEqual(0, triggerCount);
                observable.Name = "Some value";
                Assert.AreEqual(1, triggerCount);
            }

            observable.Name = "Some new value";
            Assert.AreEqual(1, triggerCount);
        }

        [TestMethod]
        public void SubscribeToProperty()
        {
            var observable = new SomeObservable();

            var triggerCount = 0;

            using (var lifetime = new Lifetime())
            {
                using (new AmbientLifetimeScope(lifetime.LifetimeManager))
                {
                    observable.Subscribe(nameof(SomeObservable.Name), () => { triggerCount++; });

                    Assert.AreEqual(0, triggerCount);
                    observable.Name = "Some value";
                    Assert.AreEqual(1, triggerCount);
                }

                try
                {
                    observable.Subscribe(nameof(SomeObservable.Name), () =>{ });
                    Assert.Fail("An exception should have been thrown");
                }
                catch(InvalidOperationException ex)
                {
                    Assert.IsTrue(ex.Message.Contains(nameof(LifetimeManager)));
                }

                // this works because even though the ambient lifetime scope is disposed, the lifetime itself is not.
                observable.Name = "Some new value";
                Assert.AreEqual(2, triggerCount);
            }

            observable.Name = "Some new value again";
            Assert.AreEqual(2, triggerCount);
        }

        [TestMethod]
        public void SubscribeUnmanagedToEvent()
        {
            var observable = new SomeObservable();

            var triggerCount = 0;

            using (var subscription = observable.SomeEvent.SubscribeUnmanaged(() => { triggerCount++; }))
            {
                Assert.AreEqual(0, triggerCount);
                observable.SomeEvent.Fire();
                Assert.AreEqual(1, triggerCount);
            }

            observable.SomeEvent.Fire();
            Assert.AreEqual(1, triggerCount);
        }

        [TestMethod]
        public void SubscribeUnmanagedToEventWithUnsubscribe()
        {
            var observable = new SomeObservable();

            var triggerCount = 0;

            Action handler = () => { triggerCount++; };
            observable.SomeEvent.SubscribeUnmanaged(handler);
            
            Assert.AreEqual(0, triggerCount);
            observable.SomeEvent.Fire();
            Assert.AreEqual(1, triggerCount);

            observable.SomeEvent.Unsubscribe(handler);
            observable.SomeEvent.Fire();
            Assert.AreEqual(1, triggerCount);
        }

        [TestMethod]
        public void SubscribeUnmanagedToEventOfStringWithUnsubscribe()
        {
            var observable = new SomeObservable();

            var triggerCount = 0;

            Action<string> handler = (s) => { triggerCount++; };
            observable.SomeEventWithAString.SubscribeUnmanaged(handler);

            Assert.AreEqual(0, triggerCount);
            observable.SomeEventWithAString.Fire("Foo");
            Assert.AreEqual(1, triggerCount);

            observable.SomeEventWithAString.Unsubscribe(handler);
            observable.SomeEventWithAString.Fire("Foo");
            Assert.AreEqual(1, triggerCount);
        }

        [TestMethod]
        public void SubscribeForLifetimeToEvent()
        {
            var observable = new SomeObservable();

            var triggerCount = 0;

            using (var lifetime = new Lifetime())
            {
                observable.SomeEvent.SubscribeForLifetime(() => { triggerCount++; }, lifetime.LifetimeManager);

                Assert.AreEqual(0, triggerCount);
                observable.SomeEvent.Fire();
                Assert.AreEqual(1, triggerCount);
            }

            observable.SomeEvent.Fire();
            Assert.AreEqual(1, triggerCount);
        }

        [TestMethod]
        public void SubscribeToEvent()
        {
            var observable = new SomeObservable();

            var triggerCount = 0;

            using (var lifetime = new Lifetime())
            {
                using (new AmbientLifetimeScope(lifetime.LifetimeManager))
                {
                    observable.SomeEvent.Subscribe(() => { triggerCount++; });

                    Assert.AreEqual(0, triggerCount);
                    observable.SomeEvent.Fire();
                    Assert.AreEqual(1, triggerCount);
                }

                observable.SomeEvent.Fire();
                Assert.AreEqual(2, triggerCount);
            }

            observable.SomeEvent.Fire();
            Assert.AreEqual(2, triggerCount);
        }

        [TestMethod]
        public void SynchronizeCollection()
        {
            var observable = new SomeObservable();
            int addCalls = 0, removeCalls = 0, changedCalls = 0;

            observable.Strings.Add("a");
            observable.Strings.Add("b");

            using (var lifetime = new Lifetime())
            {
                observable.Strings.SynchronizeForLifetime((s) => { addCalls++; }, (s) => { removeCalls++; }, () => { changedCalls++; }, lifetime.LifetimeManager);

                Assert.AreEqual(2, addCalls);
                Assert.AreEqual(0, removeCalls);
                Assert.AreEqual(1, changedCalls);

                observable.Strings.Add("c");
                Assert.AreEqual(3, addCalls);
                Assert.AreEqual(0, removeCalls);
                Assert.AreEqual(2, changedCalls);

                observable.Strings.Remove("a");
                Assert.AreEqual(3, addCalls);
                Assert.AreEqual(1, removeCalls);
                Assert.AreEqual(3, changedCalls);
            }

            observable.Strings.Add("d");
            observable.Strings.Remove("d");
            Assert.AreEqual(3, addCalls);
            Assert.AreEqual(1, removeCalls);
            Assert.AreEqual(3, changedCalls);
        }
    }
}
