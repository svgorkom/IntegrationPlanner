using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using IntegrationPlanner.Jira;

namespace IntegrationPlanner.UI.Services;

// Keeps the Jira API token in the Windows Credential Manager so it never lands in a file or a log.
public sealed class CredentialManagerTokenStore : IJiraTokenStore
{
    private const string TargetName = "IntegrationPlanner/Jira";
    private const uint TypeGeneric = 1;
    private const uint PersistLocalMachine = 2;
    private const int ErrorNotFound = 1168;

    public string? Load()
    {
        if (!CredReadW(TargetName, TypeGeneric, 0, out IntPtr buffer))
        {
            int error = Marshal.GetLastWin32Error();
            return error == ErrorNotFound ? null : throw new Win32Exception(error);
        }

        try
        {
            Credential credential = Marshal.PtrToStructure<Credential>(buffer);
            return credential.CredentialBlob == IntPtr.Zero
                ? null
                : Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / sizeof(char));
        }
        finally
        {
            CredFree(buffer);
        }
    }

    public void Save(string apiToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiToken);
        byte[] blob = Encoding.Unicode.GetBytes(apiToken);
        IntPtr blobPointer = Marshal.AllocHGlobal(blob.Length);
        try
        {
            Marshal.Copy(blob, 0, blobPointer, blob.Length);
            Credential credential = new()
            {
                Type = TypeGeneric,
                TargetName = TargetName,
                CredentialBlobSize = (uint)blob.Length,
                CredentialBlob = blobPointer,
                Persist = PersistLocalMachine,
                UserName = Environment.UserName,
            };
            if (!CredWriteW(ref credential, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
        finally
        {
            Array.Clear(blob);
            Marshal.FreeHGlobal(blobPointer);
        }
    }

    public void Clear()
    {
        if (!CredDeleteW(TargetName, TypeGeneric, 0) && Marshal.GetLastWin32Error() is int error && error != ErrorNotFound)
        {
            throw new Win32Exception(error);
        }
    }

    [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredReadW(string target, uint type, uint flags, out IntPtr credential);

    [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredWriteW(ref Credential credential, uint flags);

    [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CredDeleteW(string target, uint type, uint flags);

    [DllImport("advapi32.dll")]
    private static extern void CredFree(IntPtr buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Credential
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string Comment;
        public long LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }
}
