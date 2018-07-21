using System;
using System.Linq;
using System.Reflection;

namespace PowerArgs.Cli
{
    /// <summary>
    /// A wrapper for an IObservableObject that provides deep change notification along
    /// with deep undo / redo. 
    /// 
    /// This is useful if you are building an editing experience for a structured document.
    /// 
    /// To use this effectively you should model your document as an IObjectObservable. Nested
    /// properties and collections are supported as long as the nested properties are themselves
    /// IObservableObjects or IObservableCollections. Any properties that are not observable
    /// will not have their changes reflected by the Changed event nor will thier changes be
    /// modified by Undo or Redo operations.
    /// </summary>
    public class ObservableDocument : Lifetime
    {
        private class PropertyAssignmentAction : IUndoRedoAction
        {
            private object target;
            private PropertyInfo property;
            private object previousValue;
            private object newValue;

            public PropertyAssignmentAction(object target, PropertyInfo property, object previousValue)
            {
                this.target = target;
                this.property = property;
                this.previousValue = previousValue;
                this.newValue = property.GetValue(target);
            }

            public void Do() { } // it already happened, nothing to do
            public void Undo() => property.SetValue(target, previousValue);
            public void Redo() => property.SetValue(target, newValue);
        }

        private class AddToCollectionAction : IUndoRedoAction
        {
            private IObservableCollection target;
            private object added;
            private int index;

            public AddToCollectionAction(IObservableCollection target, object added, int index)
            {
                this.target = target;
                this.added = added;
                this.index = index;
            }

            public void Do() { }
            public void Undo() => target.RemoveAt(index);
            public void Redo() => target.Insert(index, added);
        }

        private class RemovedFromCollectionAction : IUndoRedoAction
        {
            private IObservableCollection target;
            private object removed;
            private int index;

            public RemovedFromCollectionAction(IObservableCollection target, object removed, int index)
            {
                this.target = target;
                this.removed = removed;
                this.index = index;
            }

            public void Do() { }
            public void Undo() => target.Insert(index, removed);
            public void Redo() => target.RemoveAt(index);
        }

        private class AssignedToindexCollectionAction : IUndoRedoAction
        {
            private IObservableCollection target;
            private object oldValue;
            private object newValue;
            private int index;

            public AssignedToindexCollectionAction(IObservableCollection target, object oldValue, object newValue, int index)
            {
                this.target = target;
                this.oldValue = oldValue;
                this.newValue = newValue;
                this.index = index;
            }

            public void Do() { }
            public void Undo() => target[index] = oldValue;
            public void Redo() => target[index] = newValue;
        }
        private class TrackedObservable
        {
            public string Path { get; set; }
            public object PropertyValue { get; set; }
            public override string ToString() => Path;
        }

        /// <summary>
        /// An event that fires when any change is detected in the document. This could be a 
        /// property change on the root object or some nested observable. It could also be an
        /// add, remove ,or index assigment on a property that is an IObservableCollection.
        /// </summary>
        public Event Changed { get; private set; } = new Event();

        private bool undoRedoPending = false;

        /// <summary>
        /// Undoes the last change that was detected
        /// </summary>
        /// <returns>true if there was a previous change to undo, false otherwise</returns>
        public bool Undo()
        {
            undoRedoPending = true;
            return undoRedoStack.Undo();
        }

        /// <summary>
        /// Redoes the last change that was undone
        /// </summary>
        /// <returns>true if there was a change to redo, false otherwise</returns>
        public bool Redo()
        {
            undoRedoPending = true;
            return undoRedoStack.Redo();
        }

        /// <summary>
        /// Clears the undo / redo stack.
        /// </summary>
        public void ClearUndoRedoStack() => undoRedoStack.Clear();

        private IObservableObject root;

        private ObservableCollection<TrackedObservable> trackedObservables = new ObservableCollection<TrackedObservable>();

        private UndoRedoStack undoRedoStack = new UndoRedoStack();

        /// <summary>
        /// Creates an ObservableDocument given a root observable. The ObservableDocument will
        /// immiediately attach to the root object and all child observables.
        /// </summary>
        /// <param name="root">the root object to observe</param>
        public ObservableDocument(IObservableObject root)
        {
            Watch(root);
            this.Changed.SubscribeForLifetime(() => undoRedoPending = false, this);
            this.OnDisposed(()=>
            {
                trackedObservables.Clear();
            });
        }

        private void Watch(IObservableObject obj, string path = "root")
        {
            foreach (var existingTrackedProperty in trackedObservables.Where(o => o.Path.StartsWith(path)).ToList())
            {
                trackedObservables.Remove(existingTrackedProperty);
            }

            var trackedObservable = new TrackedObservable() { PropertyValue = obj, Path = path };
            trackedObservables.Add(trackedObservable);
            var trackingLifetime = trackedObservables.GetMembershipLifetime(trackedObservable);

            foreach (var property in obj.GetType().GetProperties())
            {
                if (obj is IObservableCollection && property.Name == "Item") { continue; }
                bool isSyncing = true;
                obj.SynchronizeForLifetime(property.Name, () =>
                {
                    var myProp = property;
                    var val = myProp.GetValue(obj);
                    if (val is IObservableObject)
                    {
                        Watch(val as IObservableObject, path + "." + myProp.Name);
                    }

                    if (isSyncing)
                    {
                        isSyncing = false;
                    }
                    else
                    {
                        if (undoRedoPending == false)
                        {
                            undoRedoStack.Do(new PropertyAssignmentAction(obj, myProp, obj.GetPrevious(myProp.Name)));
                        }
                       Changed.Fire();
                    }
                }, trackingLifetime);
            }

            if (obj is IObservableCollection)
            {
                var collection = obj as IObservableCollection;
                collection
                    .WhereAs<IObservableObject>()
                    .ForEach(child => Watch(child as IObservableObject, path + "[" + Guid.NewGuid() + "]"));
       
                collection.Added.SubscribeForLifetime((o) =>
                {
                    if (undoRedoPending == false)
                    {
                        undoRedoStack.Do(new AddToCollectionAction(collection, o, collection.LastModifiedIndex));
                    }

                    if (o is IObservableObject)
                    {
                        Watch(o as IObservableObject, path + "[" + Guid.NewGuid() + "]");
                    }
                    Changed.Fire();
                }, trackingLifetime);

                collection.Removed.SubscribeForLifetime((o) =>
                {
                    if (undoRedoPending == false)
                    {
                        undoRedoStack.Do(new RemovedFromCollectionAction(collection, o, collection.LastModifiedIndex));
                    }
                    if (o is IObservableObject)
                    {
                        var tracked = trackedObservables.Where(to => to.PropertyValue == o).Single();
                        foreach (var related in trackedObservables.Where(t => t.Path.StartsWith(tracked.Path)).ToList())
                        {
                            trackedObservables.Remove(related);
                        }
                    }
                    Changed.Fire();
                }, trackingLifetime);

                collection.AssignedToIndex.SubscribeForLifetime((args) => throw new NotSupportedException("Index assignments are not supported by observable documents"), trackingLifetime);
            }
        }
    }
}
