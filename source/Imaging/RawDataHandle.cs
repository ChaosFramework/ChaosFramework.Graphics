using System;
using System.Runtime.InteropServices;

namespace ChaosFramework.Graphics.Imaging
{
    public class RawDataHandle
        : IDisposable
    {
        public readonly IntPtr firstElementAddress;
        readonly GCHandle gcHandle;

        public static RawDataHandle Create<T>(T[] arr)
            where T: struct
            => new RawDataHandle(GCHandle.Alloc(arr, GCHandleType.Pinned), Marshal.UnsafeAddrOfPinnedArrayElement(arr, 0));

        private RawDataHandle(GCHandle gcHandle, IntPtr firstElementAddress)
        {
            this.gcHandle = gcHandle;
            this.firstElementAddress = firstElementAddress;
        }

        void IDisposable.Dispose()
            => gcHandle.Free();
    }
}
