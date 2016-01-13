using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using NbuExplorer.GridViewExtensions.GridFilterFactories;

namespace NbuExplorer.GridViewExtensions
{
    /// <summary>
    ///     A control where all controls all placed which are necessary for
    ///     extending a grid for filtering.
    /// </summary>
    internal class GridFiltersControl : UserControl, ISupportInitialize
    {
        private readonly Hashtable _columnToGridFilterHash;
        private readonly Hashtable _keepFiltersHash;
        private readonly Container components = null;
        private RefreshMode _autoRefreshMode = RefreshMode.OnInput;
        private bool _baseFilterEnabled = true;
        private LogicalOperators _baseFilterOperator = LogicalOperators.And;
        private string _customFilter;
        private IGridFilterFactory _filterFactory;
        private DataGridView _grid;
        private int _initCounter;
        private bool _keepFilters;
        private string _lastRowFilter = "";
        private Label _lblFilter;
        private LogicalOperators _operator;
        private TextBox _refBox;
        private bool _refreshDisabled;

        /// <summary>
        ///     Creates a new instance
        /// </summary>
        internal GridFiltersControl()
        {
            InitializeComponent();

            _columnToGridFilterHash = new Hashtable();
            _keepFiltersHash = new Hashtable();
            BaseFilters = new StringDictionary();

            FilterFactory = new DefaultGridFilterFactory();

            RecreateGridFilters();
        }

        public override RightToLeft RightToLeft
        {
            get { return base.RightToLeft; }
            set
            {
                try
                {
                    _initCounter++;
                    base.RightToLeft = value;
                }
                finally
                {
                    _initCounter--;
                }
                RecreateGridFilters();
            }
        }

        internal string CustomFilter
        {
            get { return _customFilter; }
            set
            {
                _customFilter = value;
                RecreateRowFilter();
            }
        }

        /// <summary>
        ///     Gets and sets the <see cref="DataGridView" /> instance to use.
        /// </summary>
        internal DataGridView DataGridView
        {
            get { return _grid; }
            set
            {
                if (_grid != null)
                {
                    _grid.DataSourceChanged -= OnDataSourceChanged;
                    _grid.DataMemberChanged -= OnDataSourceChanged;
                    _grid.ColumnWidthChanged -= OnGridColumnsChanged;
                    _grid.ColumnDisplayIndexChanged -= OnGridColumnsChanged;
                    _grid.ColumnAdded -= OnGridColumnsAddedRemoved;
                    _grid.ColumnRemoved -= OnGridColumnsAddedRemoved;
                    _grid.ColumnStateChanged -= OnGridColumnsStateChanged;
                    _grid.Scroll -= OnGridScroll;
                }

                _grid = value;

                if (_grid != null)
                {
                    _grid.DataSourceChanged += OnDataSourceChanged;
                    _grid.DataMemberChanged += OnDataSourceChanged;
                    _grid.ColumnWidthChanged += OnGridColumnsChanged;
                    _grid.ColumnDisplayIndexChanged += OnGridColumnsChanged;
                    _grid.ColumnAdded += OnGridColumnsAddedRemoved;
                    _grid.ColumnRemoved += OnGridColumnsAddedRemoved;
                    _grid.ColumnStateChanged += OnGridColumnsStateChanged;
                    _grid.Scroll += OnGridScroll;
                }
                RecreateGridFilters();
            }
        }

        /// <summary>
        ///     Gets and sets whether filters are kept while switching between different tables.
        /// </summary>
        internal bool KeepFilters
        {
            get { return _keepFilters; }
            set
            {
                _keepFilters = value;
                if (!_keepFilters)
                    _keepFiltersHash.Clear();
                else
                    RecreateRowFilter();
            }
        }

        /// <summary>
        ///     Gets and sets whether the filter criteria is automatically refreshed when
        ///     changes are made to the filter controls. If set to false then a call to
        ///     <see cref="RefreshFilters" /> is needed to manually refresh the criteria.
        /// </summary>
        internal RefreshMode AutoRefreshMode
        {
            get { return _autoRefreshMode; }
            set
            {
                _autoRefreshMode = value;
                RecreateRowFilter();
            }
        }

        /// <summary>
        ///     Gets and sets the text for the filter label.
        /// </summary>
        internal string FilterText
        {
            get { return _lblFilter.Text; }
            set { _lblFilter.Text = value; }
        }

