using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;


/// <summary>
/// A utility class to determine a process parent.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct ProcessUtilities
{
    // These members must match PROCESS_BASIC_INFORMATION
    internal IntPtr Reserved1;
    internal IntPtr PebBaseAddress;
    internal IntPtr Reserved2_0;
    internal IntPtr Reserved2_1;
    internal IntPtr UniqueProcessId;
    internal IntPtr InheritedFromUniqueProcessId;

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ProcessUtilities processInformation, int processInformationLength, out int returnLength);

    [DllImport("kernel32.dll")]
    static extern int GetProcessId(IntPtr handle);

    /// <summary>
    /// Gets the parent process of the current process.
    /// </summary>
    /// <returns>An instance of the Process class.</returns>
    public static Process GetParentProcess()
    {
        return GetParentProcess(Process.GetCurrentProcess().Handle);
    }

    /// <summary>
    /// Gets the parent process of specified process.
    /// </summary>
    /// <param name="id">The process id.</param>
    /// <returns>An instance of the Process class.</returns>
    public static Process GetParentProcess(int id)
    {
        Process process = Process.GetProcessById(id);
        return GetParentProcess(process.Handle);
    }

    /// <summary>
    /// Gets the parent process of a specified process.
    /// </summary>
    /// <param name="handle">The process handle.</param>
    /// <returns>An instance of the Process class.</returns>
    public static Process GetParentProcess(IntPtr handle)
    {
        ProcessUtilities pbi = new ProcessUtilities();
        int returnLength;
        int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out returnLength);
        if (status != 0)
            throw new Win32Exception(status);

        try
        {
            return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
        }
        catch (ArgumentException)
        {
            // not found
            return null;
        }
    }

    /// <summary>
    /// Gets the chield process of the current process.
    /// </summary>
    /// <returns>An instance of the Process class.</returns>
    public static List<Process> GetChieldProcess()
    {
        return GetChieldProcess(Process.GetCurrentProcess().Handle);
    }


    /// <summary>
    /// Gets the chield process of a specified process.
    /// </summary>
    /// <param name="handle">The process handle.</param>
    /// <returns>An instance of the Process class.</returns>
    public static List<Process> GetChieldProcess(IntPtr handle)
    {
        Int32 pid = GetProcessId(handle);
        return GetChieldProcess(pid);
    }


    /// <summary>
    /// Gets the chield process of specified process.
    /// </summary>
    /// <param name="id">The process id.</param>
    /// <returns>An instance of the Process class.</returns>
    public static List<Process> GetChieldProcess(int id)
    {
        List<Process> proc = new List<Process>();

        //Busca os processos filhos
        foreach (Process p in Process.GetProcesses())
            try
            {
                if (GetParentProcess(p.Handle).Id == id)
                    proc.Add(p);
            }
            catch { }

        return proc;

    }

}
