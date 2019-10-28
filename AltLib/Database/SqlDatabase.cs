using System;
using System.Collections.Generic;
using System.Data;
using System.Transactions;

namespace AltLib
{
    /// <summary>
    /// Access methods for a database that can be accessed using SQL.
    /// </summary>
    abstract class SqlDatabase
    {
        /// <summary>
        /// The database connection string.
        /// </summary>
        protected internal string ConnectionString { get; }

        /// <summary>
        /// The factory for creating database commands for use during a execution of a
        /// multi-statement transaction (null if a transaction is not currently running).
        /// </summary>
        IDbCommandFactory Transaction { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="SqlDatabase"/>
        /// </summary>
        /// <param name="connectionString">The database connection string.</param>
        /// <exception cref="ArgumentNullException">The supplied connection string is undefined</exception>
        protected SqlDatabase(string connectionString)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(ConnectionString));
            Transaction = null;
        }

        /// <summary>
        /// Creates a factory for producing database access commands.
        /// </summary>
        /// <param name="isDisposable">Should the database connection be disposed of by the
        /// <see cref="Dispose"/> method? Specify <c>false</c> if the connection is being
        /// used by an enclosing transaction.</param>
        /// <returns>A wrapper on the connection to use. If a transaction is currently active (as defined via
        /// a prior call to <see cref="ExecuteTransaction"/>), you get back a wrapper on the connection already
        /// associated with the transaction. Otherwise you get a wrapper on a new connection object based on
        /// the value for <see cref="ConnectionString"/>. In either case, the connection should be open at return.
        /// </returns>
        protected internal abstract IDbCommandFactory CreateCommandFactory(bool isDisposable);

        /// <summary>
        /// Obtains a factory for creating database access commands.
        /// </summary>
        /// <returns>A wrapper on the connection to use. If a transaction is currently active (as defined via
        /// a prior call to <see cref="ExecuteTransaction"/>), you get back a wrapper on the connection already
        /// associated with the transaction. Otherwise you get a wrapper on a new connection object based on
        /// the value for <see cref="ConnectionString"/>. In either case, the connection should be open at return.
        /// </returns>
        IDbCommandFactory GetCommandFactory()
        {
            // If a transaction is not running, return a disposable wrapper on a new connection
            // (see remarks for ExecuteTransaction method).

            if (Transaction == null)
                return CreateCommandFactory(true);
            else
                return Transaction;
        }

        /// <summary>
        /// Executes an INSERT, UPDATE, or DELETE statement.
        /// </summary>
        /// <param name="sql">The SQL to execute</param>
        /// <returns>
        /// The number of rows affected by the statement
        /// </returns>
        internal int ExecuteNonQuery(string sql)
        {
            using (IDbCommandFactory c = GetCommandFactory())
            {
                return c.GetCommand(sql).ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Executes a query that is expected to return no more than one row.
        /// </summary>
        /// <typeparam name="T">The type of data in the query field.</typeparam>
        /// <param name="sql">The query text</param>
        /// <param name="defaultValue">The default value to be returned in cases
        /// where nothing was found, or a database null was found.</param>
        /// <returns>The selected value (or <paramref name="defaultValue"/> if not found)</returns>
        /// <remarks>There is no error if the select actually relates to multiple rows
        /// (in that case, you just get the first row that was selected).</remarks>
        /// <seealso cref="ExecuteQuery"/>
        internal T ExecuteSelect<T>(string sql, T defaultValue = default(T)) where T : IConvertible
        {
            using (IDbCommandFactory c = GetCommandFactory())
            {
                object o = c.GetCommand(sql).ExecuteScalar();

                if (o == null || o == DBNull.Value)
                    return defaultValue;

                return (T)Convert.ChangeType(o, typeof(T));
            }
        }

        /// <summary>
        /// Executes a query
        /// </summary>
        /// <typeparam name="T">The type of data produced by the query.</typeparam>
        /// <param name="query">The query to be executed</param>
        /// <returns>The rows (if any) that satisfy the query</returns>
        /// <seealso cref="ExecuteSelect"/>
        internal IEnumerable<T> ExecuteQuery<T>(IDataQuery<T> query)
        {
            using (IDbCommandFactory c = GetCommandFactory())
            {
                using (IDataReader reader = c.GetCommand(query.Text).ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return query.CreateInstance(reader);
                    }
                }
            }
        }

        /// <summary>
        /// Runs a section of code as a multi-statement transaction.
        /// </summary>
        /// <param name="transactionBody">The code to execute within the transaction (may contain
        /// further calls to <see cref="ExecuteTransaction"/>)</param>
        /// <remarks>
        /// The code within the body must obtain database connections via calls to
        /// <see cref="GetCommandFactory"/>.
        /// </remarks>
        internal void ExecuteTransaction(Action transactionBody)
        {
            // Caution: when dealing with SQLite, be aware of
            // https://elegantcode.com/2010/07/02/using-transactionscope-with-sqlite/.
            // The logic of GetCommandFactory should avoid any issue, so long as
            // transactions run in no more than one thread. We could avoid use
            // of TransactionScope by using IDbTransaction instead. That would
            // make it possible to exert more specific control, which could well
            // help with multi-threaded transactions, but I see no immediate need
            // for that sort of data processing.

            using (var ts = new TransactionScope())
            {
                Exec(transactionBody);
                ts.Complete();
            }
        }

        /// <summary>
        /// Executes the body of a multi-statement transaction.
        /// </summary>
        /// <param name="transactionBody">The code to execute within the transaction</param>
        void Exec(Action transactionBody)
        {
            bool connectionCreated = false;

            try
            {
                if (Transaction == null)
                {
                    lock (this)
                    {
                        if (Transaction == null)
                        {
                            Transaction = CreateCommandFactory(false);
                            connectionCreated = true;
                        }
                    }
                }

                transactionBody();
            }

            finally
            {
                if (connectionCreated)
                {
                    Transaction.Dispose();
                    Transaction = null;
                }
            }
        }

        /// <summary>
        /// Checks whether a table contains any rows that satisfy a specific query.
        /// </summary>
        /// <param name="tableName">The name of the table (or view) to be checked.</param>
        /// <param name="whereClause">The query to execute (without any leading "WHERE").
        /// Specify null to check whether the overall table is empty or not.</param>
        /// <returns>True if at least one row satisfies the query</returns>
        internal bool IsExisting(string tableName, string whereClause)
        {
            if (String.IsNullOrEmpty(tableName))
                throw new ArgumentNullException();

            using (IDbCommandFactory c = GetCommandFactory())
            {
                string sql = $"SELECT 1 FROM [{tableName}]";

                if (!String.IsNullOrEmpty(whereClause))
                    sql += $" WHERE {whereClause}";

                if (ExecuteSelect<int>($"SELECT EXISTS ({sql})") == 0)
                    return false;
                else
                    return true;
            }
        }
    }
}