        /// <summary>
        ///     Gets and sets the <see cref="IGridFilterFactory" /> used to generate the filter GUI.
        /// </summary>
        internal IGridFilterFactory FilterFactory
        {
            get { return _filterFactory; }
            set
            {
                if (_filterFactory != null)
                    _filterFactory.Changed -= OnFilterFactoryChanged;
                _filterFactory = value;
                if (_filterFactory == null)
                    _filterFactory = new DefaultGridFilterFactory();
                _filterFactory.Changed += OnFilterFactoryChanged;
                RecreateGridFilters();
            }
        }

        /// <summary>
        ///     The selected operator to combine the filter criterias.
        /// </summary>
        internal LogicalOperators Operator
        {
            get { return _operator; }
            set
            {
                _operator = value;
                RecreateRowFilter();
            }
        }

        /// <summary>
        ///     Gets and sets whether the filter label should be visible.
        /// </summary>
        internal bool FilterTextVisible
        {
            get { return _lblFilter.Visible; }
            set { _lblFilter.Visible = value; }
        }

        /// <summary>
        ///     Gets and sets what information is showed to the user
        ///     if an error in the builded filter criterias occurs.
        /// </summary>
        internal FilterErrorModes MessageErrorMode { get; set; } = FilterErrorModes.General;

        /// <summary>
        ///     Gets and sets what information is showed to the user
        ///     if an error in the builded filter criterias occurs.
        /// </summary>
        internal FilterErrorModes ConsoleErrorMode { get; set; } = FilterErrorModes.Off;

        /// <summary>
        ///     Gets a modifyable collection which maps <see cref="DataTable.TableName" />s
        ///     to base filter strings which are applied in front of the automatically
        ///     created filter.
        /// </summary>
        /// <remarks>
        ///     The grid contents is not automatically refreshed when modifying this
        ///     collection. A call to <see cref="RefreshFilters" /> is needed for this.
        /// </remarks>
        internal StringDictionary BaseFilters { get; }

        /// <summary>
        ///     Gets or sets which operator should be used to combine the base filter
        ///     with the automatically created filters.
        /// </summary>
        internal LogicalOperators BaseFilterOperator
        {
            get { return _baseFilterOperator; }
            set
            {
                _baseFilterOperator = value;
                RecreateRowFilter();
            }
        }

        /// <summary>
        ///     Gets or sets whether base filters should be used when refreshing
        ///     the filter criteria. Setting it to false will disable the functionality
        ///     while still keeping the base filter strings in the <see cref="BaseFilters" />
        ///     collection intact.
        /// </summary>
        internal bool BaseFilterEnabled
        {
            get { return _baseFilterEnabled; }
            set
            {
                _baseFilterEnabled = value;
                RecreateRowFilter();
            }
        }

        /// <summary>
        ///     Gets or sets the currently used base filter. Internally it adjusts the
        ///     <see cref="BaseFilters" /> collection with the given value and the current
        ///     <see cref="DataTable.TableName" /> and also initiates a refresh.
        /// </summary>
        internal string CurrentTableBaseFilter
        {
            get
            {
                if (!HasView)
                    return null;
                return BaseFilters[GetTableName()];
            }
            set
            {
                if (!HasView)
                    return;

                BaseFilters[GetTableName()] = value;
                RecreateRowFilter();
            }
        }

        private bool HasView
        {
            get
            {
                if (_grid != null &&
                    _grid.DataSource != null)
                {
                    if (_grid.DataSource is DataTable)
                        return true;
                    if (_grid.DataSource is DataView)
                        return true;
                    if (_grid.DataSource is BindingSource)
                        return true;
                    if (_grid.DataSource is IBindingListView)
                        return true;
                }
                return false;
            }
        }

        private List<DataGridViewColumn> SortedColumns
        {
            get
            {
                var result = new List<DataGridViewColumn>();
                var column = _grid.Columns.GetFirstColumn(DataGridViewElementStates.None);
                if (column == null)
                    return result;
                result.Add(column);
                while ((column = _grid.Columns.GetNextColumn(column, DataGridViewElementStates.None, DataGridViewElementStates.None)) != null)
                    result.Add(column);

                return result;
            }
        }

