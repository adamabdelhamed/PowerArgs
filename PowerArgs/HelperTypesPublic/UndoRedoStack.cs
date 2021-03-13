using System;
using System.Collections.Generic;

namespace PowerArgs
{
    /// <summary>
    /// An interface for an undoable action
    /// </summary>
    public interface IUndoRedoAction
    {
        /// <summary>
        /// Do the action for the first time
        /// </summary>
        void Do();
        /// <summary>
        /// Undo the action
        /// </summary>
        void Undo();
        /// <summary>
        /// Redo the action
        /// </summary>
        void Redo();
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class TransientUndoRedoAction : Attribute { }

    /// <summary>
    /// A class that models the standard undo / redo pattern found in many applications
    /// </summary>
    public class UndoRedoStack
    {
        private Stack<IUndoRedoAction> undoStack;
        private Stack<IUndoRedoAction> redoStack;

        public Event OnUndoRedoAction { get; private set; } = new Event();
        public Event OnEmptyUndoStack { get; private set; } = new Event();

        /// <summary>
        /// Gets the elements currently in the undo stack
        /// </summary>
        public IEnumerable<IUndoRedoAction> UndoElements
        {
            get
            {
                return undoStack.ToArray();
            }
        }

        /// <summary>
        /// Initializes the undo redo stack
        /// </summary>
        public UndoRedoStack()
        {
            undoStack = new Stack<IUndoRedoAction>();
            redoStack = new Stack<IUndoRedoAction>();
        }

        /// <summary>
        /// Do the given action for the first time.  This method will call the Do() method on he action.
        /// </summary>
        /// <param name="action">The action to do.  The Do() method will be called</param>
        public void Do(IUndoRedoAction action)
        {
            action.Do();
            Done(action);
        }

        public void Done(IUndoRedoAction action)
        {
            undoStack.Push(action);
            redoStack.Clear();
            OnUndoRedoAction.Fire();
        }

        /// <summary>
        /// Undoes the most recently done (or redone) action
        /// </summary>
        /// <returns>true if there was something to undo, false otherwise</returns>
        public bool Undo()
        {
            if (undoStack.Count == 0) return false;

            var toUndo = undoStack.Pop();
            toUndo.Undo();
            redoStack.Push(toUndo);
            OnUndoRedoAction.Fire();

            if(toUndo.GetType().HasAttr<TransientUndoRedoAction>())
            {
                Undo();
            }

            if(undoStack.None())
            {
                OnEmptyUndoStack.Fire();
            }

            return true;
        }

        /// <summary>
        /// Redoes the las thing that was undone.
        /// </summary>
        /// <returns>true if there was something to redo, false otherwise</returns>
        public bool Redo()
        {
            if (redoStack.Count == 0) return false;
            var toRedo = redoStack.Pop();
            toRedo.Redo();
            undoStack.Push(toRedo);
            OnUndoRedoAction.Fire();

            if (redoStack.Count > 0 && redoStack.Peek().GetType().HasAttr<TransientUndoRedoAction>())
            {
                Redo();
            }
            return true;
        }

        /// <summary>
        /// Clears both the undo and redo stacks
        /// </summary>
        public void Clear()
        {
            redoStack.Clear();
            undoStack.Clear();
        }
    }
}
