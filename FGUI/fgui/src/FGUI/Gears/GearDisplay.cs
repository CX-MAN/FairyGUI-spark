#if CLIENT
using SCEFGUI.Core;
using SCEFGUI.Utils;

namespace SCEFGUI.Gears;

public class GearDisplay : GearBase
{
    public string[]? Pages { get; set; }
    private int _visible;
    private uint _displayLockToken = 1;

    public GearDisplay(FGUIObject owner) : base(owner) { }

    protected override void AddStatus(string? pageId, ByteBuffer buffer) { }
    protected override void Init() { Pages = null; }

    public override void Apply()
    {
        _displayLockToken++;
        if (_displayLockToken == 0) _displayLockToken = 1;

        bool match = Pages == null || Pages.Length == 0 || Array.Exists(Pages, p => p == _controller?.SelectedPageId);
        int oldVisible = _visible;
        if (match)
            _visible = 1;
        else
            _visible = 0;
        
        if (oldVisible != _visible)
            Game.Logger.LogInformation($"[FGUI] GearDisplay.Apply: {_owner.Name}, controller={_controller?.Name}, selectedPageId={_controller?.SelectedPageId}, pages=[{string.Join(",", Pages ?? Array.Empty<string>())}], visible={_visible}");
    }

    public override void UpdateState() { }

    public uint AddLock() { _visible++; return _displayLockToken; }
    public void ReleaseLock(uint token) { if (token == _displayLockToken) _visible--; }
    public bool Connected => _controller == null || _visible > 0;
}

public class GearDisplay2 : GearBase
{
    public string[]? Pages { get; set; }
    public int Condition { get; set; }
    private int _visible;

    public GearDisplay2(FGUIObject owner) : base(owner) { }

    protected override void AddStatus(string? pageId, ByteBuffer buffer) { }
    protected override void Init() { Pages = null; }

    public override void Apply()
    {
        if (Pages == null || Pages.Length == 0 || Array.Exists(Pages, p => p == _controller?.SelectedPageId))
            _visible = 1;
        else
            _visible = 0;
    }

    public override void UpdateState() { }

    public bool Evaluate(bool connected)
    {
        bool v = Pages == null || Pages.Length == 0 || Array.Exists(Pages, p => p == _controller?.SelectedPageId);
        return Condition == 0 ? (connected && v) : (connected || v);
    }
}

public class GearXY : GearBase
{
    public bool PositionsInPercent { get; set; }
    private readonly Dictionary<string, (float x, float y)> _storage = new();
    private readonly Dictionary<string, (float px, float py)> _extStorage = new();
    private (float x, float y) _default;

    public GearXY(FGUIObject owner) : base(owner) { }

    protected override void AddStatus(string? pageId, ByteBuffer buffer)
    {
        var value = (buffer.ReadInt(), buffer.ReadInt());
        if (pageId == null) _default = value;
        else _storage[pageId] = value;
    }

    public void AddExtStatus(string? pageId, ByteBuffer buffer)
    {
        var value = (buffer.ReadFloat(), buffer.ReadFloat());
        if (pageId != null) _extStorage[pageId] = value;
    }

    protected override void Init()
    {
        _default = (_owner.X, _owner.Y);
        _storage.Clear();
    }

    public override void Apply()
    {
        if (_controller?.SelectedPageId != null && _storage.TryGetValue(_controller.SelectedPageId, out var pos))
            _owner.SetXY(pos.x, pos.y);
        else
            _owner.SetXY(_default.x, _default.y);
    }

    public override void UpdateState()
    {
        if (_controller?.SelectedPageId != null)
            _storage[_controller.SelectedPageId] = (_owner.X, _owner.Y);
    }
}

public class GearSize : GearBase
{
    private readonly Dictionary<string, (float w, float h, float sx, float sy)> _storage = new();
    private (float w, float h, float sx, float sy) _default;

    public GearSize(FGUIObject owner) : base(owner) { }

