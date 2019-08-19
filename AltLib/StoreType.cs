namespace AltLib
{
    /// <summary>
    /// The types of command store that can be created.
    /// </summary>
    public enum StoreType
    {
        /// <summary>
        /// An undefined type.
        /// </summary>
        None = 0,

        /// <summary>
        /// Type name for creating an instance of <see cref="FileStore"/>
        /// </summary>
        File,

        /// <summary>
        /// Type name for creating an instance of <see cref="SQLiteStore"/>
        /// </summary>
        SQLite,

        /// <summary>
        /// Type name for creating an instance of <see cref="MemoryStore"/>
        /// </summary>
        Memory
    }
}
