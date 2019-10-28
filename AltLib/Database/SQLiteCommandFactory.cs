using System;
using System.Data;
using System.Data.SQLite;

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
        /// <exception cref="ArgumentNullException">If a null data server or null connection was supplied</exception>
        internal SQLiteCommandFactory(SQLiteConnection c, bool isDisposable)
        {
            Connection = c ?? throw new ArgumentNullException();
            IsDisposable = isDisposable;
        }

        /// <summary>
        /// Disposes of the connection, so long as it was tagged as disposable when this
        /// wrapper was instantiated.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposable)
                Connection.Dispose();
        }

        /// <summary>
        /// Creates an instance of <see cref="IDbCommand"/> for the supplied command text.
        /// </summary>
        /// <param name="sql">The command text to be executed</param>
        /// <returns>A command object that holds the supplied command text, and associated
        /// with the database connection.</returns>
        IDbCommand IDbCommandFactory.GetCommand(string sql)
        {
            return new SQLiteCommand(sql, Connection);
        }
    }
}