        public void BeginInit()
        {
            _initCounter++;
        }

        public void EndInit()
        {
            _initCounter--;
        }

        /// <summary>
        ///     Erforderliche Methode f|r die Designerunterst|tzung.
        ///     Der Inhalt der Methode darf nicht mit dem Code-Editor gedndert werden.
        /// </summary>
        private void InitializeComponent()
        {
            _refBox = new TextBox();
            _lblFilter = new Label();
            SuspendLayout();
            //
            // _refBox
            //
            _refBox.Anchor = (AnchorStyles.Top | AnchorStyles.Left)
                             | AnchorStyles.Right;
            _refBox.Location = new Point(344, 0);
            _refBox.Name = "_refBox";
            _refBox.Size = new Size(40, 20);
            _refBox.TabIndex = 0;
            _refBox.Text = "textBox1";
            _refBox.Visible = false;
            //
            // _lblFilter
            //
            _lblFilter.Dock = DockStyle.Left;
            _lblFilter.Location = new Point(0, 0);
            _lblFilter.Name = "_lblFilter";
            _lblFilter.Size = new Size(100, 24);
            _lblFilter.TabIndex = 1;
            _lblFilter.Text = "Filter";
            _lblFilter.TextAlign = ContentAlignment.MiddleLeft;
            //
            // GridFiltersControl
            //
            Controls.Add(_lblFilter);
            Controls.Add(_refBox);
            Name = "GridFiltersControl";
            Size = new Size(384, 24);
            ResumeLayout(false);
        }

        /// <summary>
        ///     Tries to resolve a <see cref="IBindingListView" /> from a given data source.
        /// </summary>
        /// <param name="dataSource"></param>
        /// <param name="dataMember"></param>
        /// <returns></returns>
        internal static IBindingListView GetViewFromDataSource(object dataSource, string dataMember)
        {
            if (dataSource == null)
                return null;
            if (dataSource as IBindingListView != null)
                return dataSource as IBindingListView;
            if (dataSource as DataTable != null)
                return (dataSource as DataTable).DefaultView;
            if (dataSource as DataSet != null)
            {
                var dataTable = (dataSource as DataSet).Tables[dataMember];
                if (dataTable != null)
                    return dataTable.DefaultView;
                return null;
            }
            return null;
        }

        /// <summary>
        ///     Event, which gets fired whenever the filter criteria has been changed.
        /// </summary>
        internal event EventHandler AfterFiltersChanged;

        /// <summary>
        ///     Event, which gets fired whenever the filter criteria are going to be changed.
        /// </summary>
        internal event EventHandler BeforeFiltersChanging;

        /// <summary>
        ///     Event, which gets fired whenever an <see cref="IGridFilter" /> has been bound
        ///     and thus added to this instance.
        /// </summary>
        internal event GridFilterEventHandler GridFilterBound;

        /// <summary>
        ///     Event, which gets fired whenever an <see cref="IGridFilter" /> has been unbound
        ///     and thus removed to this instance.
        /// </summary>
        internal event GridFilterEventHandler GridFilterUnbound;

        /// <summary>
        ///     Die verwendeten Ressourcen bereinigen.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            DataGridView = null;

            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        ///     Initiates recalculation for the positions of the filter GUI elements.
        /// </summary>
        /// <param name="e">Event data</param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RepositionGridFilters();
        }

        /// <summary>
        ///     Initiates recalculation for the positions of the filter GUI elements.
        /// </summary>
        /// <param name="e">Event data</param>
        protected override void OnRightToLeftChanged(EventArgs e)
        {
            base.OnRightToLeftChanged(e);
            RepositionGridFilters();
        }

        /// <summary>
        ///     Clears all filters to initial state.
        /// </summary>
        internal void ClearFilters()
        {
            try
            {
                _refreshDisabled = true;
                foreach (IGridFilter gridFilter in _columnToGridFilterHash.Values)
                    gridFilter.Clear();
            }
            finally
            {
                _refreshDisabled = false;
            }
            RecreateRowFilter();
        }

