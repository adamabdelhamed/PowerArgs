using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerArgs
{
    public class AliasCollection : IList<string>
    {
        private class AliasCollectionEnumerator : IEnumerator<string>
        {
            AliasCollection c;

            IEnumerator<string> wrapped;

            public AliasCollectionEnumerator(AliasCollection c)
            {
                this.c = c;
                List<string> completeList = new List<string>();
                completeList.AddRange(c.overrides);
                completeList.AddRange(c.metadataEval());
                wrapped = completeList.GetEnumerator();
            }

            public string Current
            {
                get { return wrapped.Current; }
            }

            public void Dispose()
            {
                wrapped.Dispose();
            }

            object System.Collections.IEnumerator.Current
            {
                get { return wrapped.Current; }
            }

            public bool MoveNext()
            {
                return wrapped.MoveNext();
            }

            public void Reset()
            {
                wrapped.Reset();
            }
        }

        List<string> overrides;

        Func<List<string>> metadataEval;
        Func<bool> ignoreCaseEval;

        private IList<string> NormalizedList
        {
            get
            {
                List<string> ret = new List<string>();
                foreach (var item in this) ret.Add(item);
                return ret.AsReadOnly();
            }
        }

        private AliasCollection(Func<List<string>> metadataEval, Func<bool> ignoreCaseEval)
        {
            this.metadataEval = metadataEval;
            overrides = new List<string>();
            this.ignoreCaseEval = ignoreCaseEval;
        }

        internal AliasCollection(Func<List<ArgShortcut>> aliases, Func<bool> ignoreCaseEval) : this(EvaluateAttributes(aliases), ignoreCaseEval) { }


        private static Func<List<string>> EvaluateAttributes(Func<List<ArgShortcut>> eval)
        {
            return () =>
            {
                List<ArgShortcut> shortcuts = eval();

                List<string> ret = new List<string>();

                foreach (var attr in shortcuts.OrderBy(a => a.Shortcut == null ? 0 : a.Shortcut.Length))
                {
                    bool noShortcut = false;
                    if (attr.Policy == ArgShortcutPolicy.NoShortcut)
                    {
                        noShortcut = true;
                    }

                    var value = attr.Shortcut;

                    if (noShortcut && value != null)
                    {
                        throw new InvalidArgDefinitionException("You cannot specify a shortcut value and an ArgShortcutPolicy of NoShortcut");
                    }

                    if (value != null)
                    {
                        if (value.StartsWith("-")) value = value.Substring(1);
                        else if (value.StartsWith("/")) value = value.Substring(1);
                    }

                    if (value != null)
                    {
                        if (ret.Contains(value))
                        {
                            throw new InvalidArgDefinitionException("Duplicate ArgShortcut attributes with value: "+value);
                        }
                        ret.Add(value);
                    }
                }

                return ret;
            };
        }

        public int IndexOf(string item)
        {
            return NormalizedList.IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            throw new NotSupportedException("Insert is not supported");
        }

        public void RemoveAt(int index)
        {
            throw new NotSupportedException("RemoveAt is not supported");
        }

        public string this[int index]
        {
            get
            {
                return NormalizedList[index];
            }
            set
            {
                throw new NotSupportedException("Setting by index is not supported");
            }
        }

        public void AddRange(IEnumerable<string> items)
        {
            foreach (var item in items) Add(item);
        }

        public void Add(string item)
        {
            if (NormalizedList.Contains(item, new CaseAwareStringComparer(ignoreCaseEval())))
            {
                throw new InvalidArgDefinitionException("The alias '"+item+"' has already been added");
            }

            overrides.Add(item);
        }

        public void Clear()
        {
            throw new NotSupportedException("Clear is not supported, use ClearOverrides() to clear items that have manually been added");
        }

        public void ClearOverrides()
        {
            overrides.Clear();
        }

        public bool Contains(string item)
        {
            return NormalizedList.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            NormalizedList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return NormalizedList.Count(); }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

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

        public IEnumerator<string> GetEnumerator()
        {
            return new AliasCollectionEnumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new AliasCollectionEnumerator(this);
        }
    }
}
