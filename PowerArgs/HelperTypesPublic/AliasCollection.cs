using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    /// <summary>
    /// This class tracks the command line aliases for a CommandLineArgument and a CommandLineAction.
    /// It combines the aliases that have been retrieved from the ArgShortcut attibute and any additional
    /// aliases that may have been added to the model manually into a single collection.  It also makes sure that those two sources
    /// of aliases don't conflict.
    /// 
    /// </summary>
    public class AliasCollection : IList<string>
    {
        private class AliasCollectionEnumerator : IEnumerator<string>
        {
            private AliasCollection _collection;
            private IEnumerator<string> _currentEnumerator;
            private int _state; // 0: overrides, 1: metadata, 2: done
            private string _current;

            public AliasCollectionEnumerator(AliasCollection collection)
            {
                _collection = collection;
                _currentEnumerator = collection.overrides.GetEnumerator();
                _state = 0;
                _current = null;
            }

            public string Current => _current;

            object System.Collections.IEnumerator.Current => _current;

            public bool MoveNext()
            {
                // Handle overrides
                if (_state == 0)
                {
                    if (_currentEnumerator.MoveNext())
                    {
                        _current = _currentEnumerator.Current;
                        return true;
                    }
                    // Move to the next state (metadata)
                    _currentEnumerator.Dispose();
                    _currentEnumerator = _collection.metadataEval().GetEnumerator();
                    _state = 1;
                }

                // Handle metadata
                if (_state == 1)
                {
                    if (_currentEnumerator.MoveNext())
                    {
                        _current = _currentEnumerator.Current;
                        // Move default alias to the front dynamically if needed
                        if (_collection._defaultAlias != null && _current == _collection._defaultAlias)
                        {
                            return MoveNext(); // Skip this as it will be handled in the next step
                        }
                        return true;
                    }
                    _currentEnumerator.Dispose();
                    _state = 2;
                }

                // Handle default alias if necessary
                if (_state == 2 && _collection._defaultAlias != null)
                {
                    _current = _collection._defaultAlias;
                    _collection._defaultAlias = null; // Only yield once
                    return true;
                }

                // Enumeration complete
                _current = null;
                return false;
            }

            public void Reset()
            {
                _currentEnumerator?.Dispose();
                _currentEnumerator = _collection.overrides.GetEnumerator();
                _state = 0;
                _current = null;
            }

            public void Dispose()
            {
                _currentEnumerator?.Dispose();
            }
        }


        private string _defaultAlias;

        public string DefaultAlias
        {
            get
            {
                if (_defaultAlias != null && this.Contains(_defaultAlias) == false)
                {
                    _defaultAlias = null;
                }

                return _defaultAlias ?? this.First();
            }
            set
            {
                if (this.Contains(value) == false)
                {
                    throw new InvalidOperationException($"{value} is not in the collection");
                }

                _defaultAlias = value;

            }
        }

        List<string> overrides;

        Func<List<string>> metadataEval;
        Func<bool> ignoreCaseEval;


        private AliasCollection(Func<List<string>> metadataEval, Func<bool> ignoreCaseEval)
        {
            this.metadataEval = metadataEval;
            overrides = new List<string>();
            this.ignoreCaseEval = ignoreCaseEval;
        }

        internal AliasCollection(Func<List<ArgShortcut>> aliases, Func<bool> ignoreCaseEval, bool stripLeadingArgInticatorsOnAttributeValues = true) : this(EvaluateAttributes(aliases, stripLeadingArgInticatorsOnAttributeValues), ignoreCaseEval) { }


        private static Func<List<string>> EvaluateAttributes(Func<List<ArgShortcut>> eval, bool stripLeadingArgIndicatorsOnAttributeValues)
        {
            return () =>
            {
                // Get the list of ArgShortcut objects from the eval function
                List<ArgShortcut> shortcuts = eval();

                // Allocate only one list for the result
                List<string> ret = new List<string>();

                // Avoid LINQ and sort the list in place to reduce allocations
                shortcuts.Sort((a, b) =>
                {
                    int lenA = a.Shortcut == null ? 0 : a.Shortcut.Length;
                    int lenB = b.Shortcut == null ? 0 : b.Shortcut.Length;
                    return lenA.CompareTo(lenB);
                });

                // Iterate over the sorted list and perform processing
                foreach (var attr in shortcuts)
                {
                    if (attr.Policy == ArgShortcutPolicy.NoShortcut && attr.Shortcut != null)
                    {
                        throw new InvalidArgDefinitionException("You cannot specify a shortcut value and an ArgShortcutPolicy of NoShortcut");
                    }

                    var value = attr.Shortcut;

                    if (value != null && stripLeadingArgIndicatorsOnAttributeValues)
                    {
                        if (value.StartsWith("-")) value = value.Substring(1);
                        else if (value.StartsWith("/")) value = value.Substring(1);
                    }

                    if (value != null)
                    {
                        // Avoid duplicate entries in the result list
                        if (ret.Contains(value))
                        {
                            throw new InvalidArgDefinitionException("Duplicate ArgShortcut attributes with value: " + value);
                        }
                        ret.Add(value);
                    }
                }

                return ret;
            };
        }


        /// <summary>
        /// Gets the index of the given alias in the collection.
        /// </summary>
        /// <param name="item">the alias to look for</param>
        /// <returns>The index of item if found in the list; otherwise, -1.</returns>
        public int IndexOf(string item)
        {
            var i = 0;
            foreach (var alias in this)
            {
                if (alias == item) return i;
                i++;
            }
            return -1;
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="index">Not supported</param>
        /// <param name="item">Not supported</param>
        public void Insert(int index, string item)
        {
            throw new NotSupportedException("Insert is not supported");
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="index">Not supported</param>
        public void RemoveAt(int index)
        {
            throw new NotSupportedException("RemoveAt is not supported");
        }

        /// <summary>
        /// The setter is not supported.  The getter returns the item at the specified index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns>the item at the specified index</returns>
        public string this[int index]
        {
            get
            {
                var iter = 0;
                foreach (var alias in this)
                {
                    if (iter == index) return alias;
                    iter++;
                }
                throw new IndexOutOfRangeException();
            }
            set
            {
                throw new NotSupportedException("Setting by index is not supported");
            }
        }

        /// <summary>
        /// Adds the given aliases to the collection. 
        /// </summary>
        /// <param name="items">The alias to add</param>
        public void AddRange(IEnumerable<string> items)
        {
            foreach (var item in items) Add(item);
        }

        /// <summary>
        /// Adds the given alias to the collection.  An InvalidArgDefinitionException is thrown if you try to add
        /// the same alias twice (case sensitivity is determined by the CommandLineArgument or CommandLineAction).
        /// </summary>
        /// <param name="item">The alias to add</param>
        public void Add(string item)
        {
            foreach (var alias in this)
            {
                if (string.Equals(alias, item, ignoreCaseEval() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    throw new InvalidArgDefinitionException("The alias '" + item + "' has already been added");
                }
            }

            overrides.Add(item);
        }

        /// <summary>
        /// Clear is not supported, use ClearOverrides() to clear items that have manually been added
        /// </summary>
        public void Clear()
        {
            throw new NotSupportedException("Clear is not supported, use ClearOverrides() to clear items that have manually been added");
        }

        /// <summary>
        /// Clears the aliases that have been manually addd to this collection via Add() or AddRange().
        /// Aliases that are inferred from the Metadata will still be present in the collection. 
        /// </summary>
        public void ClearOverrides()
        {
            overrides.Clear();
        }

        /// <summary>
        /// Tests to see if this Alias collection contains the given item.  Case sensitivity is enforced
        /// based on the CommandLineArgument or CommandLineAction.
        /// </summary>
        /// <param name="item">The item to test for containment</param>
        /// <returns>True if the collection contains the item, otherwise false</returns>
        public bool Contains(string item)
        {
            foreach (var alias in this)
            {
                if (string.Equals(alias, item, ignoreCaseEval() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Copies this collection to an array, starting at the given index
        /// </summary>
        /// <param name="array">the destination array</param>
        /// <param name="arrayIndex">the starting index of where to place the elements into the destination</param>
        public void CopyTo(string[] array, int arrayIndex)
        {
            foreach (var alias in this)
            {
                array[arrayIndex] = alias;
                arrayIndex++;
            }
        }

        /// <summary>
        /// Gets the count of aliases
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;
                foreach (var alias in this)
                {
                    count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Not read only ever
        /// </summary>
        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes the given alias from the collection if it was added via Add() or AddRange().  If
        /// it was added by injecting metadata into a CommandLineArgument or a CommandLineAction then
        /// an InvalidOperationException will be thrown.  The correct way to remove metadata injected
        /// aliases is to remove it from the metadata directly.
        /// </summary>
        /// <param name="item">the item to remove</param>
        /// <returns>true if the alias was removed, false otherwise</returns>
        public bool Remove(string item)
        {
            if (overrides.Contains(item))
            {
                return overrides.Remove(item);
            }
            else if (metadataEval().Contains(item))
            {
                throw new InvalidOperationException("The alias '" + item + "' was added via metadata and cannot be removed from this collection");
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets an enumerator capable of enumerating all aliases
        /// </summary>
        /// <returns>an enumerator capable of enumerating all aliases</returns>
        public IEnumerator<string> GetEnumerator()
        {
            return new AliasCollectionEnumerator(this);
        }

        /// <summary>
        /// Gets an enumerator capable of enumerating all aliases
        /// </summary>
        /// <returns>an enumerator capable of enumerating all aliases</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new AliasCollectionEnumerator(this);
        }
    }
}