        /// <summary>
        ///     Sets all filters to the specified values.
        ///     The values must be in order of the column styles in the current view.
        ///     This function should normally be used with data previously coming
        ///     from the <see cref="GetFilters" /> function.
        /// </summary>
        /// <param name="filters">filters to set</param>
        internal void SetFilters(string[] filters)
        {
            for (var i = 0; i < _grid.Columns.Count && i < filters.Length; i++)
            {
                var gridFilter = (IGridFilter) _columnToGridFilterHash[_grid.Columns[i]];
                if (filters[i].Length > 0)
                    gridFilter.SetFilter(filters[i]);
                else
                    gridFilter.Clear();
            }
        }

        /// <summary>
        ///     Gets all filters currently set
        /// </summary>
        /// <returns></returns>
        internal string[] GetFilters()
        {
            var result = new string[_columnToGridFilterHash.Count];
            for (var i = 0; i < _grid.Columns.Count; i++)
            {
                var column = _grid.Columns[i];
                var gridFilter = (IGridFilter) _columnToGridFilterHash[column];
                if (gridFilter.HasFilter)
                    result[i] = gridFilter.GetFilter(string.Format("[{0}]", column.DataPropertyName));
                else
                    result[i] = "";
            }
            return result;
        }

        /// <summary>
        ///     Refreshes the filter criteria to match the current contents of the associated
        ///     filter controls.
        /// </summary>
        internal void RefreshFilters()
        {
            _lastRowFilter = "_";
            RecreateRowFilter(true);
        }

        /// <summary>
        ///     Gets all currently set <see cref="IGridFilter" />s.
        /// </summary>
        /// <returns>Collection of <see cref="IGridFilter" />s.</returns>
        internal GridFilterCollection GetGridFilters()
        {
            if (_grid.Columns == null ||
                _columnToGridFilterHash == null)
                return null;

            return new GridFilterCollection(_grid.Columns, _columnToGridFilterHash);
        }

        private void SetRowFilter(string rowFilter)
        {
            OnBeforeFiltersChanging(EventArgs.Empty);
            try
            {
                if (_grid != null &&
                    _grid.DataSource != null)
                {
                    if (_grid.DataSource is DataTable)
                        ((DataTable) _grid.DataSource).DefaultView.RowFilter = rowFilter;
                    else if (_grid.DataSource is DataView)
                        ((DataView) _grid.DataSource).RowFilter = rowFilter;
                    else if (_grid.DataSource is BindingSource)
                        ((BindingSource) _grid.DataSource).Filter = rowFilter;
                    else if (_grid.DataSource is IBindingListView)
                        ((IBindingListView) _grid.DataSource).Filter = rowFilter;
                }
            }
            finally
            {
                OnAfterFiltersChanged(EventArgs.Empty);
            }
        }

        private string GetTableName()
        {
            if (_grid != null &&
                _grid.DataSource != null)
            {
                string name = null;
                if (GetDataSourceName(_grid.DataSource, ref name))
                    return name;
                if (_grid.DataSource is BindingSource)
                {
                    if (GetDataSourceName(((BindingSource) _grid.DataSource).DataSource, ref name))
                        return name;
                    return ((BindingSource) _grid.DataSource).DataMember;
                }
                if (_grid.DataSource is IBindingListView)
                {
                    return _grid.DataSource.GetType().Name;
                }
            }
            return null;
        }

        private bool GetDataSourceName(object dataSource, ref string name)
        {
            if (_grid.DataSource is DataTable)
            {
                name = ((DataTable) _grid.DataSource).TableName;
                return true;
            }
            if (_grid.DataSource is DataView)
            {
                name = ((DataView) _grid.DataSource).Table.TableName;
                return true;
            }
            if (_grid.DataSource is IBindingListView)
            {
                name = _grid.DataSource.GetType().Name;
                return true;
            }
            return false;
        }

        /*
        private BindingSource CurrentBindingSource
        {
            get { return _currentBindingSource; }
            set
            {
                if (value == _currentBindingSource)
                    return;

                if (_currentBindingSource != null)
                {
                    _currentBindingSource.DataSourceChanged -= new EventHandler(OnDataSourceChanged);
                    _currentBindingSource.DataMemberChanged -= new EventHandler(OnDataSourceChanged);
                }

                _currentBindingSource = value;

                if (_currentBindingSource != null)
                {
                    _currentBindingSource.DataSourceChanged += new EventHandler(OnDataSourceChanged);
                    _currentBindingSource.DataMemberChanged += new EventHandler(OnDataSourceChanged);
                }
            }
        }

        private DataView CurrentView
        {
            get
            {
                if (_grid == null)
                {
                    this.CurrentBindingSource = null;
                    return null;
                }
                else if (_grid.DataSource as BindingSource != null)
                {
                    this.CurrentBindingSource = _grid.DataSource as BindingSource;
                    return this.CurrentBindingSource.List as DataView;
                }
                else
                {
                    this.CurrentBindingSource = null;
                    return GetViewFromDataSource(_grid.DataSource, _grid.DataMember);
                }
            }
        }
        */

