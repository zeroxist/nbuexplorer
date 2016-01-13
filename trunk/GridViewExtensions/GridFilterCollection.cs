using System;
using System.Collections;
using System.Windows.Forms;

namespace NbuExplorer.GridViewExtensions
{
    /// <summary>
    ///     Typesafe collection of <see cref="IGridFilter" />s.
    /// </summary>
    public class GridFilterCollection : ReadOnlyCollectionBase
    {
        private readonly Hashtable _columnsToGridFiltersHash;

        /// <summary>
        ///     Creates a new instance.
        /// </summary>
        /// <param name="columns">List of <see cref="DataGridViewColumn" />s which are associated with <see cref="IGridFilter" />s.</param>
        /// <param name="columnsToGridFiltersHash">
        ///     Mapping between <see cref="DataGridViewColumn" />s and
        ///     <see cref="IGridFilter" />s.
        /// </param>
        internal GridFilterCollection(IList columns, Hashtable columnsToGridFiltersHash)
        {
            _columnsToGridFiltersHash = (Hashtable) columnsToGridFiltersHash.Clone();

            foreach (DataGridViewColumn column in columns)
            {
                var gridFilter = (IGridFilter) _columnsToGridFiltersHash[column];
                if (gridFilter != null)
                    InnerList.Add(gridFilter);
            }
        }

        /// <summary>
        ///     Gets the element at the specified index.
        /// </summary>
        public IGridFilter this[int index]
        {
            get { return (IGridFilter) InnerList[index]; }
        }

        /// <summary>
        ///     Gets the <see cref="IGridFilter" /> which is associated with the given <see cref="DataGridViewColumn" />.
        /// </summary>
        public IGridFilter this[DataGridViewColumn column]
        {
            get
            {
                if (InnerList.Contains(_columnsToGridFiltersHash[column]))
                    return (IGridFilter) _columnsToGridFiltersHash[column];
                return null;
            }
        }

        /// <summary>
        ///     Determines whether a given <see cref="IGridFilter" /> is contained in this instance.
        /// </summary>
        /// <param name="gridFilter"><see cref="IGridFilter" /> to be checked.</param>
        /// <returns>True if <see cref="IGridFilter" /> is contained in this instance otherwise False.</returns>
        public bool Contains(IGridFilter gridFilter)
        {
            return InnerList.Contains(gridFilter);
        }

        /// <summary>
        ///     Gets a <see cref="IGridFilter" /> which is associated with a <see cref="DataGridViewColumn" />
        ///     with the specified <see cref="DataGridViewColumn.Name" />.
        /// </summary>
        /// <param name="name">
        ///     <see cref="DataGridViewColumn.Name" />
        /// </param>
        /// <returns>An <see cref="IGridFilter" /> or null if no appropriate was found.</returns>
        public IGridFilter GetByName(string name)
        {
            foreach (DataGridViewColumn column in _columnsToGridFiltersHash.Keys)
                if (column.Name == name)
                    return this[column];
            return null;
        }

        /// <summary>
        ///     Gets a <see cref="IGridFilter" /> which is associated with a <see cref="DataGridViewColumn" />
        ///     with the specified <see cref="DataGridViewColumn.HeaderText" />.
        /// </summary>
        /// <param name="headerText">
        ///     <see cref="DataGridViewColumn.HeaderText" />
        /// </param>
        /// <returns>An <see cref="IGridFilter" /> or null if no appropriate was found.</returns>
        public IGridFilter GetByHeaderText(string headerText)
        {
            foreach (DataGridViewColumn column in _columnsToGridFiltersHash.Keys)
                if (column.HeaderText == headerText)
                    return this[column];
            return null;
        }

        /// <summary>
        ///     Gets a <see cref="IGridFilter" /> which is associated with a <see cref="DataGridViewColumn" />
        ///     with the specified <see cref="DataGridViewColumn.DataPropertyName" />.
        /// </summary>
        /// <param name="dataPropertyName">
        ///     <see cref="DataGridViewColumn.DataPropertyName" />
        /// </param>
        /// <returns>An <see cref="IGridFilter" /> or null if no appropriate was found.</returns>
        public IGridFilter GetByDataPropertyName(string dataPropertyName)
        {
            foreach (DataGridViewColumn column in _columnsToGridFiltersHash.Keys)
                if (column.DataPropertyName == dataPropertyName)
                    return this[column];
            return null;
        }

        /// <summary>
        ///     Creates a filtered list which only contains <see cref="IGridFilter" />s of the specified type.
        /// </summary>
        /// <param name="dataType">Type by which should be filtered.</param>
        /// <param name="exactMatch">
        ///     Defines whether the types must match exactly
        ///     (otherwise inheriting types will also be returned).
        /// </param>
        /// <returns>Collection of matching <see cref="IGridFilter" />s.</returns>
        public GridFilterCollection FilterByGridFilterType(Type dataType, bool exactMatch)
        {
            if (!typeof (IGridFilter).IsAssignableFrom(dataType))
                throw new ArgumentException("Given type must implement IGridFilter.", "dataType");
            var filtered = new ArrayList();
            foreach (DataGridViewColumn column in _columnsToGridFiltersHash.Keys)
                if (this[column] != null &&
                    (this[column].GetType().Equals(dataType)
                     || (!exactMatch && dataType.IsAssignableFrom(this[column].GetType()))))
                {
                    filtered.Add(column);
                }
            return new GridFilterCollection(filtered, _columnsToGridFiltersHash);
        }
    }
}