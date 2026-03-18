using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace Kwerty.DviZe.Hid.Win;

internal static class Win32
{
    //
    // cfgmgr32.h
    //

    public const uint CM_GET_DEVICE_INTERFACE_LIST_PRESENT = 0x00000000;
    public const uint CM_LOCATE_DEVNODE_NORMAL = 0x00000000;
    public const uint CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE = 0;
    public const uint CM_NOTIFY_ACTION_DEVICEINTERFACEARRIVAL = 0;
    public const uint CM_NOTIFY_ACTION_DEVICEINTERFACEREMOVAL = 1;
    public const int CR_SUCCESS = 0x00000000;
    public const int CR_BUFFER_SMALL = 0x0000001A;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    public delegate uint CM_NOTIFY_CALLBACK(IntPtr hNotify, IntPtr Context, uint Action, [In] ref byte EventData, int EventDataSize);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    public static extern int CM_Get_Device_Interface_List_Size(out int pulLen, Guid InterfaceClassGuid, IntPtr pDeviceID, uint ulFlags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    public static extern int CM_Get_Device_Interface_List(Guid InterfaceClassGuid, IntPtr pDeviceID, char[] Buffer, int BufferLen, uint ulFlags);

    [DllImport("cfgmgr32.dll")]
    public static extern int CM_Register_Notification([In] ref CM_NOTIFY_FILTER__DeviceInterface pFilter, IntPtr pContext, CM_NOTIFY_CALLBACK pCallback, out SafeCmNotificationHandle pNotifyContext);

    [DllImport("cfgmgr32.dll")]
    public static extern int CM_Unregister_Notification(IntPtr pNotifyContext);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    public static extern int CM_Get_Device_Interface_Property(string pszDeviceInterface, [In] ref DEVPROPKEY PropertyKey, out uint PropertyType, IntPtr PropertyBuffer, [In, Out] ref int PropertyBufferSize, uint ulFlags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    public static extern int CM_Get_DevNode_Property(IntPtr dnDevInst, [In] ref DEVPROPKEY PropertyKey, out uint PropertyType, IntPtr PropertyBuffer, [In, Out] ref int PropertyBufferSize, uint ulFlags);

    [DllImport("cfgmgr32.dll", CharSet = CharSet.Unicode)]
    public static extern int CM_Locate_DevNode(out IntPtr pdnDevInst, string pDeviceID, uint ulFlags);

    // This definition represents a flattened subset of the CM_NOTIFY_FILTER discriminated union,
    // for use when dealing with device interface events (FilterType = CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE).
    // Layout is consistent across x86/x64 because all fields are 4-byte aligned.
    // Size accounts for the largest union member (400 bytes).
    [StructLayout(LayoutKind.Explicit, Size = 416)]
    public struct CM_NOTIFY_FILTER__DeviceInterface
    {
        // Common fields

        [FieldOffset(0)]
        public int cbSize;

        [FieldOffset(4)]
        public uint Flags;

        [FieldOffset(8)]
        public uint FilterType;

        [FieldOffset(12)]
        public uint Reserved;

        // u.DeviceInterface fields

        [FieldOffset(16)]
        public Guid ClassGuid;
    }

    // This definition represents a flattened subset of the CM_NOTIFY_EVENT_DATA discriminated union,
    // for use when dealing with device interface events (FilterType = CM_NOTIFY_FILTER_TYPE_DEVICEINTERFACE).
    // Layout is consistent across x86/x64 because all fields are 4-byte aligned.
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public struct CM_NOTIFY_EVENT_DATA__DeviceInterfaceHeader
    {
        // Common fields

        [FieldOffset(0)]
        public uint FilterType;

        [FieldOffset(4)]
        public uint Reserved;

        // u.DeviceInterface fields

        [FieldOffset(8)]
        public Guid ClassGuid;
    }

    public class SafeCmNotificationHandle() : SafeHandleZeroOrMinusOneIsInvalid(ownsHandle: true)
    {
        protected override bool ReleaseHandle() => CM_Unregister_Notification(handle) == CR_SUCCESS;
    }

    //
    // devpkey.h
    //

    public readonly static DEVPROPKEY DEVPKEY_Device_InstanceId = new(new Guid(0x78c34fc8, 0x104a, 0x4aca, 0x9e, 0xa4, 0x52, 0x4d, 0x52, 0x99, 0x6e, 0x57), 256);

    public readonly static DEVPROPKEY DEVPKEY_Device_ContainerId = new(new Guid(0x8c7ed206, 0x3f8a, 0x4827, 0xb3, 0xab, 0xae, 0x9e, 0x1f, 0xae, 0xfc, 0x6c), 2);

    //
    // devpropdef.h
    //

    public struct DEVPROPKEY(Guid fmtid, uint pid)
    {
        public Guid fmtid = fmtid;

        public uint pid = pid;
    }

    //
    // fileapi.h
    //

    public const uint OPEN_EXISTING = 3;

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    //
    // hidpi.h
    //

    public const int HIDP_STATUS_SUCCESS = 0x00110000;

    //
    // hidsdi.h
    //

    [DllImport("hid.dll")]
    public static extern void HidD_GetHidGuid(out Guid HidGuid);

    [DllImport("hid.dll", SetLastError = true)]
    public static extern bool HidD_GetProductString(SafeFileHandle HidDeviceObject, byte[] Buffer, int BufferLength);

    [DllImport("hid.dll", SetLastError = true)]
    public static extern bool HidD_GetManufacturerString(SafeFileHandle HidDeviceObject, byte[] Buffer, int BufferLength);

    [DllImport("hid.dll", SetLastError = true)]
    public static extern bool HidD_GetSerialNumberString(SafeFileHandle HidDeviceObject, byte[] Buffer, int BufferLength);

    [DllImport("hid.dll", SetLastError = true)]
    public static extern bool HidD_GetAttributes(SafeFileHandle HidDeviceObject, [In, Out] ref HIDD_ATTRIBUTES Attributes);

    [DllImport("hid.dll", SetLastError = true)]
    public static extern bool HidD_GetPreparsedData(SafeFileHandle HidDeviceObject, out SafeHidPreparsedDataHandle PreparsedData);

    [DllImport("hid.dll", SetLastError = true)]
    public static extern bool HidD_FreePreparsedData(IntPtr PreparsedData);

    [DllImport("hid.dll")]
    public static extern int HidP_GetCaps(SafeHidPreparsedDataHandle PreparsedData, out HIDP_CAPS Capabilities);

    [DllImport("hid.dll", SetLastError = true)]
    public static extern bool HidD_GetFeature(SafeFileHandle HidDeviceObject, [In] ref byte ReportBuffer, int ReportBufferLength);

    [DllImport("hid.dll", SetLastError = true)]
    public static extern bool HidD_SetFeature(SafeFileHandle HidDeviceObject, [In] ref byte ReportBuffer, int ReportBufferLength);

    public struct HIDD_ATTRIBUTES
    {
        public int Size;

        public ushort VendorID;

        public ushort ProductID;

        public ushort VersionNumber;
    }

    public struct HIDP_CAPS
    {
        public ushort Usage;

        public ushort UsagePage;

        public ushort InputReportByteLength;

        public ushort OutputReportByteLength;

        public ushort FeatureReportByteLength;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
        public ushort[] Reserved;

        public ushort NumberLinkCollectionNodes;

        public ushort NumberInputButtonCaps;

        public ushort NumberInputValueCaps;

        public ushort NumberInputDataIndices;

        public ushort NumberOutputButtonCaps;

        public ushort NumberOutputValueCaps;

        public ushort NumberOutputDataIndices;

        public ushort NumberFeatureButtonCaps;

        public ushort NumberFeatureValueCaps;

        public ushort NumberFeatureDataIndices;
    }

    public class SafeHidPreparsedDataHandle() : SafeHandleZeroOrMinusOneIsInvalid(ownsHandle: true)
    {
        protected override bool ReleaseHandle() => HidD_FreePreparsedData(handle);
    }

    //
    // WinBase.h
    //

    public const uint FILE_FLAG_OVERLAPPED = 0x40000000;

    //
    // winerror.h
    //

    public const int ERROR_SUCCESS = 0;
    public const int ERROR_ACCESS_DENIED = 5;
    public const int ERROR_SHARING_VIOLATION = 32;
    public const int ERROR_INVALID_PARAMETER = 87;
    public const int ERROR_DEVICE_NOT_CONNECTED = 1167;

    //
    // winnt.h
    //

    public const uint GENERIC_READ = 0x80000000;
    public const uint GENERIC_WRITE = 0x40000000;
    public const uint FILE_SHARE_READ = 0x00000001;
    public const uint FILE_SHARE_WRITE = 0x00000002;
}
