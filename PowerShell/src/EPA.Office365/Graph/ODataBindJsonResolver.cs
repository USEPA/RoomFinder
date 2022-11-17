using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Reflection;

namespace EPA.Office365.Graph
{
    internal class ODataBindJsonResolver : CamelCasePropertyNamesContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            var skipEmptyArray = false;
            var originalPropertyName = property.PropertyName;

            if (property.PropertyName.Equals("template_odata_bind", StringComparison.OrdinalIgnoreCase))
            {
                property.PropertyName = "template@odata.bind";
            }
            else if (property.PropertyName.Equals("owners_odata_bind", StringComparison.OrdinalIgnoreCase))
            {
                property.PropertyName = "owners@odata.bind";
                skipEmptyArray = true;
            }
            else if (property.PropertyName.Equals("members_odata_bind", StringComparison.OrdinalIgnoreCase))
            {
                property.PropertyName = "members@odata.bind";
                skipEmptyArray = true;
            }

            if (skipEmptyArray)
            {
                property.ShouldSerialize = instance =>
                {
                    if (instance
                        .GetType()
                        .GetProperty(originalPropertyName)
                        .GetValue(instance, null) is IEnumerable enumerator)
                    {
                        // check to see if there is at least one item in the Enumerable
                        return enumerator.GetEnumerator().MoveNext();
                    }
                    else
                    {
                        // if the enumerable is null, we defer the decision to NullValueHandling
                        return true;
                    }
                };
            }

            return property;
        }
    }
}
