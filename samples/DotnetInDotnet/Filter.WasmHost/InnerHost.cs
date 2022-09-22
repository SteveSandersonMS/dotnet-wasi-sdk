#pragma warning disable CS8618
using System.Text;
using System.Diagnostics.CodeAnalysis;
using Filter.Common;
using System.Runtime.InteropServices;
using Dummy;
using System.Diagnostics;
using System.Text.Json;

namespace Filter.WasmHost
{
    public static class InnerHost
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(BundleMyReferences))]
        public static void Main()
        {
        }

        private static IFilter Filter;
        private static byte[] Buffer;

        public unsafe static IntPtr InstallFilter(byte* asseblyQualifiedFilterNamePtr, int asseblyQualifiedFilterNameLen, int bufferSize)
        {
            var asseblyQualifiedFilterName = Encoding.UTF8.GetString(asseblyQualifiedFilterNamePtr, asseblyQualifiedFilterNameLen);
            var filterType = Type.GetType(asseblyQualifiedFilterName);
            Debug.Assert(filterType != null, "InnerHost.InstallFilter type not found" + asseblyQualifiedFilterName);

            var filter = Activator.CreateInstance(filterType) as IFilter;
            Debug.Assert(filter != null, "InnerHost.InstallFilter failed to create instance" + asseblyQualifiedFilterName);

            Filter = filter;
            Buffer = new byte[bufferSize];

            var pinnedArray = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
            var ptr = pinnedArray.AddrOfPinnedObject();
            return ptr;
        }

        public unsafe static int Apply(int lenght)
        {
            try
            {
                ReadOnlyMemory<byte> eventUtf8Json = new(Buffer, 0, lenght);
                var doc = JsonDocument.Parse(eventUtf8Json);
                GC.Collect();

                return Filter.Apply(doc.RootElement) ? 1 : 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 0;
            }
        }
    }
}
