#if CLIENT
using System.Drawing;
using GameUI.Control;
using GameUI.Control.Primitive;
using GameUI.Control.Extensions;
using GameUI.Control.Behavior;
using GameUI.Enum;
using GameCore.ResourceType;
using GameCore.Platform.SDL;

namespace SCEFGUI.Render;

/// <summary>
/// Real SCE adapter implementation using actual SCE UI API
/// </summary>
public class SCEAdapter : ISCEAdapter
{
    // Store TouchBehavior instances to manage lifecycle
    private readonly Dictionary<object, TouchBehavior> _touchBehaviors = new();
    // Store captured pointer buttons per control
    private readonly Dictionary<object, PointerButtons> _capturedPointers = new();
    // Store long press handlers
    private readonly Dictionary<object, Action> _longPressHandlers = new();
    // Store pointer move handlers
    private readonly Dictionary<object, Action<float, float>> _pointerMoveHandlers = new();
    
    public object CreatePanel() => new Panel();
    public object CreateLabel() => new Label();
    public object CreateButton() => new Button();
    public object CreateScrollablePanel() => new Panel();
    public object CreateInput() => new Label();
    public object CreateVirtualizingPanel() => new VirtualizingPanel();
    public object CreateCanvas() => new Canvas();
    
    // Create Canvas for image rendering with atlas region support
    public object CreateCanvas(float width, float height)
    {
        var canvas = new Canvas();
        canvas.Size(width, height);
        return canvas;
    }

    public void SetPosition(object control, float x, float y)
    {
        if (control is Control c)
            c.Position(x, y);
    }

    public void SetSize(object control, float width, float height)
    {
        if (control is Control c)
            c.Size(width, height);
    }

    public void SetVisible(object control, bool visible)
    {
        if (control is Control c)
        {
            if (visible) c.Show();
            else c.Hide();
        }
    }

    public void SetOpacity(object control, float opacity)
    {
        if (control is Control c)
            c.Opacity(opacity);
    }

    public void SetRotation(object control, float rotation)
    {
        // SCE doesn't have direct rotation support for UI controls
        // This would require a custom implementation
    }

    public void SetScale(object control, float scaleX, float scaleY)
    {
        // SCE doesn't have direct scale support for UI controls
        // Could be approximated by adjusting size
    }

    public void SetTouchable(object control, bool touchable)
    {
        if (control is Control c)
        {
            if (touchable) c.Enable();
            else c.Disable();
        }
    }

    public void SetGrayed(object control, bool grayed)
    {
        if (control is Control c)
        {
            if (grayed) c.Disable();
            else c.Enable();
        }
    }

    public void SetBackgroundColor(object control, Color color)
    {
        if (control is Control c)
            c.Background(color);
    }

    public void SetBackgroundImage(object control, string imagePath)
    {
        if (control is Control c && !string.IsNullOrEmpty(imagePath))
        {
            // SCE Control has Image property for setting image path
            c.Image = imagePath;
            Game.Logger.LogInformation($"[FGUI] SetBackgroundImage: {imagePath}");
        }
    }

    public void SetSlicedImage(object control, string imagePath, int left, int right, int top, int bottom)
    {
        if (control is Control c && !string.IsNullOrEmpty(imagePath))
        {
            c.Image = imagePath;
            c.SlicedEdges = new Thickness(left, top, right, bottom);
            Game.Logger.LogInformation($"[FGUI] SetSlicedImage: {imagePath}, edges: L{left} R{right} T{top} B{bottom}");
        }
    }

