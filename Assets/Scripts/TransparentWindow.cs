using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

public class TransparentWindow : MonoBehaviour
{
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
  const int GWL_STYLE = -16;
  const int GWL_EXSTYLE = -20;
  const int GWLP_WNDPROC = -4;
  const int HWND_BOTTOM = 1;
  const int SPI_GETWORKAREA = 48;
  const uint WS_POPUP = 0x80000000;
  const uint WS_VISIBLE = 0x10000000;
  const uint WS_CLIPSIBLINGS = 0x04000000;
  const int WS_EX_NOACTIVATE = 0x08000000;
  const int WS_EX_TOOLWINDOW = 0x00000080;
  const uint SWP_FRAMECHANGED = 0x0020;
  const int SWP_NOACTIVATE = 0x0010;
  const uint WM_WINDOWPOSCHANGING = 0x0046;

  [StructLayout(LayoutKind.Sequential)]
  struct MARGINS
  {
    public int cxLeftWidth;
    public int cxRightWidth;
    public int cyTopHeight;
    public int cyBottomHeight;
  }

  [StructLayout(LayoutKind.Sequential)]
  struct WINDOWPOS
  {
    public IntPtr hwnd;
    public IntPtr hwndInsertAfter;
    public int x;
    public int y;
    public int cx;
    public int cy;
    public uint flags;
  }

  [StructLayout(LayoutKind.Sequential)]
  struct RECT
  {
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
  }

  [StructLayout(LayoutKind.Sequential)]
  struct POINT
  {
    public int x;
    public int y;
  }

  [DllImport("user32.dll")] static extern IntPtr GetActiveWindow();
  [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);
  [DllImport("user32.dll")] static extern long SetWindowLongPtr(IntPtr hWnd, int nIndex, long dwNewLong);
  [DllImport("user32.dll")] static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, WndProcDelegate bwNewLong);
  [DllImport("user32.dll")] static extern long GetWindowLongPtr(IntPtr hWnd, int nIndex);
  [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
  [DllImport("user32.dll")] static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
  [DllImport("user32.dll")] static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);
  [DllImport("Dwmapi.dll")] static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMarInset);
  [DllImport("user32.dll")] static extern bool GetCursorPos(out POINT p);
  [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr hWnd, out RECT r);
  [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr hWnd, ref POINT p);

  delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

  IntPtr hWnd_ = IntPtr.Zero;
  IntPtr oldWndProc_ = IntPtr.Zero;
  WndProcDelegate newWndProc_;

  // keep window on bottom
  IntPtr HookedWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
  {
    if (msg == WM_WINDOWPOSCHANGING)
    {
      var wp = Marshal.PtrToStructure<WINDOWPOS>(lParam);
      wp.hwndInsertAfter = new IntPtr(HWND_BOTTOM);
      Marshal.StructureToPtr(wp, lParam, false);
    }

    // Always chain to the original proc.
    return CallWindowProc(oldWndProc_, hWnd, msg, wParam, lParam);
  }

  POINT GetPos(int width, int height, int corner)
  {
    POINT pos = new POINT();
    pos.x = 0;
    pos.y = 0;

    RECT desktop = new RECT();
    if (!SystemParametersInfo(SPI_GETWORKAREA, 0, ref desktop, 0))
      return pos;

    // bottom right
    pos.x = desktop.Right - width;
    pos.y = desktop.Bottom - height;

    if (corner == 1)
    {
      // bottom left
      pos.x = desktop.Left;
    }
    else if (corner == 2)
    {
      // top right
      pos.y = desktop.Top;
    }
    else if (corner == 3)
    {
      // top left
      pos.x = desktop.Left;
      pos.y = desktop.Top;
    }

    return pos;
  }

  public void Init(int width, int height, int corner)
  {
    newWndProc_ = HookedWndProc;

    hWnd_ = GetActiveWindow();
    if (hWnd_ == IntPtr.Zero)
      return;

    // no title bar
    SetWindowLong(hWnd_, GWL_STYLE, WS_POPUP | WS_VISIBLE | WS_CLIPSIBLINGS);

    // prevent taskbar icon
    SetWindowLongPtr(hWnd_, GWL_EXSTYLE, GetWindowLongPtr(hWnd_, GWL_EXSTYLE) | WS_EX_TOOLWINDOW);

    // placement & apply style changes
    POINT pos = GetPos(width, height, corner);
    SetWindowPos(hWnd_, new IntPtr(HWND_BOTTOM), pos.x, pos.y, width, height, SWP_FRAMECHANGED | SWP_NOACTIVATE);

    // transparent background
    MARGINS margins = new MARGINS { cxLeftWidth = -1 };
    DwmExtendFrameIntoClientArea(hWnd_, ref margins);

    // replace window proc to keep window on bottom
    oldWndProc_ = SetWindowLongPtr(hWnd_, GWLP_WNDPROC, newWndProc_);
  }

  public void Resize(int width, int height, int corner)
  {
    if (hWnd_ == IntPtr.Zero)
      return;

    POINT pos = GetPos(width, height, corner);
    SetWindowPos(hWnd_, new IntPtr(HWND_BOTTOM), pos.x, pos.y, width, height, SWP_NOACTIVATE);
  }

  void Restore()
  {
    if (hWnd_ == IntPtr.Zero || oldWndProc_ == IntPtr.Zero)
      return;

    SetWindowLongPtr(hWnd_, GWLP_WNDPROC, (long)oldWndProc_);
    hWnd_ = IntPtr.Zero;
    oldWndProc_ = IntPtr.Zero;
  }

  void OnDestroy()
  {
    Restore();
  }

  private void OnApplicationQuit()
  {
    Restore();
  }

  public bool allow_mouse
  {
    get
    {
      if (hWnd_ == IntPtr.Zero)
        return true;

      if (!GetCursorPos(out POINT cursor))
        return true;

      if (!GetClientRect(hWnd_, out RECT client))
        return true;

      POINT tl = new POINT { x = client.Left, y = client.Top };
      POINT br = new POINT { x = client.Right, y = client.Bottom };
      ClientToScreen(hWnd_, ref tl);
      ClientToScreen(hWnd_, ref br);

      return cursor.x >= tl.x && cursor.x < br.x && cursor.y >= tl.y && cursor.y < br.y;
    }
  }

  public bool Tick(ref Vector2 mouse_pos)
  {
    mouse_pos = Mouse.current.position.ReadValue();
    return (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame);
  }

  public bool bottom
  {
    set { }
  }
