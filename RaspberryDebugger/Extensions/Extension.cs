using System;

namespace RaspberryDebugger.Extensions
{
    public static class EnumMemberAttributeExtension
    {
        /// <summary>
        /// Gets the type of the attribute for an enumeration value.
        /// </summary>
        /// <typeparam name="T">Specifies the attribute type type.</typeparam>
        /// <param name="value">The enum value.</param>
        /// <returns>The attribute type.</returns>
        public static T GetAttributeOfType<T>(this Enum value) where T : Attribute
        {
            var memInfo    = value.GetType().GetMember(value.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);

            return (attributes.Length > 0) 
                ? (T)attributes[0] 
                : null;
        }
    }
}
