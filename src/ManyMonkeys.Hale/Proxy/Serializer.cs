using System;
using System.Linq.Expressions;
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

        public static TValue GetValueOrDefault<TValue, T>(this T entity, Func<T, TValue> action, 
            TValue defaultValue = default(TValue)) where T : class, IHaleObject
        {
            try
            {
                var value = action(entity);
                return value;
            }
            catch (ReferenceNotFoundException)
            {
            }
            return defaultValue;
        }

        public static bool IsReferenced<TValue, T>(this T entity, Func<T, TValue> action)
            where T : class, IHaleObject
        {
            try
            {
                action(entity);
            }
            catch (ReferenceNotFoundException)
            {
                return false;
            }
            return true;
        }
    }

    public class ReferenceNotFoundException : Exception
    {
        public ReferenceNotFoundException()
        {

        }

        public ReferenceNotFoundException(string message) : base(message)
        {

        }

        public ReferenceNotFoundException(string message, Exception exception) : base(message, exception)
        {

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
                return attr?.PropertyName ?? name;
            }

            if (invocation.Method.IsSpecialName && invocation.Method.Name.StartsWith("get_"))
            {
                var name = GetPropertySerializationName(out var property);
                if (!_obj.TryGetValue(name, out var token))
                {
                    throw new ReferenceNotFoundException(@"Property not found: {name}");
                }
                var value = _obj[name];
                invocation.ReturnValue = Convert.ChangeType(value, property.PropertyType);
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