#elif !UNITY_EDITOR && UNITY_STANDALONE_OSX

  [DllImport("TransparentWindow")] static extern void ApplyTransparency();
  [DllImport("TransparentWindow")] static extern int GetMousePos(out double x, out double y, out int buttons);
  [DllImport("TransparentWindow")] static extern void SetBottomWindow(int b);
  [DllImport("TransparentWindow")] static extern void SetSize(int width, int height, int corner);

  private int buttons_ = 0;
  private Vector2 screen_size_ = Vector2.zero;
  private int reapply_frames_ = 3;

  void Update()
  {
    // deal with metal layer being rebuilt by Unity - reapply window transparency
    if (Screen.width != screen_size_.x || Screen.height != screen_size_.y)
    {
      screen_size_ = new Vector2Int(Screen.width, Screen.height);
      reapply_frames_ = 3;
    }

    if (reapply_frames_ > 0)
    {
      ApplyTransparency();
      --reapply_frames_;
    }
  }

  public void Init(int width, int height, int corner)
  {
    ApplyTransparency();
    Resize(width, height, corner);
    bottom = true;
  }

  public void Resize(int width, int height, int corner)
  {
    SetSize(width, height, corner);
  }

  public bool allow_mouse
  {
    get { return true; }
  }

  public bool Tick(ref Vector2 mouse_pos)
  {
    double x = 0;
    double y = 0;
    int pressed = GetMousePos(out x, out y, out buttons_);
    mouse_pos.x = (float)x;
    mouse_pos.y = (float)y;
    return (pressed != 0);
  }

  public bool bottom
  {
    set { SetBottomWindow(value ? 1 : 0); }
  }
#else
  public void Init(int width, int height, int corner)
  {
  }

  public void Resize(int width, int height, int corner)
  {
  }

  public bool allow_mouse
  {
    get { return true; }
  }

  public bool Tick(ref Vector2 mouse_pos)
  {
    mouse_pos = Mouse.current.position.ReadValue();
    return (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame);
  }

  public bool bottom
  {
    set { }
  }
#endif
}
