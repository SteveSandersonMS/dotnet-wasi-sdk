using Filter.Common;
using System.Text.Json;

namespace Filter.Custom
{
    // this is the customer code, we expect to have many such filters
    public class Customer1UntrustedFilter : IFilter
    {
        internal static class EventFields
        {
            public const string ProductContext = "productContext";
        }

        public bool Apply(JsonElement pubSubEvent)
        {
            var res = pubSubEvent.TryGetProperty(EventFields.ProductContext, out var productContext)
                && productContext.ValueKind == JsonValueKind.String
                && productContext.GetString() == "FooBar";
            return res;
        }
    }
}