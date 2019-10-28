using System;
using System.Data;
using Newtonsoft.Json;

namespace AltLib
{
    /// <summary>
    /// Query to select command data from a branch table.
    /// </summary>
    class CmdDataQuery : IDataQuery<CmdData>
    {
        /// <summary>
        /// The database query to be executed
        /// </summary>
        string QueryText { get; }

        /// <summary>
        /// Creates an instance of <see cref="CmdDataQuery"/>
        /// </summary>
        /// <param name="branchId">The ID of the branch to select from</param>
        /// <param name="minCmd">The sequence number of the earliest command to select</param>
        /// <param name="maxCmd">The sequence number of the latest command to select</param>
        internal CmdDataQuery(Guid branchId, uint minCmd, uint maxCmd)
        {
            QueryText = $"SELECT Data FROM [{branchId}] WHERE [Sequence] BETWEEN {minCmd} AND {maxCmd}"; 
        }

        /// <summary>
        /// The SQL query text that should be used to retrieve data.
        /// </summary>
        string IDataQuery<CmdData>.Text => QueryText;

        /// <summary>
        /// Creates an instance of the objects produced by the query.
        /// </summary>
        /// <param name="reader">The database reader (positioned on the
        /// row that needs to be parsed).</param>
        /// <returns>The instance that corresponds to the current row in
        /// the supplied reader.</returns>
        CmdData IDataQuery<CmdData>.CreateInstance(IDataReader reader)
        {
            string json = reader.GetString(0);
            return JsonConvert.DeserializeObject<CmdData>(json);
        }
    }
}
