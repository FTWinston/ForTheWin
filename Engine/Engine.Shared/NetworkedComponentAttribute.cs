using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace FTW.Engine.Shared
{
    [System.AttributeUsage(System.AttributeTargets.Class, Inherited=false)]
    public class NetworkedComponentAttribute : Attribute
    {
        public byte UniqueID { get; private set; }

        public NetworkedComponentAttribute(byte uniqueID)
        {
            UniqueID = uniqueID;
        }

        private static IEnumerable<KeyValuePair<byte, Type>> GetNetworkedComponentAttributeTypes()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                foreach (var type in assembly.GetTypes())
                {
                    object[] attributes = type.GetCustomAttributes(typeof(NetworkedComponentAttribute), false);
                    if (attributes.Length > 0)
                        yield return new KeyValuePair<byte, Type>(((NetworkedComponentAttribute)attributes[0]).UniqueID, type);
                }
        }

        public static void BuildTypeLists()
        {
            var typesByID = new SortedList<byte, Type>(); // for the client
            var IDsByType = new SortedList<Type, byte>(); // for the server
            
            foreach (var kvp in GetNetworkedComponentAttributeTypes())
            {
                if (typesByID.ContainsKey(kvp.Key))
                    throw new TypeLoadException(string.Format("The ID NetworkedComponentAttribute ID {0} is used by types {1} and {2} - IDs should be unique to one type.", kvp.Key, typesByID[kvp.Key].FullName, kvp.Value.FullName));

                typesByID.Add(kvp.Key, kvp.Value);
                IDsByType.Add(kvp.Value, kvp.Key);
            }
        }
    }
}
