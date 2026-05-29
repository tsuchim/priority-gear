using System.ComponentModel;
using System.Runtime.InteropServices;
using PriorityGear.Core;

namespace PriorityGear.Windows;

public sealed class WindowsCoreTopologyProvider
{
    public IReadOnlyList<PhysicalCoreInfo> GetPhysicalCores()
    {
        int length = 0;
        if (!NativeMethods.GetLogicalProcessorInformationEx(RelationProcessorCore, 0, ref length))
        {
            int error = Marshal.GetLastWin32Error();
            if (error != ErrorInsufficientBuffer)
            {
                throw new Win32Exception(error);
            }
        }

        nint buffer = Marshal.AllocHGlobal(length);
        try
        {
            if (!NativeMethods.GetLogicalProcessorInformationEx(RelationProcessorCore, buffer, ref length))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            List<PhysicalCoreInfo> cores = [];
            int offset = 0;
            while (offset < length)
            {
                nint item = nint.Add(buffer, offset);
                LogicalProcessorInformationEx info = Marshal.PtrToStructure<LogicalProcessorInformationEx>(item);
                ProcessorRelationship relationship = Marshal.PtrToStructure<ProcessorRelationship>(nint.Add(item, 8));
                ulong mask = relationship.GroupMask.Mask;
                CoreEfficiencyClass efficiencyClass = relationship.EfficiencyClass > 0
                    ? CoreEfficiencyClass.Efficiency
                    : CoreEfficiencyClass.Performance;
                ushort group = relationship.GroupCount == 1 ? relationship.GroupMask.Group : (ushort)1;
                cores.Add(new PhysicalCoreInfo(cores.Count, mask, efficiencyClass, group));
                offset += (int)info.Size;
            }

            return cores;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private const int RelationProcessorCore = 0;
    private const int ErrorInsufficientBuffer = 122;

    [StructLayout(LayoutKind.Sequential)]
    private struct LogicalProcessorInformationEx
    {
        public int Relationship;
        public uint Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessorRelationship
    {
        public byte Flags;
        public byte EfficiencyClass;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] Reserved;
        public ushort GroupCount;
        public GroupAffinity GroupMask;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GroupAffinity
    {
        public ulong Mask;
        public ushort Group;
        public ushort Reserved0;
        public ushort Reserved1;
        public ushort Reserved2;
    }

    private static partial class NativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool GetLogicalProcessorInformationEx(int relationshipType, nint buffer, ref int returnedLength);
    }
}
