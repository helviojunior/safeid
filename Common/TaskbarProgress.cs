/*
Adicionar as seguintes referências
PresentationCore
PresentationFramework
System.Xaml
WindowsBase
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAPICodePack.Taskbar;

namespace IAM.Taskbar
{
    public enum TaskbarProgressState
    {
        NoProgress = 0,
        Indeterminate = 1,
        Normal = 2,
        Error = 4,
        Paused = 8,
    }

    public static class TaskbarProgress
    {
        public static void SetProgressState(TaskbarProgressState state)
        {
            try
            {
                if (TaskbarManager.IsPlatformSupported)
                    TaskbarManager.Instance.SetProgressState((TaskbarProgressBarState)((Int32)state));
            }
            catch { }
        }

        public static void SetProgressValue(Int32 currentValue, Int32 maximumValue, IntPtr windowHandle)
        {
            try
            {
                if (TaskbarManager.IsPlatformSupported)
                    TaskbarManager.Instance.SetProgressValue(currentValue, maximumValue, windowHandle);
            }
            catch { }
        }
    }
}
