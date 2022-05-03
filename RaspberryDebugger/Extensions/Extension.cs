using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace RaspberryDebugger.Extensions
{
    public static class EnumMemberAttributeExtension
    {
        /// <summary>
        /// Gets the type of the attribute of.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumVal">The enum value.</param>
        /// <returns>value</returns>
        public static T GetAttributeOfType<T>(this Enum enumVal) where T : Attribute
        {
            var memInfo = enumVal.GetType().GetMember(enumVal.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);

            return (attributes.Length > 0) 
                ? (T)attributes[0] 
                : null;
        }
    }
}