    public void SetTintColor(object control, Color color)
    {
        // SCE图片着色实现
        // 尝试使用Control的Tint属性或者通过混合颜色实现
        if (control is Control c)
        {
            // 方案1: 如果Control有Tint属性
            // c.Tint = color;
            
            // 方案2: 使用Opacity来模拟白色以外的着色效果
            // 这是一个fallback，不能完美实现着色
            if (color != Color.White)
            {
                // 如果是灰色调（R=G=B），可以用opacity模拟
                float brightness = (color.R + color.G + color.B) / (3f * 255f);
                // c.Opacity(brightness);  // 这会影响整体透明度，不理想
                
                // 最佳做法：记录日志说明需要SCE支持
                Game.Logger.LogInformation($"[FGUI] SetTintColor: {color}, SCE may not fully support image tinting");
            }
        }
    }

    public void SetImageRegion(object control, string atlasPath, RectangleF region, bool rotated)
    {
        if (control is Canvas canvas && !string.IsNullOrEmpty(atlasPath))
        {
            // Use Canvas.DrawImage to draw specific region from atlas
            var image = new Image(atlasPath);
            var destWidth = region.Width;
            var destHeight = region.Height;
            
            canvas.OnRender += (sender, e) =>
            {
                var destRect = new RectangleF(0, 0, destWidth, destHeight);
                canvas.DrawImage(image, region, destRect);
            };
            Game.Logger.LogInformation($"[FGUI] SetImageRegion (Canvas): {atlasPath}, region: {region}, destSize: {destWidth}x{destHeight}");
        }
        else if (control is Control c && !string.IsNullOrEmpty(atlasPath))
        {
            // Fallback: just set the atlas image (will show whole image)
            c.Image = atlasPath;
            Game.Logger.LogInformation($"[FGUI] SetImageRegion (Control): {atlasPath}, region: {region}");
        }
    }

    /// <summary>
    /// Set sliced (nine-patch) image from atlas with proper region cropping
    /// </summary>
    public void SetSlicedImageFromAtlas(object control, string atlasPath, RectangleF spriteRect, 
        int left, int right, int top, int bottom, float destWidth, float destHeight)
    {
        if (control is Canvas canvas && !string.IsNullOrEmpty(atlasPath))
        {
            var image = new Image(atlasPath);
            
            // Sprite source region in atlas
            float sx = spriteRect.X;
            float sy = spriteRect.Y;
            float sw = spriteRect.Width;
            float sh = spriteRect.Height;
            
            // Nine-slice dimensions in source
            float srcCenterW = sw - left - right;
            float srcCenterH = sh - top - bottom;
            
            // Nine-slice dimensions in destination  
            float dstCenterW = destWidth - left - right;
            float dstCenterH = destHeight - top - bottom;
            
            canvas.OnRender += (sender, e) =>
            {
                // Draw 9 slices
                // Row 1: Top-left, Top-center, Top-right
                if (top > 0)
                {
                    if (left > 0)
                        canvas.DrawImage(image, sx, sy, left, top, 0, 0, left, top);
                    if (srcCenterW > 0 && dstCenterW > 0)
                        canvas.DrawImage(image, sx + left, sy, srcCenterW, top, left, 0, dstCenterW, top);
                    if (right > 0)
                        canvas.DrawImage(image, sx + sw - right, sy, right, top, destWidth - right, 0, right, top);
                }
                
                // Row 2: Middle-left, Center, Middle-right
                if (srcCenterH > 0 && dstCenterH > 0)
                {
                    if (left > 0)
                        canvas.DrawImage(image, sx, sy + top, left, srcCenterH, 0, top, left, dstCenterH);
                    if (srcCenterW > 0 && dstCenterW > 0)
                        canvas.DrawImage(image, sx + left, sy + top, srcCenterW, srcCenterH, left, top, dstCenterW, dstCenterH);
                    if (right > 0)
                        canvas.DrawImage(image, sx + sw - right, sy + top, right, srcCenterH, destWidth - right, top, right, dstCenterH);
                }
                
                // Row 3: Bottom-left, Bottom-center, Bottom-right
                if (bottom > 0)
                {
                    if (left > 0)
                        canvas.DrawImage(image, sx, sy + sh - bottom, left, bottom, 0, destHeight - bottom, left, bottom);
                    if (srcCenterW > 0 && dstCenterW > 0)
                        canvas.DrawImage(image, sx + left, sy + sh - bottom, srcCenterW, bottom, left, destHeight - bottom, dstCenterW, bottom);
                    if (right > 0)
                        canvas.DrawImage(image, sx + sw - right, sy + sh - bottom, right, bottom, destWidth - right, destHeight - bottom, right, bottom);
                }
            };
            
            Game.Logger.LogInformation($"[FGUI] SetSlicedImageFromAtlas: {atlasPath}, sprite: {spriteRect}, edges: L{left} R{right} T{top} B{bottom}, dest: {destWidth}x{destHeight}");
        }
        else if (control is Control c && !string.IsNullOrEmpty(atlasPath))
        {
            // Fallback for non-Canvas controls - just set image with SlicedEdges
            c.Image = atlasPath;
            c.SlicedEdges = new Thickness(left, top, right, bottom);
            Game.Logger.LogInformation($"[FGUI] SetSlicedImage (Control): {atlasPath}, edges: L{left} R{right} T{top} B{bottom}");
        }
    }

