using System;
using System.Data;

namespace AltLib
{
    /// <summary>
    /// Factory for producing instances of <see cref="IDbCommand"/>
    /// </summary>
    interface IDbCommandFactory : IDisposable
    {
        /// <summary>
        /// Creates an instance of <see cref="IDbCommand"/> for the supplied command text.
        /// </summary>
        /// <param name="sql">The command text to be executed</param>
        /// <returns>A command object that holds the supplied command text, and associated
        /// with the database connection.</returns>
        /// <remarks>When done with the command object, make sure to dispose of it (otherwise
        /// the database may remain locked)</remarks>
        IDbCommand GetCommand(string sql);

        /// <summary>
        /// Completes a transaction.
        /// </summary>
        void Complete();
    }
}
