
using System.Text.Json;

namespace Filter.Common
{
    // this is common interface for the untrusted code which we would like to wrap in WASI sandbox
    public interface IFilter
    {
        bool Apply(JsonElement pubSubEvent);
    }
}