    public void SetText(object control, string text)
    {
        if (control is Label label)
            label.Text = text;
    }

    public void SetTextColor(object control, Color color)
    {
        if (control is Label label)
            label.TextColor = color;
    }

    public void SetFontSize(object control, int size)
    {
        if (control is Label label)
            label.FontSize = size;
    }

    public void SetBold(object control, bool bold)
    {
        if (control is Label label)
            label.Bold = bold;
    }

    public void SetItalic(object control, bool italic)
    {
        if (control is Label label)
            label.Italic = italic;
    }

    public void SetTextAlign(object control, TextAlign align)
    {
        if (control is Control c)
        {
            var hca = align switch
            {
                TextAlign.Left => HorizontalContentAlignment.Left,
                TextAlign.Center => HorizontalContentAlignment.Center,
                TextAlign.Right => HorizontalContentAlignment.Right,
                _ => HorizontalContentAlignment.Left
            };
            c.HorizontalContentAlignment = hca;
        }
    }

    public void SetTextVerticalAlign(object control, TextVerticalAlign align)
    {
        if (control is Control c)
        {
            var vca = align switch
            {
                TextVerticalAlign.Top => VerticalContentAlignment.Top,
                TextVerticalAlign.Middle => VerticalContentAlignment.Center,
                TextVerticalAlign.Bottom => VerticalContentAlignment.Bottom,
                _ => VerticalContentAlignment.Top
            };
            c.VerticalContentAlignment = vca;
        }
    }
    
    public void SetInputPlaceholder(object control, string placeholder)
    {
        if (control is Label label)
        {
            // SCE Label可能没有Placeholder属性
            // 需要根据实际SCE API调整
            // label.Placeholder = placeholder;
            Game.Logger.LogInformation($"[FGUI] SetInputPlaceholder: {placeholder}");
        }
    }
    
    public void SetInputPassword(object control, bool isPassword)
    {
        if (control is Control c)
        {
            // SCE需要支持密码模式
            // c.PasswordMode = isPassword;
            Game.Logger.LogInformation($"[FGUI] SetInputPassword: {isPassword}");
        }
    }
    
    public void SetInputMaxLength(object control, int maxLength)
    {
        if (control is Control c)
        {
            // c.MaxLength = maxLength;
            Game.Logger.LogInformation($"[FGUI] SetInputMaxLength: {maxLength}");
        }
    }
    
    public void SetInputEditable(object control, bool editable)
    {
        if (control is Control c)
        {
            if (editable)
                c.Enable();
            else
                c.Disable();
        }
    }

    public void SetCornerRadius(object control, float radius)
    {
        if (control is Control c)
            c.CornerRadius(radius);
    }

    public void SetZIndex(object control, int zIndex)
    {
        if (control is Control c)
            c.ZIndex(zIndex);
    }

