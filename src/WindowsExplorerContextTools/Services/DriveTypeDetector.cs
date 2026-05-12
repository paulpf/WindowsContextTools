using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace WindowsExplorerContextTools.Services;

internal static class DriveTypeDetector
{
    private const uint IOCTL_STORAGE_QUERY_PROPERTY = 0x002D1400;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint OPEN_EXISTING = 3;

    public static bool IsSolidStateDrive(string path)
    {
        try
        {
            var root = Path.GetPathRoot(path);

            if (string.IsNullOrEmpty(root) || !char.IsLetter(root[0]))
            {
                return false;
            }

            var driveLetter = root[0];
            var devicePath = $@"\\.\{driveLetter}:";

            using var handle = CreateFile(
                devicePath,
                0,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                return false;
            }

            var query = new STORAGE_PROPERTY_QUERY
            {
                PropertyId = 7, // StorageDeviceSeekPenaltyProperty
                QueryType = 0   // PropertyStandardQuery
            };

            var result = DeviceIoControl(
                handle,
                IOCTL_STORAGE_QUERY_PROPERTY,
                ref query,
                (uint)Marshal.SizeOf(query),
                out DEVICE_SEEK_PENALTY_DESCRIPTOR descriptor,
                (uint)Marshal.SizeOf<DEVICE_SEEK_PENALTY_DESCRIPTOR>(),
                out _,
                IntPtr.Zero);

            if (!result)
            {
                return false;
            }

            return !descriptor.IncursSeekPenalty;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        uint dwIoControlCode,
        ref STORAGE_PROPERTY_QUERY lpInBuffer,
        uint nInBufferSize,
        out DEVICE_SEEK_PENALTY_DESCRIPTOR lpOutBuffer,
        uint nOutBufferSize,
        out uint lpBytesReturned,
        IntPtr lpOverlapped);

    [StructLayout(LayoutKind.Sequential)]
    private struct STORAGE_PROPERTY_QUERY
    {
        public int PropertyId;
        public int QueryType;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] AdditionalParameters;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DEVICE_SEEK_PENALTY_DESCRIPTOR
    {
        public int Version;
        public int Size;
        [MarshalAs(UnmanagedType.U1)]
        public bool IncursSeekPenalty;
    }
}
