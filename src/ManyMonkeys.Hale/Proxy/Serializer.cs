using System;
using System.Reflection;
using Castle.DynamicProxy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ManyMonkeys.Hale.Proxy
{
    public interface IHaleObject
    {
        string SerializeProxy();
    }

    public static class Serializer
    {
        public static T DeserializeAsProxy<T>(this string wire) where T : class, IHaleObject
        {
            var obj = JsonConvert.DeserializeObject(wire);

            var generator = new ProxyGenerator();
            var proxy = generator.CreateInterfaceProxyWithoutTarget<T>(new HaleObjectInterceptor((JObject)obj));

            return proxy; 
        }
    }

    public class HaleObjectInterceptor : IInterceptor
    {
        private readonly JObject _obj;

        public HaleObjectInterceptor(JObject obj)
        {
            _obj = obj;
        }

        public void Intercept(IInvocation invocation)
        {
            string GetPropertySerializationName(out PropertyInfo property)
            {
                var name = invocation.Method.Name.Substring(4);
                var declaringType = invocation.Method.DeclaringType;
                property = declaringType.GetProperty(name);
                var attr = property.GetCustomAttribute<JsonPropertyAttribute>();
                if (attr != null)
                {
                    name = attr.PropertyName;
                }

                return name;
            }

            if (invocation.Method.IsSpecialName && invocation.Method.Name.StartsWith("get_"))
            {
                var name = GetPropertySerializationName(out var property);
                invocation.ReturnValue = Convert.ChangeType(_obj[name], property.PropertyType);
            }

            if (invocation.Method.IsSpecialName && invocation.Method.Name.StartsWith("set_"))
            {
                var name = GetPropertySerializationName(out var property);
                _obj.Add(name, new JValue(invocation.Arguments[0]));
            }

            if (invocation.Method.Name == "SerializeProxy")
            {
                invocation.ReturnValue = JsonConvert.SerializeObject(_obj);
            }
        }
    }
}
