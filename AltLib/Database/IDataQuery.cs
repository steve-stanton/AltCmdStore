using System;
using System.Data;

namespace AltLib
{
    /// <summary>
    /// A factory for creating objects selected from a database.
    /// </summary>
    /// <typeparam name="T">The type of object created by the query</typeparam>
    interface IDataQuery<out T>
    {
        /// <summary>
        /// The SQL query text that should be used to retrieve data.
        /// </summary>
        string Text { get; }

        /// <summary>
        /// Creates an instance of the objects produced by the query.
        /// </summary>
        /// <param name="reader">The database reader (positioned on the
        /// row that needs to be parsed).</param>
        /// <returns>The instance that corresponds to the current row in
        /// the supplied reader.</returns>
        T CreateInstance(IDataReader reader);
    }
}
