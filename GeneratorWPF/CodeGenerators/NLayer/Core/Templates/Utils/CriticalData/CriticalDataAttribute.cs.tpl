using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace {{ core_project_name }}.Utils.CriticalData;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class CriticalDataAttribute : Attribute
{
}

/// <summary>
/// Json Serilaze Ignore Critical Properties for logs ...
/// </summary>
public class IgnoreCriticalDataResolver : DefaultContractResolver
{
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        var props = base.CreateProperties(type, memberSerialization);

        return props.Where(p =>
        {
            if (string.IsNullOrEmpty(p.PropertyName)) return true;

            PropertyInfo? propertyInfo = type.GetProperty(p.PropertyName);
            if (propertyInfo == null) return true;

            return !Attribute.IsDefined(propertyInfo, typeof(CriticalDataAttribute));
        }).ToList();
    }
}
