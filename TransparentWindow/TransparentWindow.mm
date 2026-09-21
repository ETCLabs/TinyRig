#import <Cocoa/Cocoa.h>
#import <QuartzCore/QuartzCore.h>

static NSWindow* GetWindow()
{
  NSWindow* window = [NSApp mainWindow];
  if (window)
    return window;

  return [[NSApp windows] firstObject];
}

extern "C" void SetBottomWindow(int b)
{
  NSWindow* window = GetWindow();
  if (window == nil)
    return;

  window.level = (b == 0) ? NSNormalWindowLevel : (NSWindowLevel)CGWindowLevelForKey(kCGDesktopWindowLevelKey);
}

extern "C" void ApplyTransparency()
{
  NSWindow* window = GetWindow();
  if (window == nil)
    return;

  window.opaque = NO;
  window.backgroundColor = [NSColor clearColor];
  window.hasShadow = NO;
  
  window.styleMask = NSWindowStyleMaskBorderless;

  // Alternative method to NSWindowStyleMaskBorderless
  // window.titlebarAppearsTransparent = YES;
  // window.titleVisibility = NSWindowTitleHidden;
  // window.styleMask |= NSWindowStyleMaskFullSizeContentView;
  // [window standardWindowButton:NSWindowCloseButton].hidden = YES;
  // [window standardWindowButton:NSWindowMiniaturizeButton].hidden = YES;
  // [window standardWindowButton:NSWindowZoomButton].hidden = YES;

  NSView* view = window.contentView;
  view.wantsLayer = YES;
  CALayer* layer = view.layer;
  if (layer != nil)
  {
    layer.opaque = NO;
    layer.backgroundColor = [NSColor clearColor].CGColor;
  }
}

extern "C" int GetMousePos(double* x, double* y, int* buttons)
{
  NSWindow* window = GetWindow();  
  if (window == nil)
  {
    *x = *y = -1;
    return 0;
  }

  NSPoint screenPoint = [NSEvent mouseLocation];
  NSPoint windowPoint = [window convertPointFromScreen:screenPoint];
  NSPoint pixelPoint = [window convertPointToBacking:windowPoint];
  *x = pixelPoint.x;
  *y = pixelPoint.y;

  int old_buttons = *buttons;
  *buttons = [NSEvent pressedMouseButtons];

  if (old_buttons == 0 && *buttons != 0)
  {
    if (NSPointInRect(screenPoint, window.frame))
    {
      SetBottomWindow(false);
      return 1;
    }
  }

  return 0;
}

extern "C" void SetSize(int width, int height, int corner)
{
  NSWindow* window = GetWindow();  
  if (window == nil)
    return;

  NSScreen* screen = window.screen ?: [NSScreen mainScreen];
  if (screen == nil)
    return;

  NSRect desktop = screen.visibleFrame;
  
  // bottom right
  CGFloat x = NSMaxX(desktop) - width;
  CGFloat y = NSMinY(desktop);
  
  if (corner == 1)
  {
    // bottom left
    x = 0;
  }
  else if (corner == 2)
  {
    // top right
    y = NSMaxY(desktop) - height;
  }
  else if (corner == 3)
  {
    // top left
    x = 0;
    y = NSMaxY(desktop) - height;
  }

  [window setFrame:NSMakeRect(x, y, width, height) display:YES animate:NO];
}