    protected override void AddStatus(string? pageId, ByteBuffer buffer)
    {
        var value = (buffer.ReadInt(), buffer.ReadInt(), buffer.ReadFloat(), buffer.ReadFloat());
        if (pageId == null) _default = value;
        else _storage[pageId] = value;
    }

    protected override void Init()
    {
        _default = (_owner.Width, _owner.Height, _owner.ScaleX, _owner.ScaleY);
        _storage.Clear();
    }

    public override void Apply()
    {
        if (_controller?.SelectedPageId != null && _storage.TryGetValue(_controller.SelectedPageId, out var size))
        {
            _owner.SetSize(size.w, size.h);
            _owner.SetScale(size.sx, size.sy);
        }
        else
        {
            _owner.SetSize(_default.w, _default.h);
            _owner.SetScale(_default.sx, _default.sy);
        }
    }

    public override void UpdateState()
    {
        if (_controller?.SelectedPageId != null)
            _storage[_controller.SelectedPageId] = (_owner.Width, _owner.Height, _owner.ScaleX, _owner.ScaleY);
    }
}

public class GearLook : GearBase
{
    private readonly Dictionary<string, (float alpha, float rotation, bool grayed, bool touchable)> _storage = new();
    private (float alpha, float rotation, bool grayed, bool touchable) _default;

    public GearLook(FGUIObject owner) : base(owner) { }

    protected override void AddStatus(string? pageId, ByteBuffer buffer)
    {
        var value = (buffer.ReadFloat(), buffer.ReadFloat(), buffer.ReadBool(), buffer.ReadBool());
        if (pageId == null) _default = value;
        else _storage[pageId] = value;
    }

    protected override void Init()
    {
        _default = (_owner.Alpha, _owner.Rotation, _owner.Grayed, _owner.Touchable);
        _storage.Clear();
    }

    public override void Apply()
    {
        if (_controller?.SelectedPageId != null && _storage.TryGetValue(_controller.SelectedPageId, out var look))
        {
            _owner.Alpha = look.alpha;
            _owner.Rotation = look.rotation;
            _owner.Grayed = look.grayed;
            _owner.Touchable = look.touchable;
        }
        else
        {
            _owner.Alpha = _default.alpha;
            _owner.Rotation = _default.rotation;
            _owner.Grayed = _default.grayed;
            _owner.Touchable = _default.touchable;
        }
    }

    public override void UpdateState()
    {
        if (_controller?.SelectedPageId != null)
            _storage[_controller.SelectedPageId] = (_owner.Alpha, _owner.Rotation, _owner.Grayed, _owner.Touchable);
    }
}

public class GearColor : GearBase
{
    private readonly Dictionary<string, System.Drawing.Color> _storage = new();
    private System.Drawing.Color _default = System.Drawing.Color.White;

    public GearColor(FGUIObject owner) : base(owner) { }

    protected override void AddStatus(string? pageId, ByteBuffer buffer)
    {
        var color = buffer.ReadColor();
        if (pageId == null) _default = color;
        else _storage[pageId] = color;
    }

    protected override void Init()
    {
        if (_owner is UI.IColorGear cg)
            _default = cg.Color;
        _storage.Clear();
    }

    public override void Apply()
    {
        if (_owner is UI.IColorGear cg)
        {
            if (_controller?.SelectedPageId != null && _storage.TryGetValue(_controller.SelectedPageId, out var color))
                cg.Color = color;
            else
                cg.Color = _default;
        }
    }

    public override void UpdateState()
    {
        if (_controller?.SelectedPageId != null && _owner is UI.IColorGear cg)
            _storage[_controller.SelectedPageId] = cg.Color;
    }
}

public class GearText : GearBase
{
    private readonly Dictionary<string, string> _storage = new();
    private string _default = "";

    public GearText(FGUIObject owner) : base(owner) { }

    protected override void AddStatus(string? pageId, ByteBuffer buffer)
    {
        var text = buffer.ReadS() ?? "";
        if (pageId == null) _default = text;
        else _storage[pageId] = text;
    }

