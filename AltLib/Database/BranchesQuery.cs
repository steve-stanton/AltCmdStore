using System.Data;
using Newtonsoft.Json;

namespace AltLib
{
    /// <summary>
    /// Query to select all rows from the Branches table.
    /// </summary>
    class BranchesQuery : IDataQuery<BranchInfo>
    {
        /// <summary>
        /// The SQL query text that should be used to retrieve data.
        /// </summary>
        string IDataQuery<BranchInfo>.Text => "SELECT Data FROM Branches";

        /// <summary>
        /// Creates an instance of the objects produced by the query.
        /// </summary>
        /// <param name="reader">The database reader (positioned on the
        /// row that needs to be parsed).</param>
        /// <returns>The instance that corresponds to the current row in
        /// the supplied reader.</returns>
        BranchInfo IDataQuery<BranchInfo>.CreateInstance(IDataReader reader)
        {
            string data = reader.GetString(0);
            return JsonConvert.DeserializeObject<BranchInfo>(data);
        }
    }
}