        /// <summary>
        ///     Initiates a recalculation of the needed filter GUI elements and their positions.
        /// </summary>
        private void RecreateGridFilters()
        {
            if (_initCounter > 0)
                return;

            //first clean up what has beed done before
            foreach (DataGridViewColumn column in _columnToGridFilterHash.Keys)
            {
                var gridFilter = _columnToGridFilterHash[column] as IGridFilter;
                gridFilter.Changed -= OnFilterChanged;
                gridFilter.FilterControl.KeyPress -= OnFilterControlKeyPress;
                gridFilter.FilterControl.Leave -= OnFilterControlLeave;
                if (Controls.Contains(gridFilter.FilterControl))
                {
                    Controls.Remove(gridFilter.FilterControl);
                    gridFilter.FilterControl.Dispose();
                }
                OnGridFilterUnbound(new GridFilterEventArgs(column, gridFilter));
            }
            _columnToGridFilterHash.Clear();

            //adjust the position for the filter GUI
            Height = _refBox.Height;

            if (_grid == null)
                return;

            var rowHeadersWidth = _grid.RowHeadersVisible ? _grid.RowHeadersWidth : 0;
            _lblFilter.Width = rowHeadersWidth;

            if (!HasView)
            {
                //provide a dummy representation when nothing is set
                //this allows better desing time support
                _refBox.Visible = true;
                _refBox.Left = rowHeadersWidth + 1;
                _refBox.Width = Width - rowHeadersWidth - 1;

                return;
            }
            _refBox.Visible = false;

            _filterFactory.BeginGridFilterCreation();
            try
            {
                for (var i = 0; i < _grid.Columns.Count; i++)
                {
                    var column = _grid.Columns[i];
                    var dataType = column.ValueType;
                    //create a filter
                    var gridFilter = _filterFactory.CreateGridFilter(column);
                    if (!gridFilter.UseCustomFilterPlacement)
                    {
                        //adjust the vertical positions
                        gridFilter.FilterControl.Top = 0;
                        gridFilter.FilterControl.Height = Height;
                        gridFilter.FilterControl.Visible = false;
                        //add the GUI element to our controls collection
                        Controls.Add(gridFilter.FilterControl);
                        gridFilter.FilterControl.BringToFront();
                    }
                    //notification needed when the filter settings are changed
                    gridFilter.Changed += OnFilterChanged;
                    gridFilter.FilterControl.KeyPress += OnFilterControlKeyPress;
                    gridFilter.FilterControl.Leave += OnFilterControlLeave;
                    //added to hash to provider fast access
                    _columnToGridFilterHash.Add(column, gridFilter);

                    OnGridFilterBound(new GridFilterEventArgs(column, gridFilter));
                }
            }
            finally
            {
                _filterFactory.EndGridFilterCreation();
            }
            if (_keepFilters && _keepFiltersHash.ContainsKey(GetTableName()))
                SetFilters((string[]) _keepFiltersHash[GetTableName()]);

            RepositionGridFilters();
        }

