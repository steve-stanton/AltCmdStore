using System.Data;
using Newtonsoft.Json;

namespace AltLib
{
    /// <summary>
    /// Query to select all rows from the Branches table.
    /// </summary>
    class BranchesQuery : IDataQuery<AltCmdFile>
    {
        /// <summary>
        /// The SQL query text that should be used to retrieve data.
        /// </summary>
        string IDataQuery<AltCmdFile>.Text => "SELECT Name, Data FROM Branches";

        /// <summary>
        /// Creates an instance of the objects produced by the query.
        /// </summary>
        /// <param name="reader">The database reader (positioned on the
        /// row that needs to be parsed).</param>
        /// <returns>The instance that corresponds to the current row in
        /// the supplied reader.</returns>
        AltCmdFile IDataQuery<AltCmdFile>.CreateInstance(IDataReader reader)
        {
            string name = reader.GetString(0);
            string data = reader.GetString(1);

            var result = JsonConvert.DeserializeObject<AltCmdFile>(data);

            // The FileName property is supposed to contain a "full path" that
            // reflects the branch hierarchy. But that cannot be determined until
            // we have loaded all the branches.
            result.FileName = name;

            return result;
        }
    }
}
