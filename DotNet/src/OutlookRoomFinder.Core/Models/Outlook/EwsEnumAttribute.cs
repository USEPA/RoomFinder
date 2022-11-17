using System;

namespace OutlookRoomFinder.Core.Models.Outlook
{
    /// <summary>
    /// EwsEnumAttribute decorates enum values with the name that should be used for the
    /// enumeration value in the schema.
    /// If this is used to decorate an enumeration, be sure to add that enum type to the dictionary in EwsUtilities.cs
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    internal sealed class EwsEnumAttribute : Attribute
    {
        /// <summary>
        /// The name for the enum value used in the server protocol
        /// </summary>
        private readonly string schemaName;

        /// <summary>
        /// Initializes a new instance of the <see cref="EwsEnumAttribute"/> class.
        /// </summary>
        /// <param name="schemaName">Thename used in the protocol for the enum.</param>
        internal EwsEnumAttribute(string schemaName)
        {
            this.schemaName = schemaName;
        }

        /// <summary>
        /// Gets the name of the name used for the enum in the protocol.
        /// </summary>
        /// <value>The name of the name used for the enum in the protocol.</value>
        internal string SchemaName
        {
            get
            {
                return this.schemaName;
            }
        }
    }
}
