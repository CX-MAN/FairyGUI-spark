#if CLIENT
using System.Drawing;
using SCEFGUI.Core;
using SCEFGUI.Event;
using SCEFGUI.Utils;

namespace SCEFGUI.UI;

public enum ButtonMode { Common, Check, Radio }

public class FGUIButton : FGUIComponent, IColorGear
{
    public const string UP = "up";
    public const string DOWN = "down";
    public const string OVER = "over";
    public const string SELECTED_OVER = "selectedOver";
    public const string DISABLED = "disabled";
    public const string SELECTED_DISABLED = "selectedDisabled";

    public bool ChangeStateOnClick { get; set; } = true;

    protected FGUIObject? _titleObject;
    protected FGUIObject? _iconObject;
    protected FGUIController? _relatedController;
    protected string? _relatedPageId;
    protected FGUIController? _buttonController;

    private ButtonMode _mode;
    private bool _selected;
    private string _title = "";
    private string? _icon;
    private string? _selectedTitle;
    private string? _selectedIcon;
    private int _downEffect;
    private float _downEffectValue = 0.8f;
    private bool _downScaled;
    private bool _down;
    private bool _over;

    private EventListener? _onChanged;

    public EventListener OnChanged => _onChanged ??= new EventListener();

    public override string? Icon
    {
        get => _icon;
        set
        {
            _icon = value;
            var val = (_selected && _selectedIcon != null) ? _selectedIcon : _icon;
            if (_iconObject != null) _iconObject.Icon = val;
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            if (_titleObject != null)
                _titleObject.Text = (_selected && _selectedTitle != null) ? _selectedTitle : _title;
        }
    }

    public override string? Text
    {
        get => Title;
        set => Title = value ?? "";
    }

    public string? SelectedIcon
    {
        get => _selectedIcon;
        set
        {
            _selectedIcon = value;
            var val = (_selected && _selectedIcon != null) ? _selectedIcon : _icon;
            if (_iconObject != null) _iconObject.Icon = val;
        }
    }

    public string? SelectedTitle
    {
        get => _selectedTitle;
        set
        {
            _selectedTitle = value;
            if (_titleObject != null)
                _titleObject.Text = (_selected && _selectedTitle != null) ? _selectedTitle : _title;
        }
    }

    public Color TitleColor
    {
        get => GetTextField()?.Color ?? Color.Black;
        set { var tf = GetTextField(); if (tf != null) tf.Color = value; }
    }

    public Color Color
    {
        get => TitleColor;
        set => TitleColor = value;
    }

    public int TitleFontSize
    {
        get => GetTextField()?.FontSize ?? 0;
        set { var tf = GetTextField(); if (tf != null) tf.FontSize = value; }
    }

    public bool Selected
    {
        get => _selected;
        set
        {
            if (_mode == ButtonMode.Common) return;
            if (_selected != value)
            {
                _selected = value;
                SetCurrentState();
                if (_selectedTitle != null && _titleObject != null)
                    _titleObject.Text = _selected ? _selectedTitle : _title;
                if (_selectedIcon != null && _iconObject != null)
                    _iconObject.Icon = _selected ? _selectedIcon : _icon;
                if (_relatedController != null && Parent != null && Parent.BuildingDisplayList == 0)
                {
                    if (_selected)
                        _relatedController.SelectedPageId = _relatedPageId ?? "";
                }
            }
        }
    }

    public ButtonMode Mode
    {
        get => _mode;
        set { if (_mode != value) { if (value == ButtonMode.Common) Selected = false; _mode = value; } }
    }

    public FGUIController? RelatedController
    {
        get => _relatedController;
        set { if (value != _relatedController) { _relatedController = value; _relatedPageId = null; } }
    }

    public string? RelatedPageId { get => _relatedPageId; set => _relatedPageId = value; }

    public FGUITextField? GetTextField()
    {
        if (_titleObject is FGUITextField tf) return tf;
        if (_titleObject is FGUILabel label) return label.GetTextField();
        if (_titleObject is FGUIButton btn) return btn.GetTextField();
        return null;
    }

