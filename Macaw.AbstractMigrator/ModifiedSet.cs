using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Macaw.AbstractMigrator
{
    /// <summary>
    /// INterface for the result of 2 sequences comparison
    /// </summary>
    /// <typeparam name="T">Implements IEquatable</typeparam>
    public interface IModifiedSet<T>
    {
        /// <summary>
        /// Gets the sequence of items added
        /// </summary>
        IEnumerable<T> Added { get; }

        /// <summary>
        /// Gets the sequence of items removed
        /// </summary>
        IEnumerable<T> Removed { get; }

        /// <summary>
        /// Gets the sequence of items modified
        /// </summary>
        IEnumerable<ModifiedItem<T>> Modified { get; }

        /// <summary>
        /// Nothing has been added,removed or modified.
        /// </summary>
        bool IsEmpty { get; }
    }

    /// <summary>
    /// Pair of old and new objects. Used by list comparator
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct ModifiedItem<T>
    {
        public ModifiedItem(T old, T @new)
        {
            _old = old;
            _new = @new;
        }

        private T _old;
        public T Old
        {
            get { return _old; }
        }

        private T _new;
        public T New
        {
            get { return _new; }
        }
    }

    public class ModifiedSet<T> : IModifiedSet<T>
    {
        List<T> _add = new List<T>();
        List<T> _remove = new List<T>();
        List<ModifiedItem<T>> _mods = new List<ModifiedItem<T>>();

        public IEnumerable<T> Added
        {
            get
            {
                return _add;
            }
        }

        public IEnumerable<T> Removed
        {
            get
            {
                return _remove;
            }
        }

        public IEnumerable<ModifiedItem<T>> Modified
        {
            get { return _mods; }
        }

        public bool IsEmpty
        {
            get { return _add.Count == 0 && _remove.Count == 0 && _mods.Count == 0; }
        }

        public void ModifiedItem(T old, T @new)
        {
            _mods.Add(new ModifiedItem<T>(old, @new));
        }

        public void AddedItem(T tag)
        {
            _add.Add(tag);
        }

        public void RemovedItem(T tag)
        {
            _remove.Add(tag);
        }
    }
}
