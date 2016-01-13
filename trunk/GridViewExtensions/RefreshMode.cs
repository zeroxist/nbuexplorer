namespace NbuExplorer.GridViewExtensions
{
    /// <summary>
    ///     Modes which determine when the filter criteria get automatically
    ///     applied to the contents of the grid.
    /// </summary>
    public enum RefreshMode
    {
        /// <summary>
        ///     Filters are regenerated on every user input.
        /// </summary>
        OnInput,

        /// <summary>
        ///     Filters are regenerated whenever the user presses Enter while
        ///     the focus is in one of the filter controls.
        /// </summary>
        OnEnter,

        /// <summary>
        ///     Filters are regenerated whenever one of the filter controls
        ///     looses input focus.
        /// </summary>
        OnLeave,

        /// <summary>
        ///     Filters are regenerated whenever one of the filter controls
        ///     looses input focus or the user presses Enter while
        ///     the focus is in one of the filter controls.
        /// </summary>
        OnEnterOrLeave,

        /// <summary>
        ///     No automatic filter generation.
        /// </summary>
        Off
    }
}