    protected void SetState(string val)
    {
        if (_buttonController != null)
            _buttonController.SelectedPage = val;

        if (_downEffect == 1)
        {
            float v = _downEffectValue;
            if (val == DOWN || val == SELECTED_OVER || val == SELECTED_DISABLED)
            {
                foreach (var child in _children)
                    if (child is IColorGear cg && child is not FGUITextField)
                        cg.Color = Color.FromArgb(255, (int)(255 * v), (int)(255 * v), (int)(255 * v));
            }
            else
            {
                foreach (var child in _children)
                    if (child is IColorGear cg && child is not FGUITextField)
                        cg.Color = Color.White;
            }
        }
        else if (_downEffect == 2)
        {
            if (val == DOWN || val == SELECTED_OVER || val == SELECTED_DISABLED)
            {
                if (!_downScaled) { _downScaled = true; SetScale(ScaleX * _downEffectValue, ScaleY * _downEffectValue); }
            }
            else
            {
                if (_downScaled) { _downScaled = false; SetScale(ScaleX / _downEffectValue, ScaleY / _downEffectValue); }
            }
        }
    }

    protected void SetCurrentState()
    {
        if (Grayed && _buttonController != null && _buttonController.HasPage(DISABLED))
            SetState(_selected ? SELECTED_DISABLED : DISABLED);
        else
            SetState(_selected ? (_over ? SELECTED_OVER : DOWN) : (_over ? OVER : UP));
    }

    public override void HandleControllerChanged(FGUIController c)
    {
        base.HandleControllerChanged(c);
        if (_relatedController == c)
            Selected = _relatedPageId == c.SelectedPageId;
    }

    protected override void HandleGrayedChanged()
    {
        if (_buttonController != null && _buttonController.HasPage(DISABLED))
            SetState(Grayed ? (_selected ? SELECTED_DISABLED : DISABLED) : (_selected ? DOWN : UP));
        else
            base.HandleGrayedChanged();
    }

    protected override void ConstructExtension(ByteBuffer buffer)
    {
        buffer.Seek(0, 6);

        _mode = (ButtonMode)buffer.ReadByte();
        buffer.ReadS(); // sound URL
        buffer.ReadFloat(); // soundVolumeScale
        _downEffect = buffer.ReadByte();
        _downEffectValue = buffer.ReadFloat();
        if (_downEffect == 2)
            SetPivot(0.5f, 0.5f, PivotAsAnchor);

        _buttonController = GetController("button");
        _titleObject = GetChild("title");
        _iconObject = GetChild("icon");

        // Apply title/icon that was set in Setup_AfterAdd to the child objects
        if (_titleObject != null && !string.IsNullOrEmpty(_title))
            _titleObject.Text = _title;
        if (_iconObject != null && !string.IsNullOrEmpty(_icon))
            _iconObject.Icon = _icon;

        if (_mode == ButtonMode.Common)
            SetState(UP);

        // 注册事件处理器
        RegisterEventHandlers();

        Game.Logger.LogInformation($"[FGUI] Button ConstructExtension: name='{Name}', title='{_title}', titleObjText='{_titleObject?.Text}', mode={_mode}");
    }

    /// <summary>
    /// 注册按钮事件处理器
    /// </summary>
    private void RegisterEventHandlers()
    {
        // 鼠标进入/离开事件
        AddEventListener("onPointerEnter", HandleRollOver);
        AddEventListener("onPointerLeave", HandleRollOut);

        // 触摸/点击事件
        AddEventListener("onTouchBegin", HandleTouchBegin);
        AddEventListener("onTouchEnd", HandleTouchEnd);
        AddEventListener("onClick", HandleClick);

        // 从舞台移除事件
        AddEventListener("onRemovedFromStage", HandleRemovedFromStage);
    }

    /// <summary>
    /// 鼠标进入事件处理
    /// </summary>
    private void HandleRollOver(EventContext context)
    {
        if (_buttonController == null || !_buttonController.HasPage(OVER))
            return;

        _over = true;
        if (_down)
            return;

        if (Grayed && _buttonController.HasPage(DISABLED))
            return;

        SetState(_selected ? SELECTED_OVER : OVER);
    }

    /// <summary>
    /// 鼠标离开事件处理
    /// </summary>
    private void HandleRollOut(EventContext context)
    {
        if (_buttonController == null || !_buttonController.HasPage(OVER))
            return;

        _over = false;
        if (_down)
            return;

        if (Grayed && _buttonController.HasPage(DISABLED))
            return;

        SetState(_selected ? DOWN : UP);
    }

