using System; 
using Google.Protobuf; 
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GenericTreeView.SharedTypes
{
    public class ProtoMessageConverter : JsonConverter
    {
        private readonly System.Type protoBaseType = typeof(Google.Protobuf.IMessage);
        private JsonSerializer jsonSerializer = new JsonSerializer();


        public override bool CanConvert(System.Type objectType)
        {
            return typeof(Google.Protobuf.IMessage).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue, JsonSerializer serializer)
        {
            var converter = new ExpandoObjectConverter();
            object o = converter.ReadJson(reader, objectType, existingValue, serializer);
            string text = JsonConvert.SerializeObject(o);

            IMessage message = (IMessage)Activator.CreateInstance(objectType);
            return Google.Protobuf.JsonParser.Default.Parse(text, message.Descriptor);
        }

        public override void WriteJson(JsonWriter writer, object value,JsonSerializer serializer)
        {
            writer.WriteRawValue(Google.Protobuf.JsonFormatter.Default.Format((IMessage)value));
        }
    }
}