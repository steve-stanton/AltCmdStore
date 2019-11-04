using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using NLog;

namespace AltLib
{
    /// <summary>
    /// Wrapper on an instance of <see cref="SQLiteConnection"/>
    /// </summary>
    /// <remarks>This class is utilized by <see cref="SQLiteDatabase.GetCommandFactory"/> to help avoid
    /// premature disposal of the connection. If you call <c>GetConnection</c> while a transaction
    /// is running, you get back a wrapper on a non-disposable connection. If there's no transaction,
    /// you get back a disposable wrapper.
    /// <para/>
    /// If we worked with a plain <c>SQLiteConnection</c>, it's <c>Dispose</c> method would get
    /// called when the application hits the end of the <c>using</c> block. By using the wrapper,
    /// we can control whether the connection will be disposed of or not.
    /// </remarks>
    class SQLiteCommandFactory : IDbCommandFactory
    {
        static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Global instance counter (for use in debugging)
        /// </summary>
        static uint InstanceCount = 0;

        /// <summary>
        /// An ID for this instance (for use in debugging)
        /// </summary>
        uint InstanceId { get; }

        /// <summary>
        /// The database connection
        /// </summary>
        internal SQLiteConnection Connection { get; }

        /// <summary>
        /// Should the connection be disposed of by the <see cref="Dispose"/> method?
        /// </summary>
        bool IsDisposable { get; }

        /// <summary>
        /// Creates a new instance of <see cref="SQLiteCommandFactory"/> that wraps the supplied connection.
        /// </summary>
        /// <param name="c">The connection to wrap (not null).</param>
        /// <param name="isDisposable">Should the connection be disposed of by the
        /// <see cref="Dispose"/> method? Specify <c>false</c> if the connection is being
        /// used by an enclosing transaction.</param>
        /// <exception cref="ArgumentNullException">An undefined connection was supplied</exception>
        internal SQLiteCommandFactory(SQLiteConnection c, bool isDisposable)
        {
            InstanceId = ++InstanceCount;
            Connection = c ?? throw new ArgumentNullException();
            IsDisposable = isDisposable;
            //Log.Trace($"Created connection {InstanceId} Transaction={!IsDisposable}");
        }

        /// <summary>
        /// Disposes of the connection, so long as it was tagged as disposable when this
        /// wrapper was instantiated.
        /// </summary>
        /// <remarks>If a transaction has been started, you must explicitly call
        /// <see cref="Complete"/> to dispose of the connection.</remarks>
        public void Dispose()
        {
            if (IsDisposable)
            {
                Connection.Dispose();
                //Log.Trace($"Connection {InstanceId} disposed");
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="IDbCommand"/> for the supplied command text.
        /// </summary>
        /// <param name="sql">The command text to be executed</param>
        /// <returns>A command object that holds the supplied command text, and associated
        /// with the database connection.</returns>
        IDbCommand IDbCommandFactory.GetCommand(string sql)
        {
            if (Connection.State != ConnectionState.Open)
            {
                //Log.Trace($"Opening connection {InstanceId}");
                Connection.Open();
            }

            Log.Trace($"SQL.{InstanceId}: {sql}");
            return new SQLiteCommand(sql, Connection);
        }

        /// <summary>
        /// Completes a transaction by disposing of the connection.
        /// </summary>
        void IDbCommandFactory.Complete()
        {
            Debug.Assert(!IsDisposable);
            Connection.Dispose();
            //Log.Trace($"Transaction {InstanceId} completed");
        }
    }
}