    /// <summary>
    /// 触摸开始事件处理
    /// </summary>
    private void HandleTouchBegin(EventContext context)
    {
        _down = true;
        // context.CaptureTouch(); // SCE中可能需要不同的实现

        if (_mode == ButtonMode.Common)
        {
            if (Grayed && _buttonController != null && _buttonController.HasPage(DISABLED))
                SetState(SELECTED_DISABLED);
            else
                SetState(DOWN);
        }

        // TODO: linkedPopup支持
        // if (linkedPopup != null) { ... }
    }

    /// <summary>
    /// 触摸结束事件处理
    /// </summary>
    private void HandleTouchEnd(EventContext context)
    {
        if (_down)
        {
            _down = false;
            if (_mode == ButtonMode.Common)
            {
                if (Grayed && _buttonController != null && _buttonController.HasPage(DISABLED))
                    SetState(DISABLED);
                else if (_over)
                    SetState(OVER);
                else
                    SetState(UP);
            }
            else
            {
                if (!_over
                    && _buttonController != null
                    && (_buttonController.SelectedPage == OVER || _buttonController.SelectedPage == SELECTED_OVER))
                {
                    SetCurrentState();
                }
            }
        }
    }

    /// <summary>
    /// 点击事件处理
    /// </summary>
    private void HandleClick(EventContext context)
    {
        // TODO: 音效播放
        // if (sound != null) { ... }

        if (_mode == ButtonMode.Check)
        {
            if (ChangeStateOnClick)
            {
                Selected = !_selected;
                _onChanged?.Call(context);
            }
        }
        else if (_mode == ButtonMode.Radio)
        {
            if (ChangeStateOnClick && !_selected)
            {
                Selected = true;
                _onChanged?.Call(context);
            }
        }
        else // Common mode
        {
            if (_relatedController != null && _relatedPageId != null)
                _relatedController.SelectedPageId = _relatedPageId;
        }
    }

    /// <summary>
    /// 从舞台移除事件处理
    /// </summary>
    private void HandleRemovedFromStage(EventContext context)
    {
        if (_over)
            HandleRollOut(context);
    }

    /// <summary>
    /// 模拟按钮点击（程序化触发）
    /// </summary>
    /// <param name="downEffect">是否显示按下效果</param>
    /// <param name="clickCall">是否触发点击回调</param>
    public void FireClick(bool downEffect = true, bool clickCall = true)
    {
        if (downEffect && _mode == ButtonMode.Common)
        {
            SetState(DOWN);
            // 延迟恢复状态
            Tween.GTween.DelayedCall(0.1f, () =>
            {
                SetState(_over ? OVER : UP);
            });
        }

        if (clickCall)
        {
            HandleClick(new EventContext());
        }
    }

    public override void Setup_AfterAdd(ByteBuffer buffer, int beginPos)
    {
        base.Setup_AfterAdd(buffer, beginPos);

        if (!buffer.Seek(beginPos, 6))
            return;

        if ((ObjectType)buffer.ReadByte() != PackageItem?.ObjectType)
            return;

        string? str;

        str = buffer.ReadS();
        if (str != null) Title = str;

        str = buffer.ReadS();
        if (str != null) SelectedTitle = str;

        str = buffer.ReadS();
        if (str != null) Icon = str;

        str = buffer.ReadS();
        if (str != null) SelectedIcon = str;

        if (buffer.ReadBool())
            TitleColor = buffer.ReadColor();

        int iv = buffer.ReadInt();
        if (iv != 0) TitleFontSize = iv;

        iv = buffer.ReadShort();
        if (iv >= 0 && Parent != null)
            _relatedController = Parent.GetControllerAt(iv);

        _relatedPageId = buffer.ReadS();

        buffer.ReadS(); // sound override
        if (buffer.ReadBool())
            buffer.ReadFloat(); // soundVolumeScale override

        Selected = buffer.ReadBool();

        Game.Logger.LogInformation($"[FGUI] Button Setup_AfterAdd: title='{_title}', selected={_selected}");
    }
}

public interface IColorGear
{
    Color Color { get; set; }
}
#endif