    protected override void Init()
    {
        _default = _owner.Text ?? "";
        _storage.Clear();
    }

    public override void Apply()
    {
        if (_controller?.SelectedPageId != null && _storage.TryGetValue(_controller.SelectedPageId, out var text))
            _owner.Text = text;
        else
            _owner.Text = _default;
    }

    public override void UpdateState()
    {
        if (_controller?.SelectedPageId != null)
            _storage[_controller.SelectedPageId] = _owner.Text ?? "";
    }
}

public class GearIcon : GearBase
{
    private readonly Dictionary<string, string> _storage = new();
    private string _default = "";

    public GearIcon(FGUIObject owner) : base(owner) { }

    protected override void AddStatus(string? pageId, ByteBuffer buffer)
    {
        var icon = buffer.ReadS() ?? "";
        if (pageId == null) _default = icon;
        else _storage[pageId] = icon;
    }

    protected override void Init()
    {
        _default = _owner.Icon ?? "";
        _storage.Clear();
    }

    public override void Apply()
    {
        if (_controller?.SelectedPageId != null && _storage.TryGetValue(_controller.SelectedPageId, out var icon))
            _owner.Icon = icon;
        else
            _owner.Icon = _default;
    }

    public override void UpdateState()
    {
        if (_controller?.SelectedPageId != null)
            _storage[_controller.SelectedPageId] = _owner.Icon ?? "";
    }
}

public class GearAnimation : GearBase
{
    private readonly Dictionary<string, (bool playing, int frame)> _storage = new();
    private (bool playing, int frame) _default;

    public GearAnimation(FGUIObject owner) : base(owner) { }

    protected override void AddStatus(string? pageId, ByteBuffer buffer)
    {
        var value = (buffer.ReadBool(), buffer.ReadInt());
        if (pageId == null) _default = value;
        else _storage[pageId] = value;
    }

    public void AddExtStatus(string? pageId, ByteBuffer buffer)
    {
        // Extension data for animation gear (version >= 6)
    }

    protected override void Init()
    {
        if (_owner is UI.FGUIMovieClip mc)
            _default = (mc.Playing, mc.Frame);
        _storage.Clear();
    }

    public override void Apply()
    {
        if (_owner is UI.FGUIMovieClip mc)
        {
            if (_controller?.SelectedPageId != null && _storage.TryGetValue(_controller.SelectedPageId, out var anim))
            {
                mc.Playing = anim.playing;
                mc.Frame = anim.frame;
            }
            else
            {
                mc.Playing = _default.playing;
                mc.Frame = _default.frame;
            }
        }
    }

    public override void UpdateState()
    {
        if (_controller?.SelectedPageId != null && _owner is UI.FGUIMovieClip mc)
            _storage[_controller.SelectedPageId] = (mc.Playing, mc.Frame);
    }
}

public class GearFontSize : GearBase
{
    private readonly Dictionary<string, int> _storage = new();
    private int _default;

    public GearFontSize(FGUIObject owner) : base(owner) { }

    protected override void AddStatus(string? pageId, ByteBuffer buffer)
    {
        var size = buffer.ReadInt();
        if (pageId == null) _default = size;
        else _storage[pageId] = size;
    }

    protected override void Init()
    {
        if (_owner is UI.FGUITextField tf)
            _default = tf.FontSize;
        _storage.Clear();
    }

    public override void Apply()
    {
        if (_owner is UI.FGUITextField tf)
        {
            if (_controller?.SelectedPageId != null && _storage.TryGetValue(_controller.SelectedPageId, out var size))
                tf.FontSize = size;
            else
                tf.FontSize = _default;
        }
    }

    public override void UpdateState()
    {
        if (_controller?.SelectedPageId != null && _owner is UI.FGUITextField tf)
            _storage[_controller.SelectedPageId] = tf.FontSize;
    }
}
#endif