        private void RepositionGridFilters()
        {
            if (_initCounter > 0)
                return;

            if (_grid == null ||
                _grid.Columns == null ||
                _grid.Columns.Count == 0)
                return;

            try
            {
                SuspendLayout();

                var rowHeadersWidth = _grid.RowHeadersVisible ? _grid.RowHeadersWidth : 0;
                var filterWidth = _grid.RowHeadersVisible ? _grid.RowHeadersWidth - 1 : 0;
                var curPos = rowHeadersWidth;

                if (filterWidth > 0)
                {
                    _lblFilter.Width = filterWidth;
                    _lblFilter.Visible = true;
                    curPos++;
                    if (base.RightToLeft == RightToLeft.Yes)
                    {
                        if (_lblFilter.Dock != DockStyle.Right)
                            _lblFilter.Dock = DockStyle.Right;
                    }
                    else
                    {
                        if (_lblFilter.Dock != DockStyle.Left)
                            _lblFilter.Dock = DockStyle.Left;
                    }
                }
                else
                {
                    if (_lblFilter.Visible)
                        _lblFilter.Visible = false;
                }

                //this loop goes through all column styles and iteratively sets
                //their horizontal positions and widths
                var sortedColumns = SortedColumns;
                for (var i = 0; i < sortedColumns.Count; i++)
                {
                    var column = sortedColumns[i];

                    var gridFilter = _columnToGridFilterHash[column] as IGridFilter;
                    if (gridFilter != null &&
                        !gridFilter.UseCustomFilterPlacement)
                    {
                        if (!column.Visible)
                        {
                            if (gridFilter.FilterControl.Visible)
                                gridFilter.FilterControl.Visible = false;
                            continue;
                        }
                        var from = curPos - _grid.HorizontalScrollingOffset;
                        var width = column.Width + (i == 0 ? 1 : 0);

                        if (from < rowHeadersWidth)
                        {
                            width -= rowHeadersWidth - from;
                            from = rowHeadersWidth;
                        }

                        if (from + width > Width)
                            width = Width - from;

                        if (width < 4)
                        {
                            if (gridFilter.FilterControl.Visible)
                                gridFilter.FilterControl.Visible = false;
                        }
                        else
                        {
                            if (base.RightToLeft == RightToLeft.Yes)
                                from = Width - from - width;

                            if (gridFilter.FilterControl.Left != from ||
                                gridFilter.FilterControl.Width != width)
                                gridFilter.FilterControl.SetBounds(from, 0, width, 0, BoundsSpecified.X | BoundsSpecified.Width);

                            if (!gridFilter.FilterControl.Visible)
                                gridFilter.FilterControl.Visible = true;
                        }
                    }
                    curPos += column.Width + (i == 0 ? 1 : 0);
                }
            }
            finally
            {
                ResumeLayout();
            }

            RecreateRowFilter();
            Invalidate();
        }

        private void RecreateRowFilter()
        {
            RecreateRowFilter(false);
        }

