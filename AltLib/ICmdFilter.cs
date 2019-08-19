namespace AltLib
{
    /// <summary>
    /// A filter associated with a collection of <see cref="CmdData"/>
    /// </summary>
    public interface ICmdFilter
    {
        /// <summary>
        /// Is an instance of <see cref="CmdData"/> relevant?
        /// </summary>
        /// <param name="data">The command to be checked.</param>
        /// <returns>True if the command is relevant (i.e. should not be
        /// filtered out). False if the command can be ignored.</returns>
        bool IsRelevant(CmdData data);
    }
}
