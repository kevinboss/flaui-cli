using System.Runtime.InteropServices;
using System.Text;

namespace FlauiCli.Ipc;

/// <summary>
/// Spawns the daemon fully detached from the launching shell. .NET's Process.Start can't do this:
/// when it redirects the child's streams it enables handle inheritance, so the child inherits a
/// copy of the shell's stdout and keeps that pipe open for its whole life, hanging the shell.
/// CreateProcess with DETACHED_PROCESS and bInheritHandles=false gives the daemon no console and
/// no inherited handles, so the shell returns as soon as the client does.
/// </summary>
internal static class NativeSpawn
{
    private const uint DETACHED_PROCESS = 0x00000008;

    public static void SpawnDetached(string fileName, string arguments)
    {
        // CreateProcessW may write into lpCommandLine, so it must be a mutable buffer (StringBuilder).
        var commandLine = new StringBuilder().Append('"').Append(fileName).Append("\" ").Append(arguments);
        var startup = new STARTUPINFO { cb = Marshal.SizeOf<STARTUPINFO>() };

        var ok = CreateProcessW(
            null, commandLine, IntPtr.Zero, IntPtr.Zero,
            bInheritHandles: false, DETACHED_PROCESS, IntPtr.Zero, null,
            ref startup, out var info);

        if (!ok)
            throw new IOException($"Failed to spawn daemon (Win32 error {Marshal.GetLastWin32Error()}).");

        CloseHandle(info.hProcess);
        CloseHandle(info.hThread);
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateProcessW", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateProcessW(
        string? lpApplicationName,
        StringBuilder lpCommandLine,
        IntPtr lpProcessAttributes,
        IntPtr lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        IntPtr lpEnvironment,
        string? lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct STARTUPINFO
    {
        public int cb;
        public string? lpReserved;
        public string? lpDesktop;
        public string? lpTitle;
        public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput, hStdOutput, hStdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }
}