        private void RecreateRowFilter(bool ignoreAutoRefresh)
        {
            if (_autoRefreshMode == RefreshMode.Off &&
                !ignoreAutoRefresh)
                return;

            if (_refreshDisabled ||
                (string.IsNullOrEmpty(_customFilter) && _columnToGridFilterHash.Count == 0) ||
                _initCounter > 0)
                return;

            try
            {
                string rowFilter;
                var operatorString = _operator == LogicalOperators.And ? " AND " : " OR ";

                switch (_operator)
                {
                    case LogicalOperators.And:
                    case LogicalOperators.Or:
                        rowFilter = "";

                        foreach (var column in SortedColumns)
                        {
                            //ask every column for the set filter and concatenate them if needed
                            var gridFilter = _columnToGridFilterHash[column] as IGridFilter;
                            if (gridFilter == null)
                                return;
                            if (gridFilter.HasFilter &&
                                column.Visible)
                            {
                                var filter = gridFilter.GetFilter(string.Format("[{0}]", column.DataPropertyName));
                                rowFilter += ((rowFilter.Length > 0 && filter.Length > 0) ? operatorString : "") + filter;
                            }
                        }
                        break;

                    default:
                        rowFilter = "";
                        break;
                }

                var baseFilter = CurrentTableBaseFilter;
                var hasBaseFilter = baseFilter != null && baseFilter.Length > 0;
                if (hasBaseFilter && _baseFilterEnabled)
                {
                    operatorString = _baseFilterOperator == LogicalOperators.And ? " AND " : " OR ";
                    if (rowFilter.Length > 0)
                        rowFilter = "(" + rowFilter + ")" + operatorString + "(" + CurrentTableBaseFilter + ")";
                    else
                        rowFilter += CurrentTableBaseFilter;
                }

                if (!string.IsNullOrEmpty(_customFilter))
                {
                    if (string.IsNullOrEmpty(rowFilter))
                    {
                        rowFilter = _customFilter;
                    }
                    else
                    {
                        rowFilter = "( " + rowFilter + " ) AND ( " + _customFilter + " )";
                    }
                }

                if (_lastRowFilter != rowFilter)
                {
                    _lastRowFilter = rowFilter;
                    SetRowFilter(rowFilter);
                }
            }
            catch (Exception exc)
            {
                var text = GetMessageFromMode(ConsoleErrorMode, exc);
                if (text.Length > 0)
                    Console.WriteLine(text);
                text = GetMessageFromMode(MessageErrorMode, exc);
                if (text.Length > 0)
                    MessageBox.Show(text, "Filter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (_keepFilters && HasView)
                _keepFiltersHash[GetTableName()] = GetFilters();
        }

        private string GetMessageFromMode(FilterErrorModes mode, Exception exc)
        {
            var result = "";

            if ((mode & FilterErrorModes.General) == FilterErrorModes.General)
                result += "Invalid filter specified.";
            if ((mode & FilterErrorModes.ExceptionMessage) == FilterErrorModes.ExceptionMessage)
                result += (result.Length > 0 ? "\n" : "") + exc.Message;
            if ((mode & FilterErrorModes.StackTrace) == FilterErrorModes.StackTrace)
                result += (result.Length > 0 ? "\n" : "") + exc.StackTrace;

            return result;
        }

        private void OnFilterFactoryChanged(object sender, EventArgs e)
        {
            RecreateGridFilters();
        }

        private void OnDataSourceChanged(object sender, EventArgs e)
        {
            _lastRowFilter = "";

            //this probably looks weird but the DataSourceChanged event of the grid
            //must complete before calling RecreateGridFilters, otherwise the DataGridView
            //has some real nasty behaviour (e.g. showing only 3 lines although
            //the view has 100 lines)
            if (_grid.Handle.ToInt32() > 0)
                _grid.BeginInvoke(new MethodInvoker(RecreateGridFilters));
        }

        private void OnGridScroll(object sender, ScrollEventArgs e)
        {
            if (e.ScrollOrientation == ScrollOrientation.HorizontalScroll)
                RepositionGridFilters();
        }

        private void OnColumnStyleWidthChanged(object sender, EventArgs e)
        {
            RepositionGridFilters();
        }

        private void OnGridColumnsChanged(object sender, DataGridViewColumnEventArgs e)
        {
            RepositionGridFilters();
        }

        private void OnGridColumnsAddedRemoved(object sender, DataGridViewColumnEventArgs e)
        {
            RecreateGridFilters();
        }

        private void OnGridColumnsStateChanged(object sender, DataGridViewColumnStateChangedEventArgs e)
        {
            if (e.StateChanged == DataGridViewElementStates.Visible)
                RepositionGridFilters();
        }

        private void OnFilterChanged(object sender, EventArgs e)
        {
            if (_autoRefreshMode == RefreshMode.OnInput)
                RecreateRowFilter();
        }

        private void OnFilterControlLeave(object sender, EventArgs e)
        {
            if (_autoRefreshMode == RefreshMode.OnLeave ||
                _autoRefreshMode == RefreshMode.OnEnterOrLeave)
            {
                RefreshFilters();
            }
        }

        private void OnFilterControlKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r' &&
                (_autoRefreshMode == RefreshMode.OnEnter ||
                 _autoRefreshMode == RefreshMode.OnEnterOrLeave))
            {
                RefreshFilters();
            }
        }

        /// <summary>
        ///     Raises the <see cref="BeforeFiltersChanging" /> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnBeforeFiltersChanging(EventArgs e)
        {
            if (BeforeFiltersChanging != null)
                BeforeFiltersChanging(this, e);
        }

        /// <summary>
        ///     Raises the <see cref="AfterFiltersChanged" /> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnAfterFiltersChanged(EventArgs e)
        {
            if (AfterFiltersChanged != null)
                AfterFiltersChanged(this, e);
        }

        /// <summary>
        ///     Raises the <see cref="GridFilterBound" /> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnGridFilterBound(GridFilterEventArgs e)
        {
            if (GridFilterBound != null)
                GridFilterBound(this, e);
        }

        /// <summary>
        ///     Raises the <see cref="GridFilterUnbound" /> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnGridFilterUnbound(GridFilterEventArgs e)
        {
            if (GridFilterUnbound != null)
                GridFilterUnbound(this, e);
        }
    }
}