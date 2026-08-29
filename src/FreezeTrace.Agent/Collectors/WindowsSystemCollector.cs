using System.Runtime.InteropServices;
using FreezeTrace.Core.Models;

namespace FreezeTrace.Agent.Collectors;

internal sealed class WindowsSystemCollector
{
    private FileTimeSnapshot? _previousCpu;

    public TelemetrySample Collect()
    {
        var cpu = ReadCpuUsage();
        var memory = ReadMemory();
        var network = NetworkCollector.ReadTotals();
        var processes = ProcessCollector.ReadTopProcesses(5);

        return new TelemetrySample(
            DateTimeOffset.UtcNow,
            cpu,
            memory.UsagePercent,
            memory.AvailableBytes,
            memory.TotalBytes,
            network.Received,
            network.Sent,
            ForegroundProcessReader.TryGetForegroundProcessName(),
            processes);
    }

    private double ReadCpuUsage()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
            return 0;

        var now = new FileTimeSnapshot(ToUInt64(idle), ToUInt64(kernel), ToUInt64(user));

        if (_previousCpu is null)
        {
            _previousCpu = now;
            return 0;
        }

        var previous = _previousCpu.Value;
        _previousCpu = now;

        var idleDelta = now.Idle - previous.Idle;
        var kernelDelta = now.Kernel - previous.Kernel;
        var userDelta = now.User - previous.User;
        var total = kernelDelta + userDelta;

        if (total == 0)
            return 0;

        var busy = total - idleDelta;
        return Math.Clamp((double)busy / total * 100.0, 0, 100);
    }

    private static MemorySnapshot ReadMemory()
    {
        var status = new MemoryStatusEx();
        if (!GlobalMemoryStatusEx(status))
            return new MemorySnapshot(0, 0, 0);

        var used = status.ullTotalPhys - status.ullAvailPhys;
        var percent = status.ullTotalPhys == 0
            ? 0
            : (double)used / status.ullTotalPhys * 100;

        return new MemorySnapshot(percent, status.ullAvailPhys, status.ullTotalPhys);
    }

    private static ulong ToUInt64(FILETIME value) =>
        ((ulong)value.dwHighDateTime << 32) | value.dwLowDateTime;

    private readonly record struct FileTimeSnapshot(ulong Idle, ulong Kernel, ulong User);
    private readonly record struct MemorySnapshot(double UsagePercent, ulong AvailableBytes, ulong TotalBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx lpBuffer);

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME
    {
        public uint dwLowDateTime;
        public uint dwHighDateTime;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MemoryStatusEx
    {
        public uint dwLength = (uint)Marshal.SizeOf<MemoryStatusEx>();
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }
}