    public void SetClipContent(object control, bool clip)
    {
        // SCE handles clipping differently
    }

    public void AddChild(object parent, object child)
    {
        if (parent is Control p && child is Control c)
        {
            // Use AlignTop and AlignLeft to ensure absolute positioning from top-left
            c.AlignTop().AlignLeft();
            p.Add(c);
        }
    }

    public void RemoveChild(object parent, object child)
    {
        if (child is Control c)
            c.RemoveFromParent();
    }

    public void AddToRoot(object control)
    {
        if (control is Control c)
        {
            // 方法1：直接设置为屏幕尺寸
            var size = GameUI.Device.ScreenViewport.Primary.Size;
            Game.Logger.LogInformation($"[FGUI] AddToRoot: Screen size = {size.Width}x{size.Height}");
            
            // 设置位置和尺寸
            c.Position(0, 0);
            c.Size(size.Width, size.Height);
            
            // 设置拉伸以适应屏幕变化
            c.Stretch();
            c.GrowRatio(1, 1);
            
            c.Show();
            c.AddToRoot();
            
            Game.Logger.LogInformation($"[FGUI] AddToRoot: Control added to root");
        }
    }
    
    /// <summary>
    /// 添加到根节点，但保持原始尺寸不自动拉伸
    /// </summary>
    public void AddToRootWithFixedSize(object control, float width, float height)
    {
        if (control is Control c)
        {
            c.Size(width, height);
            c.AlignTop().AlignLeft();
            c.Show();
            c.AddToRoot();
        }
    }

    public void RemoveFromParent(object control)
    {
        if (control is Control c)
            c.RemoveFromParent();
    }

    public void OnClick(object control, Action handler)
    {
        if (control is Control c)
            c.Click(handler);
    }

    public void OnPointerEnter(object control, Action handler)
    {
        if (control is Control c)
            c.MouseEnter(handler);
    }

    public void OnPointerLeave(object control, Action handler)
    {
        if (control is Control c)
            c.MouseLeave(handler);
    }

    public void OnPointerPress(object control, Action handler)
    {
        if (control is Control c)
        {
            c.OnPointerPressed += (sender, e) => handler();
        }
    }
    
    public void OnPointerPressWithPosition(object control, Action<float, float> handler)
    {
        if (control is Control c)
        {
            c.OnPointerPressed += (sender, e) =>
            {
                var pos = e.PointerPosition;
                if (pos.HasValue)
                {
                    handler(pos.Value.X, pos.Value.Y);
                }
                else
                {
                    handler(0, 0);
                }
            };
        }
    }

    public void OnPointerRelease(object control, Action handler)
    {
        if (control is Control c)
        {
            c.OnPointerReleased += (sender, e) => handler();
        }
    }

    // ===== Touch Behavior (Gestures) =====
    
    public void EnableTouchBehavior(object control, TouchBehaviorConfig config)
    {
        if (control is Control c)
        {
            // Remove existing if any
            DisableTouchBehavior(control);
            
            // Add new TouchBehavior with configuration
            var tb = c.AddTouchBehaviorWithDuration(
                scaleFactor: config.ScaleFactor,
                pressAnimationDurationMs: config.AnimationDurationMs,
                longPressDurationMs: config.LongPressDurationMs
            );
            
            _touchBehaviors[control] = tb;
            
            // Wire up long press event if handler exists
            if (_longPressHandlers.TryGetValue(control, out var handler))
            {
                tb.LongPressTriggered += (s, e) => handler();
            }
        }
    }
    
    public void DisableTouchBehavior(object control)
    {
        if (_touchBehaviors.TryGetValue(control, out var tb))
        {
            // TouchBehavior is managed by the control's behavior collection
            // Just remove from our tracking dictionary
            _touchBehaviors.Remove(control);
        }
    }
    
    public void OnLongPress(object control, Action handler)
    {
        _longPressHandlers[control] = handler;
        
        // If TouchBehavior already exists, wire up the event
        if (_touchBehaviors.TryGetValue(control, out var tb))
        {
            tb.LongPressTriggered += (s, e) => handler();
        }
    }
    
