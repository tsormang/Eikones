using System.IO;
using System.Runtime.InteropServices;

namespace Eikones.Services;

internal static class WindowsShellProperties
{
    private static readonly PropertyKey DateTakenKey = new(
        new Guid("4D77D521-6B80-4CC4-88CF-ECF5F35BAF94"),
        11);

    public static string? TryGetDateTakenDisplay(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        IPropertyStore? propertyStore = null;
        try
        {
            propertyStore = (IPropertyStore)SHGetPropertyStoreFromParsingName(
                path,
                IntPtr.Zero,
                GETPROPERTYSTOREFLAGS.GPS_DEFAULT,
                typeof(IPropertyStore).GUID);

            var dateTakenKey = DateTakenKey;
            propertyStore.GetValue(ref dateTakenKey, out var value);
            try
            {
                return value.vt switch
                {
                    VarEnum.VT_FILETIME => FormatDayMonthYear(DateTime.FromFileTimeUtc(value.filetime).ToLocalTime()),
                    VarEnum.VT_DATE => FormatDayMonthYear(DateTime.FromOADate(value.date)),
                    _ => null
                };
            }
            finally
            {
                PropVariantClear(ref value);
            }
        }
        catch
        {
            return null;
        }
        finally
        {
            if (propertyStore is not null)
            {
                Marshal.ReleaseComObject(propertyStore);
            }
        }
    }

    private static string FormatDayMonthYear(DateTime dateTime) =>
        $"{dateTime.Day:D2}/{dateTime.Month:D2}/{dateTime.Year}";

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    [return: MarshalAs(UnmanagedType.Interface)]
    private static extern object SHGetPropertyStoreFromParsingName(
        string pszPath,
        IntPtr pbc,
        GETPROPERTYSTOREFLAGS flags,
        [MarshalAs(UnmanagedType.LPStruct)] Guid riid);

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant pvar);

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct PropertyKey
    {
        public Guid fmtid;
        public uint pid;

        public PropertyKey(Guid fmtid, uint pid)
        {
            this.fmtid = fmtid;
            this.pid = pid;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PropVariant
    {
        [FieldOffset(0)] public VarEnum vt;
        [FieldOffset(8)] public long filetime;
        [FieldOffset(8)] public double date;
    }

    [ComImport]
    [Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        void GetCount(out uint propertyCount);
        void GetAt(uint propertyIndex, out PropertyKey key);
        void GetValue(ref PropertyKey key, out PropVariant pv);
        void SetValue(ref PropertyKey key, ref PropVariant pv);
        void Commit();
    }

    private enum GETPROPERTYSTOREFLAGS
    {
        GPS_DEFAULT = 0
    }
}
