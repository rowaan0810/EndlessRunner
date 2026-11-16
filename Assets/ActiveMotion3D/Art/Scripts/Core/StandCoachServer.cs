using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ActiveMotion3D.EndlessRunner.Core
{
    internal sealed class StandCoachServer : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct StartupInfo
        {
            public uint Size;
            public IntPtr Reserved, Desktop, Title;
            public uint X, Y, XSize, YSize, XCount, YCount, Fill, Flags;
            public ushort ShowWindow, ReservedSize;
            public IntPtr ReservedBytes, StandardInput, StandardOutput, StandardError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessInfo
        {
            public IntPtr Process, Thread;
            public uint ProcessId, ThreadId;
        }

        [DllImport("kernel32", EntryPoint = "CreateProcessW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateProcess(string application, StringBuilder commandLine,
            IntPtr processAttributes, IntPtr threadAttributes, [MarshalAs(UnmanagedType.Bool)] bool inheritHandles,
            uint flags, IntPtr environment, string currentDirectory, ref StartupInfo startup, out ProcessInfo process);

        [DllImport("kernel32")]
        private static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

        [DllImport("kernel32")]
        private static extern bool TerminateProcess(IntPtr handle, uint exitCode);

        [DllImport("kernel32")]
        private static extern bool CloseHandle(IntPtr handle);

        private IntPtr handle;

        public bool IsRunning => handle != IntPtr.Zero && WaitForSingleObject(handle, 0) == 0x102;

        public StandCoachServer(string executable, string arguments)
        {
            const uint createNoWindow = 0x08000000;
            var command = new StringBuilder($"\"{executable}\" {arguments}");
            var startup = new StartupInfo { Size = (uint)Marshal.SizeOf<StartupInfo>() };
            // Windows IL2CPP cannot reliably start this bundled executable through Process.Start.
            if (!CreateProcess(executable, command, IntPtr.Zero, IntPtr.Zero, false, createNoWindow,
                IntPtr.Zero, Path.GetDirectoryName(executable), ref startup, out ProcessInfo process))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            handle = process.Process;
            CloseHandle(process.Thread);
        }

        public void Dispose()
        {
            if (handle == IntPtr.Zero) return;
            if (IsRunning) TerminateProcess(handle, 0);
            CloseHandle(handle);
            handle = IntPtr.Zero;
        }
    }
}
