using System;
using System.Windows.Forms;

namespace NbuExplorer.GridViewExtensions
{
    /// <summary>
    ///     Argumentsclass for events needing extended informations about <see cref="IGridFilter" />s.
    /// </summary>
    public class GridFilterEventArgs : EventArgs
    {
        /// <summary>
        ///     Creates a new instance
        /// </summary>
        /// <param name="column">Column the <see cref="IGridFilter" /> is created for.</param>
        /// <param name="gridFilter">Default <see cref="IGridFilter" /> instance.</param>
        public GridFilterEventArgs(DataGridViewColumn column, IGridFilter gridFilter)
        {
            Column = column;
            GridFilter = gridFilter;
        }

        /// <summary>
        ///     Type of the column the <see cref="IGridFilter" /> is created for.
        /// </summary>
        public Type DataType
        {
            get { return Column.ValueType; }
        }

        /// <summary>
        ///     Name of the column the <see cref="IGridFilter" /> is created for.
        /// </summary>
        public string ColumnName
        {
            get { return Column.DataPropertyName; }
        }

        /// <summary>
        ///     The column the <see cref="IGridFilter" /> is created for.
        /// </summary>
        public DataGridViewColumn Column { get; }

        /// <summary>
        ///     Text of the header of the column the <see cref="IGridFilter" /> is created for.
        /// </summary>
        public string HeaderText
        {
            get { return Column.HeaderText; }
        }

        /// <summary>
        ///     Gets/sets the <see cref="IGridFilter" /> which should be used.
        /// </summary>
        public IGridFilter GridFilter { get; set; }
    }
}