using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace NbuExplorer.GridViewExtensions.GridFilters
{
    /// <summary>
    ///     A <see cref="IGridFilter" /> implementation for filtering numeric columns
    ///     with a <see cref="NumericGridFilterControl" /> to control the filter.
    /// </summary>
    public class NumericGridFilter : GridFilterBase
    {
        internal const string IN_BETWEEN = "<x<";
        private const string FILTER_FORMAT_SINGLE = "{0} {1} {2}";
        private const string FILTER_REGEX_SINGLE = @"\[[a-zA-Z].*\] (?<Operator>(<|>|<=|>=|=|<>|)) (?<Value>(\+|-)?[0-9][0-9]*(\.[0-9]*)?)";
        private const string FILTER_FORMAT_BETWEEN = "{0} >= {1} AND {0} <= {2}";

        private const string FILTER_REGEX_BETWEEN =
            @"\[[a-zA-Z].*\] (?<Operator1>(>=)) (?<Value1>(\+|-)?[0-9][0-9]*(\.[0-9]*)?) AND \[[a-zA-Z].*\] (?<Operator2>(<=)) (?<Value2>(\+|-)?[0-9][0-9]*(\.[0-9]*)?)";

        private const string FILTER_FORMAT_STRING = "Convert({0}, 'System.String') LIKE '{1}*'";
        private const string FILTER_REGEX_STRING = @"Convert\(\[[a-zA-Z].*\],\s'System.String'\)\sLIKE\s'(?<Value>(\+|-)?[0-9][0-9]*(\.[0-9]*)?)\*'";
        private readonly NumericGridFilterControl _numericGridFilterControl;

        /// <summary>
        ///     Creates a new instance with <see cref="GridFilterBase.UseCustomFilterPlacement" />
        ///     and <see cref="ShowInBetweenOperator" /> set to false.
        /// </summary>
        public NumericGridFilter() : this(new NumericGridFilterControl(), false, false)
        {
        }

        /// <summary>
        ///     Creates a new instance with <see cref="GridFilterBase.UseCustomFilterPlacement" />
        ///     set to false.
        /// </summary>
        /// <param name="showInBetweenOperator">Determines whether the 'in between' operator is available.</param>
        public NumericGridFilter(bool showInBetweenOperator) : this(new NumericGridFilterControl(), false, showInBetweenOperator)
        {
        }

        /// <summary>
        ///     Creates a new instance with <see cref="GridFilterBase.UseCustomFilterPlacement" />
        ///     set to true and not having the 'in between' operator.
        /// </summary>
        /// <param name="numericGridFilterControl">
        ///     A <see cref="NumericGridFilterControl" />
        ///     instance which should be used by the filter.
        /// </param>
        public NumericGridFilter(NumericGridFilterControl numericGridFilterControl) : this(numericGridFilterControl, true, false)
        {
        }

        /// <summary>
        ///     Creates a new instance with <see cref="GridFilterBase.UseCustomFilterPlacement" />
        ///     set to true and not having the 'in between' operator.
        /// </summary>
        /// <param name="numericGridFilterControl">
        ///     A <see cref="NumericGridFilterControl" />
        ///     instance which should be used by the filter.
        /// </param>
        /// <param name="showInBetweenOperator">Determines whether the 'in between' operator is available.</param>
        public NumericGridFilter(NumericGridFilterControl numericGridFilterControl, bool showInBetweenOperator) : this(numericGridFilterControl, true, showInBetweenOperator)
        {
        }

        private NumericGridFilter(NumericGridFilterControl numericGridFilterControl, bool useCustomFilterPlacement, bool showInBetweenOperator) : base(useCustomFilterPlacement)
        {
            _numericGridFilterControl = numericGridFilterControl;
            _numericGridFilterControl.Changed += OnNumericGridFilterControlChanged;
            ShowInBetweenOperator = showInBetweenOperator;
        }

        /// <summary>
        ///     Sets or gets whether the 'in between' operator should be available.
        /// </summary>
        public bool ShowInBetweenOperator
        {
            get { return _numericGridFilterControl.ComboBox.Items.Contains(IN_BETWEEN); }
            set
            {
                if (value == ShowInBetweenOperator)
                    return;

                if (value)
                {
                    _numericGridFilterControl.ComboBox.Items.Add(IN_BETWEEN);
                }
                else
                {
                    _numericGridFilterControl.ComboBox.Items.Remove(IN_BETWEEN);
                    if (Operator == IN_BETWEEN)
                        _numericGridFilterControl.ComboBox.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        ///     Gets or sets the current text of the first contained <see cref="TextBox" />.
        /// </summary>
        public string Text1
        {
            get { return _numericGridFilterControl.TextBox1.Text; }
            set { _numericGridFilterControl.TextBox1.Text = value; }
        }

        /// <summary>
        ///     Gets or sets the current text of the second contained <see cref="TextBox" />.
        /// </summary>
        public string Text2
        {
            get { return _numericGridFilterControl.TextBox2.Text; }
            set { _numericGridFilterControl.TextBox2.Text = value; }
        }

        /// <summary>
        ///     Gets or sets the current operator of the contained <see cref="ComboBox" />.
        /// </summary>
        public string Operator
        {
            get { return (string) _numericGridFilterControl.ComboBox.SelectedItem; }
            set { _numericGridFilterControl.ComboBox.SelectedItem = value; }
        }

        /// <summary>
        ///     Returns the instance itsself, which contains a <see cref="TextBox" />
        ///     and a <see cref="ComboBox" /> to adjust the filter.
        /// </summary>
        public override Control FilterControl
        {
            get { return _numericGridFilterControl; }
        }

        /// <summary>
        ///     Gets whether a filter is set.
        ///     True, if the text of the <see cref="TextBox" /> is not empty.
        /// </summary>
        public override bool HasFilter
        {
            get { return Text1.Length > 0 && (Operator != IN_BETWEEN || Text2.Length > 0); }
        }

        private void OnNumericGridFilterControlChanged(object sender, EventArgs e)
        {
            OnChanged();
        }

        /// <summary>
        ///     Gets a filter with the current criteria in string representation.
        ///     If operator '*' is set in the <see cref="ComboBox" /> a text criteria
        ///     with like will be created.
        ///     All other operators will do numerical comparisons. If no valid number
        ///     is entered then all rows will be filtered out.
        /// </summary>
        /// <param name="columnName">
        ///     The name of the column for which the criteria should be generated.
        /// </param>
        /// <returns>A string representing the current filter criteria</returns>
        public override string GetFilter(string columnName)
        {
            if (!HasFilter)
                return "";

            if (Operator == "*")
                return string.Format(FILTER_FORMAT_STRING, columnName, Text1);

            try
            {
                if (Operator == IN_BETWEEN)
                {
                    var decimal1 = Text1.Length == 0 ? decimal.MinValue : Convert.ToDecimal(Text1);
                    var decimal2 = Text2.Length == 0 ? decimal.MaxValue : Convert.ToDecimal(Text2);

                    var number1 = decimal1.ToString(CultureInfo.CreateSpecificCulture("en-US"));
                    var number2 = decimal2.ToString(CultureInfo.CreateSpecificCulture("en-US"));

                    return string.Format(FILTER_FORMAT_BETWEEN, columnName, number1, number2);
                }
                var number = Convert.ToDecimal(Text1).ToString(CultureInfo.CreateSpecificCulture("en-US"));
                return string.Format(FILTER_FORMAT_SINGLE, columnName, Operator, number);
            }
            catch
            {
                return columnName + " = " + false;
            }
        }

        /// <summary>
        ///     Sets a string which a a previous result of <see cref="GetFilter" />
        ///     in order to configure the <see cref="FilterControl" /> to match the
        ///     given filter criteria.
        /// </summary>
        /// <param name="filter">filter criteria</param>
        /// <returns></returns>
        public override void SetFilter(string filter)
        {
            var regex = new Regex(FILTER_REGEX_BETWEEN, RegexOptions.ExplicitCapture);
            if (ShowInBetweenOperator && regex.IsMatch(filter))
            {
                var match = regex.Match(filter);
                _numericGridFilterControl.ComboBox.SelectedItem = IN_BETWEEN;

                var decimal1 = Convert.ToDecimal(match.Groups["Value1"].Value, CultureInfo.CreateSpecificCulture("en-US"));
                var decimal2 = Convert.ToDecimal(match.Groups["Value2"].Value, CultureInfo.CreateSpecificCulture("en-US"));

                _numericGridFilterControl.TextBox1.Text = decimal1 == decimal.MinValue ? "" : decimal1.ToString();
                _numericGridFilterControl.TextBox2.Text = decimal2 == decimal.MaxValue ? "" : decimal2.ToString();
            }
            else
            {
                regex = new Regex(FILTER_REGEX_STRING);
                if (regex.IsMatch(filter))
                {
                    var match = regex.Match(filter);
                    Text1 = match.Groups["Value"].Value;
                    Operator = "*";
                }
                else
                {
                    regex = new Regex(FILTER_REGEX_SINGLE);
                    if (regex.IsMatch(filter))
                    {
                        var match = regex.Match(filter);
                        Text1 = Convert.ToDecimal(match.Groups["Value"].Value, CultureInfo.CreateSpecificCulture("en-US")).ToString();
                        Operator = match.Groups["Operator"].Value;
                    }
                }
            }
        }

        /// <summary>
        ///     Clears the filter to its initial state.
        /// </summary>
        public override void Clear()
        {
            _numericGridFilterControl.ComboBox.SelectedIndex = 0;
            Text1 = "";
            Text2 = "";
        }
    }
}