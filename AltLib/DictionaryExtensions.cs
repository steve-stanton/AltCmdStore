using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace AltLib
{
    /// <summary>
    /// Extension methods for a dictionary of objects.
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Extracts a property that should be an enum value.
        /// </summary>
        /// <param name="props">The properties to extract the enum value from.</param>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <returns>The property value (or default(T) if not found)</returns>
        public static T GetEnum<T>(this Dictionary<string, object> props, string propertyName)
        {
            if (props.TryGetValue(propertyName, out object o) && o != null)
                return (T)Enum.Parse(typeof(T), o.ToString());
            else
                return default(T);
        }

        /// <summary>
        /// Extracts a property that should be an instance of <see cref="System.Guid"/>
        /// </summary>
        /// <param name="props">The properties to extract the value from.</param>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <returns>The property value (or <see cref="Guid.Empty"/> if not found)</returns>
        public static Guid GetGuid(this Dictionary<string, object> props, string propertyName)
        {
            if (props.TryGetValue(propertyName, out object o) && o != null)
                return Guid.Parse(o.ToString());
            else
                return Guid.Empty;
        }

        /// <summary>
        /// Extracts a property that should be an instance of <see cref="System.DateTime"/>
        /// </summary>
        /// <param name="props">The properties to extract the value from.</param>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <returns>The property value (or <see cref="DateTime.MinValue"/> if not found)</returns>
        public static DateTime GetDateTime(this Dictionary<string, object> props, string propertyName)
        {
            if (props.TryGetValue(propertyName, out object o) && o != null)
                return DateTime.Parse(o.ToString());
            else
                return DateTime.MinValue;
        }

        /// <summary>
        /// Extracts a value from a property dictionary where the value
        /// implements <see cref="IConvertible"/>
        /// </summary>
        /// <typeparam name="T">The type for the property value</typeparam>
        /// <param name="props">The properties to extract the value from.</param>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <param name="defaultValue">The value to be returned (could be null) if the
        /// <paramref name="propertyName"/> is not present in the property dictionary,
        /// or the value found is default(T).</param>
        /// <returns>The property value in the dictionary (or <paramref name="defaultValue"/>
        /// if not found).</returns>
        public static T GetValue<T>( this Dictionary<string, object> props
                                   , string propertyName
                                   , T defaultValue = default(T)) where T : IConvertible
        {
            if (props.TryGetValue(propertyName, out object o) && !Object.Equals(o, default(T)))
                return (T)Convert.ChangeType(o, typeof(T));
            else
                return defaultValue;
        }

        public static T GetObject<T>(this Dictionary<string, object> props
                                    , string propertyName
                                    , T defaultValue = default(T))
        {
            if (!props.TryGetValue(propertyName, out object o))
                return defaultValue;

            if (o != null && o.GetType() == typeof(T))
                return (T)o;

            JObject jo = (o as JObject);
            if (jo == null)
                return defaultValue;
            else
                return jo.ToObject<T>();
        }

        /// <summary>
        /// Extracts an array of items that have been deserialized from JSON.
        /// </summary>
        /// <typeparam name="T">The type for the array elements.</typeparam>
        /// <param name="props">The properties to extract the value from.</param>
        /// <param name="propertyName">The name of the property to get.</param>
        /// <returns>The item array (an empty array if the property was not found)</returns>
        public static T[] GetArray<T>( this Dictionary<string, object> props
                                     , string propertyName)
        {
            if (!props.TryGetValue(propertyName, out object o) || o == null)
                return new T[0];

            // Allow an element that wasn't actually written as an array
            if (o is T)
                return new T[] { (T)o };

            // The property may have not been serialized as yet
            Type ot = o.GetType();
            if (ot.IsArray && typeof(T).IsAssignableFrom(ot.GetElementType()))
                return (T[])o;

            // Handle data that has been deserialized from JSON
            JArray ja = (o as JArray);
            if (ja == null)
                return new T[0];
            else
                return ja.ToObject<T[]>();
        }
    }
}
