using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerArgs;
using System;

namespace ArgsTests
{

    public class MyDocument : ObservableObject
    {
        public string DocumentName { get => Get<string>(); set => Set(value); }

        public ObservableCollection<int> Numbers { get => Get<ObservableCollection<int>>(); set => Set(value); }
        public ObservableCollection<Person> Customers { get => Get<ObservableCollection<Person>>(); set => Set(value); }
    }

    public class Person : ObservableObject
    {
        public string FirstName { get => Get<string>(); set => Set(value); }

        public Person Mom { get => Get<Person>(); set => Set(value); }

        public ObservableCollection<Person> Friends { get => Get<ObservableCollection<Person>>(); set => Set(value); }
    }

    [TestClass]
    [TestCategory(Categories.Eventing)]
    public class ObservableDocumentTests
    {
        [TestMethod]
        public void ObservableDocumentE2ETest()
        {
            var root = new MyDocument();
            var observableDocument = new ObservableDocument(root);
            var notifyCount = 0;
            var sub = observableDocument.Changed.SubscribeUnmanaged(() => notifyCount++);
            Assert.AreEqual(0, notifyCount);

            var assertIncrement = new Action<Action>((action) =>
            {
                var currentCount = notifyCount;
                action();
                Assert.AreEqual(currentCount + 1, notifyCount);
            });

            var assertNoIncrement = new Action<Action>((action) =>
            {
                var currentCount = notifyCount;
                action();
                Assert.AreEqual(currentCount, notifyCount);
            });

            assertIncrement(() => root.DocumentName = "TheName");
            assertIncrement(() => root.DocumentName = null);

            Assert.AreEqual(null, root.DocumentName);
            assertIncrement(() => observableDocument.Undo());
            Assert.AreEqual("TheName", root.DocumentName);
            assertIncrement(() => observableDocument.Redo());
            Assert.AreEqual(null, root.DocumentName);

            assertIncrement(() => root.DocumentName = "TheName2");

            
            assertNoIncrement(() =>  root.DocumentName = "TheName2"); // duplicate change
            assertIncrement(()=>  root.Numbers = new ObservableCollection<int>());
            assertIncrement(()=> root.Numbers.Add(1));
            assertIncrement(() => root.Numbers.Clear());

            var originalCustomers = new ObservableCollection<Person>();
            assertIncrement(()=> root.Customers = originalCustomers);

            var joe = new Person();
            assertIncrement(() => root.Customers.Add(joe));
            assertIncrement(() => joe.FirstName = "Joe");
            assertIncrement(() => root.Customers.Remove(joe));
            assertNoIncrement(()=> joe.FirstName = "bob"); // removed so this should not trigger a change
            assertIncrement(() => root.Customers = new ObservableCollection<Person>());
            assertNoIncrement(() => originalCustomers.Add(new Person())); // removed so this should not trigger a change
            assertIncrement(() => root.Customers.Add(new Person()));
            assertIncrement(()=> root.Customers[0].Friends = new ObservableCollection<Person>());
            assertIncrement(() => root.Customers[0].Friends.Add(new Person()));
            assertIncrement(() => root.Customers[0].Friends[0].Friends = new ObservableCollection<Person>() { new Person() });
            assertIncrement(() => root.Customers[0].Friends[0].Friends.Add(new Person()));
            
            var leafFriend = root.Customers[0].Friends[0].Friends[0];
            assertNoIncrement(() => root.Customers[0].Friends[0].Friends[0] = leafFriend); // equal change, should be suppressed
            assertIncrement(()=> leafFriend.FirstName = "Yay!");
            assertIncrement(() => leafFriend.Mom = new Person());

            assertIncrement(() => root.Customers.Clear());
            assertNoIncrement(() => leafFriend.FirstName = "Jimbo");

            observableDocument.Dispose();

            assertNoIncrement(() => root.DocumentName = "No update"); // doc was disposed, should be no notification

            Assert.AreEqual(21, notifyCount);
            Console.WriteLine(notifyCount);
        }

        [TestMethod]
        public void ObservableDocumentUndoRedo()
        {
            var root = new MyDocument();
            var observableDocument = new ObservableDocument(root);
            root.Numbers = new ObservableCollection<int>();
            observableDocument.ClearUndoRedoStack();

            var numElements = 10;
            for(var i = 0; i < numElements; i++)
            {
                root.Numbers.Add(i);
            }

            
            while (observableDocument.Undo());
            Assert.AreEqual(0, root.Numbers.Count);
            while (observableDocument.Redo());
            Assert.AreEqual(numElements, root.Numbers.Count);

            for (var i = 0; i < numElements; i++)
            {
                Assert.AreEqual(i, root.Numbers[i]);
            }
        }
    }
}
