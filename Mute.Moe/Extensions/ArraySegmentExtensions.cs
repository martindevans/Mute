using System;
using System.Runtime.InteropServices;

namespace Mute.Moe.Extensions
{
    public static class ArraySegmentExtensions
    {
        /// <summary>
        /// Pin the array and return a pointer to the start of the segment
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="segment"></param>
        /// <returns></returns>
        internal static DisposableHandle Pin<T>(this ArraySegment<T> segment)
            where T : struct
        {
            var handle = GCHandle.Alloc(segment.Array, GCHandleType.Pinned);

            var size = Marshal.SizeOf(typeof(T));
            var ptr = new IntPtr(handle.AddrOfPinnedObject().ToInt64() + segment.Offset * size);

            return new DisposableHandle(ptr, handle);
        }

        internal struct DisposableHandle
            : IDisposable
        {
            private readonly IntPtr _ptr;
            private GCHandle _handle;

            public IntPtr Ptr
            {
                get
                {
                    if (!_handle.IsAllocated)
                        throw new ObjectDisposedException("GC Handle has already been freed");
                    return _ptr;
                }
            }

            internal DisposableHandle(IntPtr ptr, GCHandle handle)
            {
                _ptr = ptr;
                _handle = handle;
            }

            public void Dispose()
            {
                _handle.Free();
            }
        }
    }
}
