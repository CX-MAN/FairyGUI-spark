#if CLIENT
using System.Drawing;
using SCEFGUI.Core;
using SCEFGUI.Utils;

namespace SCEFGUI.UI;

public class FGUIImage : FGUIObject, IColorGear
{
    private Color _color = Color.White;
    private FlipType _flip = FlipType.None;
    private FillMethod _fillMethod = FillMethod.None;
    private int _fillOrigin;
    private float _fillAmount = 1;
    private bool _fillClockwise = true;

    public Color Color { get => _color; set { _color = value; UpdateDisplay(); } }
    public FlipType Flip { get => _flip; set { _flip = value; UpdateDisplay(); } }
    public FillMethod FillMethod { get => _fillMethod; set { _fillMethod = value; UpdateDisplay(); } }
    public int FillOrigin { get => _fillOrigin; set { _fillOrigin = value; UpdateDisplay(); } }
    public float FillAmount { get => _fillAmount; set { _fillAmount = Math.Clamp(value, 0, 1); UpdateDisplay(); } }
    public bool FillClockwise { get => _fillClockwise; set { _fillClockwise = value; UpdateDisplay(); } }

    public override void ConstructFromResource()
    {
        SourceWidth = PackageItem?.Width ?? 0;
        SourceHeight = PackageItem?.Height ?? 0;
        InitWidth = SourceWidth;
        InitHeight = SourceHeight;
        if (_width == 0 && _height == 0)
            SetSize(SourceWidth, SourceHeight);
    }

    public override void Setup_BeforeAdd(ByteBuffer buffer, int beginPos)
    {
        base.Setup_BeforeAdd(buffer, beginPos);
        buffer.Seek(beginPos, 5);
        if (buffer.ReadBool()) _color = buffer.ReadColor();
        _flip = (FlipType)buffer.ReadByte();
        _fillMethod = (FillMethod)buffer.ReadByte();
        if (_fillMethod != FillMethod.None)
        {
            _fillOrigin = buffer.ReadShort();
            _fillClockwise = buffer.ReadBool();
            _fillAmount = buffer.ReadFloat();
        }
    }

    protected virtual void UpdateDisplay()
    {
        // 确保原生控件已创建
        if (NativeObject == null)
            Render.SCERenderContext.Instance.CreateNativeControl(this);

        if (NativeObject == null) return;
        
        var adapter = Render.SCERenderContext.Instance.Adapter;
        if (adapter == null) return;

        // 应用颜色着色（Tint）- 这是图片叠加颜色，不是背景色
        // 使用专门的SetTintColor方法，如果SCE不支持会fallback
        if (_color != Color.White)
        {
            adapter.SetTintColor(NativeObject, _color);
        }
        else
        {
            // 重置为白色（无着色）
            adapter.SetTintColor(NativeObject, Color.White);
        }

        // Flip（翻转）- 使用Scale负值模拟
        float flipScaleX = (_flip == FlipType.Horizontal || _flip == FlipType.Both) ? -1 : 1;
        float flipScaleY = (_flip == FlipType.Vertical || _flip == FlipType.Both) ? -1 : 1;
        
        // 组合用户设置的缩放和翻转
        adapter.SetScale(NativeObject, flipScaleX * _scaleX, flipScaleY * _scaleY);

        // FillMethod（填充方法）- 用于进度条效果
        if (_fillMethod != FillMethod.None && _fillAmount < 1.0f)
        {
            // 简单实现：使用裁剪模拟填充效果
            // 水平填充：从左到右或从右到左
            // 垂直填充：从上到下或从下到上
            // 注意：这是一个简化实现，不支持radial填充
            if (_fillMethod == FillMethod.Horizontal)
            {
                float clipWidth = _width * _fillAmount;
                // TODO: 需要SCE支持裁剪区域设置
                Game.Logger.LogInformation($"[FGUI] Image '{Name}' FillMethod.Horizontal: amount={_fillAmount}");
            }
            else if (_fillMethod == FillMethod.Vertical)
            {
                float clipHeight = _height * _fillAmount;
                Game.Logger.LogInformation($"[FGUI] Image '{Name}' FillMethod.Vertical: amount={_fillAmount}");
            }
            else
            {
                Game.Logger.LogWarning($"[FGUI] Image '{Name}' FillMethod {_fillMethod} not supported in SCE");
            }
        }
    }
    
    public void UpdateGear(int index)
    {
        // Gear4 是颜色相关
        if (index == 4)
        {
            UpdateDisplay();
        }
    }
}

public class FGUIMovieClip : FGUIImage
{
    private float _interval;
    private bool _swing;
    private float _repeatDelay;
    private bool _playing = true;
    private int _frame;
    private int _frameCount;