    public void OnDoubleClick(object control, Action handler)
    {
        // SCE doesn't have built-in double click, implement with timing
        if (control is Control c)
        {
            DateTime lastClick = DateTime.MinValue;
            c.OnPointerClicked += (sender, e) =>
            {
                var now = DateTime.Now;
                if ((now - lastClick).TotalMilliseconds < 300)
                {
                    handler();
                    lastClick = DateTime.MinValue; // Reset to prevent triple-click
                }
                else
                {
                    lastClick = now;
                }
            };
        }
    }
    
    // ===== Pointer Capture (for Drag/Swipe) =====
    
    public void CapturePointer(object control)
    {
        if (control is Control c)
        {
            c.CapturePointer(PointerButtons.Button1);
            _capturedPointers[control] = PointerButtons.Button1;
        }
    }
    
    public void ReleasePointer(object control)
    {
        if (control is Control c && _capturedPointers.TryGetValue(control, out var buttons))
        {
            c.ReleasePointer(buttons);
            _capturedPointers.Remove(control);
        }
    }
    
    public void OnPointerCapturedMove(object control, Action<float, float> handler)
    {
        if (control is Control c)
        {
            _pointerMoveHandlers[control] = handler;
            c.OnPointerCapturedMove += (sender, e) =>
            {
                var pos = e.PointerPosition;
                if (pos.HasValue)
                {
                    handler(pos.Value.X, pos.Value.Y);
                }
            };
        }
    }
    
    // ===== Virtualizing Panel =====
    
    public void SetVirtualizingPanelConfig(object panel, VirtualPanelConfig config)
    {
        if (panel is VirtualizingPanel vp)
        {
            vp.ItemSize = new SizeF(config.ItemWidth, config.ItemHeight);
            vp.ScrollOrientation = config.IsHorizontal ? GameUI.Enum.Orientation.Horizontal : GameUI.Enum.Orientation.Vertical;
            // CachePages handled via CacheLength
            if (config.CachePages > 0)
            {
                vp.CacheLength = new GameUI.Control.Struct.VirtualizationCacheLength(config.CachePages, config.CachePages);
            }
        }
    }
    
    public void SetVirtualizingPanelItems(object panel, int itemCount, Action<int, object> itemRenderer)
    {
        if (panel is VirtualizingPanel vp)
        {
            // Set up the virtualization callback - OnChildVirtualizationPhase 
            // gives us Control + Phase, we can get ItemIndex from Control.ItemIndex
            vp.OnChildVirtualizationPhase += (sender, e) =>
            {
                var control = e.Control;
                var index = control.ItemIndex;
                
                if (index >= 0 && index < itemCount)
                {
                    itemRenderer(index, control);
                }
            };
            
            // Create items list to trigger virtualization
            var items = new List<int>();
            for (int i = 0; i < itemCount; i++)
                items.Add(i);
            vp.ItemsSource = items.Cast<object>();
        }
    }
    
    public void RefreshVirtualizingPanel(object panel)
    {
        if (panel is VirtualizingPanel vp)
        {
            vp.GenerateChildren();
        }
    }

    // ===== Utilities =====
    
    public void Dispose(object control)
    {
        // Clean up TouchBehavior if exists
        DisableTouchBehavior(control);
        
        // Clean up handlers
        _longPressHandlers.Remove(control);
        _pointerMoveHandlers.Remove(control);
        _capturedPointers.Remove(control);
        
        if (control is Control c)
            c.RemoveFromParent();
    }

    public byte[]? LoadTexture(string path) => null;
    
    public SizeF GetScreenSize()
    {
        // 获取实际屏幕尺寸（设备无关像素）
        var size = GameUI.Device.ScreenViewport.Primary.Size;
        return new SizeF(size.Width, size.Height);
    }
}
#endif
