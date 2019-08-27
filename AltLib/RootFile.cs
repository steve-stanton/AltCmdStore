using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AltLib
{
    /// <summary>
    /// The content of the file in the root folder of a command store
    /// (holding the ID of the store among other things).
    /// </summary>
    public class RootFile
    {
        /// <summary>
        /// The name of the file that holds the ID of the command store.
        /// </summary>
        public const string Name = ".root";

        /// <summary>
        /// The path of the folder that contains the root file.
        /// </summary>
        [JsonIgnore]
        public string DirectoryName { get; internal set; }

        /// <summary>
        /// The ID of the command store.
        /// </summary>
        public Guid StoreId { get; }

        /// <summary>
        /// The ID of the upstream store (or <see cref="Guid.Empty"/> if
        /// the current store has no upstream because it is not a clone).
        /// </summary>
        public Guid UpstreamId { get; }

        /// <summary>
        /// The location of the upstream store (if any) that should be used
        /// as the default when pushing and fetching.
        /// </summary>
        /// <remarks>
        /// This applies only to clones (it will be undefined if the store is not a clone).
        /// <para/>
        /// When a clone is created, this will be set with an initial value that matches
        /// the value of <see cref="ICloneStore.Origin"/>. It may be changed if the user
        /// later decides to push to a different mirror of the original.
        /// </remarks>
        public string UpstreamLocation { get; }

        /// <summary>
        /// The locations of all versions of the upstream that the clone has pushed to
        /// (along with the time of the last push).
        /// </summary>
        /// <remarks>
        /// This applies only to clones (it will be undefined if the store is not a clone).
        /// <para/>
        /// The key is the location of the upstream store, the value is the
        /// time (UTC) when the last push was made to that location.</remarks>
        public Dictionary<string, DateTime> PushTimes { get; }

        /// <summary>
        /// Creates a new instance of <see cref="RootFile"/>
        /// </summary>
        /// <param name="storeId">The ID of a command store.</param>
        /// <param name="upstreamId">The ID of the upstream store.</param>
        /// <param name="upstreamLocation">The location of the upstream store (if any) that should be used
        /// as the default when pushing and fetching. For stores on the local file system,
        /// this needs to be a full path (relative specs may confuse things)</param>
        /// <param name="pushTimes">The locations of all versions of the upstream that
        /// the clone has pushed to (along with the time of the last push).</param>
        [JsonConstructor]
        internal RootFile(Guid storeId,
                          Guid upstreamId,
                          string upstreamLocation = null,
                          Dictionary<string, DateTime> pushTimes = null)
        {
            StoreId = storeId;
            UpstreamId = upstreamId;
            UpstreamLocation = upstreamLocation;
            PushTimes = pushTimes;
        }
    }
}
