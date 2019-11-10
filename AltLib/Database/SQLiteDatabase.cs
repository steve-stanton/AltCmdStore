using System;
using System.Data.SQLite;

namespace AltLib
{
    /// <summary>
    /// Access methods for a SQLite database.
    /// </summary>
    class SQLiteDatabase : SqlDatabase
    {
        /// <summary>
        /// The file specification for the SQLite database file.
        /// </summary>
        internal string FileName { get; }

        /// <summary>
        /// Generates the connection string for working with a SQLite database
        /// </summary>
        /// <param name="sqliteFileName">The file specification for the SQLite database file.</param>
        /// <param name="readOnly">Specify true if access will be readonly</param>
        /// <returns>The connection string</returns>
        static string GetConnectionString(string sqliteFileName, bool readOnly)
        {
            if (String.IsNullOrWhiteSpace(sqliteFileName))
                throw new ArgumentNullException();

            var sb = new SQLiteConnectionStringBuilder
            {
                DataSource = sqliteFileName,
                DateTimeFormat = SQLiteDateFormats.ISO8601,
                DateTimeKind = DateTimeKind.Utc,
                JournalMode = SQLiteJournalModeEnum.Wal,
                ReadOnly = readOnly
            };

            return sb.ToString();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLiteDatabase"/> class.
        /// </summary>
        /// <param name="sqliteFileName">The file specification for the SQLite database file.</param>
        /// <param name="readOnly">Specify true if access will be readonly</param>
        internal SQLiteDatabase(string sqliteFileName, bool readOnly = false)
            : base(GetConnectionString(sqliteFileName, readOnly))
        {
            FileName = sqliteFileName;
        }

        /// <summary>
        /// Creates a factory for producing database access commands.
        /// </summary>
        /// <param name="isDisposable">Should the database connection be disposed of by the
        /// <see cref="Dispose"/> method? Specify <c>false</c> if the connection is being
        /// used by an enclosing transaction.</param>
        /// <returns>A wrapper on the connection to use. If a transaction is currently active (as defined via
        /// a prior call to <see cref="SqlDatabase.ExecuteTransaction"/>), you get back a wrapper on the connection
        /// associated with the transaction. Otherwise you get a wrapper on a new connection object based on the
        /// value for <see cref="SqlDatabase.ConnectionString"/>. In either case, the connection is open at return.
        /// </returns>
        protected internal override IDbCommandFactory CreateCommandFactory(bool isDisposable)
        {
            var conn = new SQLiteConnection(ConnectionString);
            return new SQLiteCommandFactory(conn, isDisposable);
        }
    }
}
