using System.Collections.Generic;
using System.Data;

namespace AltLib
{
    /// <summary>
    /// Query to select all rows from the Properties table.
    /// </summary>
    class PropertiesQuery : IDataQuery<KeyValuePair<string, string>>
    {
        public string Text => "SELECT Name,Value FROM Properties";

        public KeyValuePair<string, string> CreateInstance(IDataReader reader)
        {
            return new KeyValuePair<string, string>(reader.GetString(0), reader.GetString(1));
        }
    }
}
