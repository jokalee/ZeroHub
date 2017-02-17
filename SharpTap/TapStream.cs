using Microsoft.Win32.SafeHandles;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZeroHub
{
    public class TapStream
    {
        public TapStream(IntPtr Handle)
        {
            mHandle = new SafeFileHandle(Handle, true);
            ThreadPool.BindHandle(mHandle);
        }

        public void Dispose()
        {
            mHandle.Dispose();
        }

        private SafeFileHandle mHandle;

        unsafe public Task<int> ReadAsync(byte[] bytes,int count)
        {
            Overlapped overlapped = new Overlapped(0, 0, IntPtr.Zero, null);
            NativeOverlapped* intOverlapped = null;
            ManualResetEvent complete = new ManualResetEvent(false);
            uint readBytes = 0;
            IOCompletionCallback iocomplete = delegate (uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
            {
                Overlapped.Free(intOverlapped);
                readBytes = numBytes;
                complete.Set();
            };
            intOverlapped = overlapped.UnsafePack(iocomplete, bytes);
            int hr = 0;
            int r = ReadFileNative(mHandle, bytes, count, intOverlapped, out hr);
            return Task.Run(() => { complete.WaitOne(); return (int)readBytes; });
        }

        unsafe public Task WriteAsync(byte[] bytes)
        {
            Overlapped overlapped = new Overlapped(0, 0, IntPtr.Zero, null);
            NativeOverlapped* intOverlapped = null;
            ManualResetEvent complete = new ManualResetEvent(false);
            IOCompletionCallback iocomplete = delegate (uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
            {
                Overlapped.Free(intOverlapped);
                complete.Set();
            };
            intOverlapped = overlapped.UnsafePack(iocomplete, bytes);
            int hr = 0;
            int r = WriteFileNative(mHandle, bytes, bytes.Length, intOverlapped, out hr);
            return Task.Run(() => { complete.WaitOne(); });
        }

        private unsafe int ReadFileNative(SafeFileHandle handle, byte[] bytes, int count, NativeOverlapped* overlapped, out int hr)
        {
            int r = 0;
            int numBytesRead = 0;

            fixed (byte* p = bytes)
            {
                r = Win32Native.ReadFile(handle, p, count, IntPtr.Zero, overlapped);
            }

            if (r == 0)
            {
                hr = Marshal.GetLastWin32Error();
                if (hr == Win32Native.ERROR_INVALID_HANDLE)
                    mHandle.Dispose();
                return -1;
            }
            else
                hr = 0;
            return numBytesRead;
        }

        private unsafe int WriteFileNative(SafeFileHandle handle, byte[] bytes, int count, NativeOverlapped* overlapped, out int hr)
        {
            int numBytesWritten = 0;
            int r = 0;

            fixed (byte* p = bytes)
            {
                r = Win32Native.WriteFile(handle, p, count, IntPtr.Zero, overlapped);
            }

            if (r == 0)
            {
                hr = Marshal.GetLastWin32Error();
                // See code:#errorInvalidHandle in "private long SeekCore(long offset, SeekOrigin origin)".
                if (hr == Win32Native.ERROR_INVALID_HANDLE)
                    mHandle.Dispose();
                return -1;
            }
            else
                hr = 0;
            return numBytesWritten;
        }

    }
}