    public bool Playing { get => _playing; set => _playing = value; }
    public int Frame { get => _frame; set => _frame = Math.Clamp(value, 0, Math.Max(_frameCount - 1, 0)); }
    public int FrameCount => _frameCount;
    public float Interval { get => _interval; set => _interval = value; }
    public bool Swing { get => _swing; set => _swing = value; }
    public float RepeatDelay { get => _repeatDelay; set => _repeatDelay = value; }

    public override void ConstructFromResource()
    {
        base.ConstructFromResource();
        if (PackageItem?.RawData != null)
        {
            var buffer = PackageItem.RawData;
            buffer.Seek(0, 0);
            _interval = buffer.ReadInt() / 1000f;
            _swing = buffer.ReadBool();
            _repeatDelay = buffer.ReadInt() / 1000f;
            buffer.Seek(0, 1);
            _frameCount = buffer.ReadShort();
        }
    }
}

public enum GraphType { Empty, Rect, Ellipse, Polygon, RegularPolygon }

public class FGUIGraph : FGUIObject, IColorGear
{
    private GraphType _type;
    private Color _fillColor = Color.Transparent;
    private Color _lineColor = Color.Black;
    private int _lineSize = 1;
    private float[] _cornerRadius = new float[4];
    private PointF[]? _polygonPoints;
    
    public GraphType Type => _type;
    public Color FillColor { get => _fillColor; set { _fillColor = value; UpdateGraphDisplay(); } }
    public Color LineColor { get => _lineColor; set { _lineColor = value; UpdateGraphDisplay(); } }
    public int LineSize { get => _lineSize; set { _lineSize = value; UpdateGraphDisplay(); } }
    public Color Color { get => _fillColor; set => FillColor = value; }

    public override void Setup_BeforeAdd(ByteBuffer buffer, int beginPos)
    {
        base.Setup_BeforeAdd(buffer, beginPos);
        buffer.Seek(beginPos, 5);

        int type = buffer.ReadByte();
        _type = (GraphType)type;
        
        if (type != 0)
        {
            _lineSize = buffer.ReadInt();
            _lineColor = buffer.ReadColor();
            _fillColor = buffer.ReadColor();
            bool roundedRect = buffer.ReadBool();
            if (roundedRect)
            {
                for (int i = 0; i < 4; i++)
                    _cornerRadius[i] = buffer.ReadFloat();
            }

            if (type == 3) // Polygon
            {
                int cnt = buffer.ReadShort() / 2;
                _polygonPoints = new PointF[cnt];
                for (int i = 0; i < cnt; i++)
                    _polygonPoints[i] = new PointF(buffer.ReadFloat(), buffer.ReadFloat());
            }
            else if (type == 4) // RegularPolygon
            {
                buffer.ReadShort(); // sides
                buffer.ReadFloat(); // startAngle
                int cnt = buffer.ReadShort();
                for (int i = 0; i < cnt; i++)
                    buffer.ReadFloat(); // distances
            }
            
            // Apply the shape drawing after parsing - this triggers display updates
            if (_width > 0 && _height > 0)
            {
                Game.Logger.LogInformation($"[FGUI] Graph '{Name}' Setup_BeforeAdd: type={_type}, size=({_width}x{_height}), fillColor={_fillColor}");
            }
        }
    }
    
    public void DrawRect(float width, float height, int lineSize, Color lineColor, Color fillColor)
    {
        _type = GraphType.Rect;
        SetSize(width, height);
        _lineSize = lineSize;
        _lineColor = lineColor;
        _fillColor = fillColor;
        UpdateGraphDisplay();
    }
    
    public void DrawEllipse(float width, float height, Color fillColor)
    {
        _type = GraphType.Ellipse;
        SetSize(width, height);
        _fillColor = fillColor;
        UpdateGraphDisplay();
    }
    
    public void Clear()
    {
        _type = GraphType.Empty;
        _fillColor = Color.Transparent;
        UpdateGraphDisplay();
    }
    
    private void UpdateGraphDisplay()
    {
        if (NativeObject == null)
            Render.SCERenderContext.Instance.CreateNativeControl(this);
        if (NativeObject != null)
        {
            Render.SCERenderContext.Instance.Adapter?.SetBackgroundColor(NativeObject, _fillColor);
            if (_type == GraphType.Ellipse)
                Render.SCERenderContext.Instance.Adapter?.SetCornerRadius(NativeObject, Math.Min(_width, _height) / 2);
            else if (_type == GraphType.Rect && _cornerRadius[0] > 0)
                Render.SCERenderContext.Instance.Adapter?.SetCornerRadius(NativeObject, _cornerRadius[0]);
        }
    }
}
#endif
