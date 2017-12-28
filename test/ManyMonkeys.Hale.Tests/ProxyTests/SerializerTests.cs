using System;
using System.Runtime.Serialization;
using ManyMonkeys.Hale.Proxy;
using Newtonsoft.Json;
using Xunit;

namespace ManyMonkeys.Hale.Tests.ProxyTests
{
    
    public class SerializerTests
    {
        public interface IUser : IHaleObject
        {
            [JsonProperty(PropertyName = "name")]
            string Name { get; set; }

            //[JsonProperty(PropertyName = "age")]
            int? Age { get; set; }

            [JsonProperty(PropertyName = "mentalAge")]
            int MentalAge { get; set; }
        }

        [Fact]
        public void CanDeserializeAsProxy()
        {
            var source = "{\"name\":\"Arthur Dent\"}";
            var user = source.DeserializeAsProxy<IUser>();

            Assert.Equal("Arthur Dent", user.Name);
        }

        [Fact]
        public void CanSerializeProxy()
        {
            var source = "{\"name\":\"Arthur Dent\"}";
            var user = source.DeserializeAsProxy<IUser>();
            var wire = user.SerializeProxy();
            Assert.Equal(source, wire);
        }

        [Fact]
        public void CanSerializeModifiedProxy()
        {
            var fmtSource = "{{\"name\":\"Arthur Dent\"{0}}}";
            var user = string.Format(fmtSource, "").DeserializeAsProxy<IUser>();

            var wire = user.SerializeProxy();
            Assert.Equal(string.Format(fmtSource, ""), wire);

            user.Age = 42;

            wire = user.SerializeProxy();
            Assert.Equal(string.Format(fmtSource, ",\"Age\":42"), wire);
        }

        [Fact]
        public void CanReadPropertyAsDefaultValue()
        {
            var source = "{\"name\":\"Arthur Dent\"}";
            var user = source.DeserializeAsProxy<IUser>();
            Assert.Equal(0, user.GetValueOrDefault(u => u.MentalAge));
        }

        [Fact]
        public void CanReadNullablePropertyAsDefaultValue()
        {
            var source = "{\"name\":\"Arthur Dent\"}";
            var user = source.DeserializeAsProxy<IUser>();
            Assert.Null(user.GetValueOrDefault(u => u.Age));
        }

        [Fact]
        public void CanCheckIfPropertyIsReferenced()
        {
            var source = "{\"name\":\"Arthur Dent\"}";
            var user = source.DeserializeAsProxy<IUser>();
            Assert.True(user.IsReferenced(u => u.Name));
        }

        [Fact]
        public void CanCheckIfPropertyIsUnreferenced()
        {
            var source = "{\"name\":\"Arthur Dent\"}";
            var user = source.DeserializeAsProxy<IUser>();
            Assert.False(user.IsReferenced(u => u.Age));
        }
    }
}
