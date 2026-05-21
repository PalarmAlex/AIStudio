using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AIStudio.Common
{
  /// <summary>
  /// IFileSaveDialog без FOS_FILEMUSTEXIST — можно подтвердить несуществующий путь из поля «Папка».
  /// </summary>
  internal static class NativeFolderDialogInterop
  {
    private const uint FOS_PICKFOLDERS = 0x00000020;
    private const uint FOS_FORCEFILESYSTEM = 0x00000040;
    private const uint FOS_FILEMUSTEXIST = 0x00001000;
    private const uint FOS_PATHMUSTEXIST = 0x00000800;
    private const uint SIGDN_FILESYSPATH = 0x80058000;
    private const int HRESULT_ERROR_CANCELLED = unchecked((int)0x800704C7);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    private static extern int SHCreateItemFromParsingName(
        string pszPath,
        IntPtr pbc,
        ref Guid riid,
        out IShellItem ppv);

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    public static bool TryPickFolder(
        IntPtr ownerHwnd,
        string initialPath,
        string title,
        out string selectedPath)
    {
      selectedPath = null;
      IFileDialog dialog = null;

      try
      {
        dialog = (IFileDialog)new FileSaveDialogClass();
        dialog.GetOptions(out uint options);
        options |= FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM;
        options &= ~FOS_FILEMUSTEXIST;
        options &= ~FOS_PATHMUSTEXIST;
        dialog.SetOptions(options);

        if (!string.IsNullOrEmpty(title))
          dialog.SetTitle(title);

        ApplyInitialPath(dialog, initialPath);

        int hr = dialog.Show(ownerHwnd == IntPtr.Zero ? GetActiveWindow() : ownerHwnd);
        if (hr == HRESULT_ERROR_CANCELLED)
          return false;
        if (hr != 0)
          Marshal.ThrowExceptionForHR(hr);

        dialog.GetResult(out IShellItem item);
        item.GetDisplayName(SIGDN_FILESYSPATH, out IntPtr pszPath);
        try
        {
          selectedPath = Marshal.PtrToStringUni(pszPath);
        }
        finally
        {
          if (pszPath != IntPtr.Zero)
            Marshal.FreeCoTaskMem(pszPath);
        }

        return !string.IsNullOrWhiteSpace(selectedPath);
      }
      finally
      {
        if (dialog != null)
          Marshal.ReleaseComObject(dialog);
      }
    }

    private static void ApplyInitialPath(IFileDialog dialog, string initialPath)
    {
      if (string.IsNullOrWhiteSpace(initialPath))
        return;

      string trimmed = initialPath.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
      string parent = Path.GetDirectoryName(trimmed);
      string name = Path.GetFileName(trimmed);

      if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
      {
        Guid shellItemGuid = typeof(IShellItem).GUID;
        if (SHCreateItemFromParsingName(parent, IntPtr.Zero, ref shellItemGuid, out IShellItem folderItem) == 0)
        {
          dialog.SetFolder(folderItem);
          Marshal.ReleaseComObject(folderItem);
        }

        if (!string.IsNullOrEmpty(name))
          dialog.SetFileName(name);
      }
      else
      {
        dialog.SetFileName(trimmed);
      }
    }

    [ComImport]
    [Guid("C0B4E2F3-BA21-4773-8DBF-335EBF4511B7")]
    private class FileSaveDialogClass
    {
    }

    [ComImport]
    [Guid("42f85136-db7e-439c-85f1-e4075d135fc8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileDialog
    {
      [PreserveSig]
      int IsModal();

      [PreserveSig]
      int GetModalWindow(out IntPtr phwnd);

      [PreserveSig]
      int Show(IntPtr hwndOwner);

      void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);
      void SetFileTypeIndex(uint iFileType);
      void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
      void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
      void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
      void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
      void GetResult(out IShellItem ppsi);
      void AddPlace(IShellItem psi, uint fdap);
      void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
      void Close(int hr);
      void SetClientGuid(ref Guid guid);
      void ClearClientData();
      void SetFilter(IntPtr pFilter);
      void GetOptions(out uint pdwOptions);
      void SetOptions(uint dwOptions);
      void GetSelectedItems(out IntPtr ppsai);
      void GetFolder(out IShellItem ppsi);
      void GetCurrentSelection(out IShellItem ppsi);
      void SetFolder(IShellItem psi);
      void SetSaveAsItem(IShellItem psi);
      void SetProperties(IntPtr pStore);
    }

    [ComImport]
    [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
      void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
      void GetParent(out IShellItem ppsi);
      void GetDisplayName(uint sigdnName, out IntPtr ppszName);
      void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
      void Compare(IShellItem psi, uint hint, out int piOrder);
    }
  }
}
