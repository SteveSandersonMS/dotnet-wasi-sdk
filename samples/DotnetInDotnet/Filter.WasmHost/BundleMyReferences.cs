using System.Text.Json;

namespace Dummy
{
    // this dummy class is here to make sure that wasm SDK linker knows which DLLs we need bundled
    public class BundleMyReferences
    {
        public bool Apply(ReadOnlyMemory<byte> eventUtf8Json)
        {
            try
            {
                var doc = JsonDocument.Parse(eventUtf8Json);
                var root = doc.RootElement;

                return root.TryGetProperty("dummy", out var productContext)
                    && productContext.ValueKind == JsonValueKind.String
                    && productContext.GetString() == "dummy";
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}