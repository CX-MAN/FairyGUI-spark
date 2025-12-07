#if CLIENT
using System.Drawing;
using SCEFGUI.Core;
using SCEFGUI.Utils;

namespace SCEFGUI.UI;

public class FGUILoader : FGUIObject
{
    private string _url = "";
    private AlignType _align = AlignType.Left;
    private VertAlignType _verticalAlign = VertAlignType.Top;
    private FillType _fill = FillType.None;
    private bool _shrinkOnly;
    private bool _autoSize;
    private FGUIObject? _content;

    public string Url { get => _url; set { if (_url != value) { _url = value; LoadContent(); } } }
    public override string? Icon { get => _url; set => Url = value ?? ""; }
    public AlignType Align { get => _align; set => _align = value; }
    public VertAlignType VerticalAlign { get => _verticalAlign; set => _verticalAlign = value; }
    public FillType Fill { get => _fill; set => _fill = value; }
    public bool ShrinkOnly { get => _shrinkOnly; set => _shrinkOnly = value; }
    public bool AutoSize { get => _autoSize; set => _autoSize = value; }
    public FGUIObject? Content => _content;

    private void LoadContent()
    {
        if (string.IsNullOrEmpty(_url)) { ClearContent(); return; }
        if (_url.StartsWith(FGUIPackage.URL_PREFIX))
        {
            var item = FGUIPackage.GetItemByURL(_url);
            if (item != null) LoadFromPackage(item);
            else LoadExternal();
        }
        else
        {
            LoadExternal();
        }
    }

    private void LoadFromPackage(PackageItem item)
    {
        if (item.Type == PackageItemType.Image) { var image = new FGUIImage { PackageItem = item }; image.ConstructFromResource(); SetContent(image); }
        else if (item.Type == PackageItemType.MovieClip) { var clip = new FGUIMovieClip { PackageItem = item }; clip.ConstructFromResource(); SetContent(clip); }
        else if (item.Type == PackageItemType.Component) { var obj = item.Owner?.CreateObject(item); if (obj != null) SetContent(obj); }
    }

    /// <summary>
    /// Override this to implement custom external loading
    /// </summary>
    protected virtual void LoadExternal()
    {
        // Default: do nothing, subclasses can override
    }

    /// <summary>
    /// Override this to free external resources
    /// </summary>
    protected virtual void FreeExternal()
    {
        // Default: do nothing, subclasses can override
    }

    protected void SetContent(FGUIObject obj) { ClearContent(); _content = obj; UpdateLayout(); }
    
    private void ClearContent() 
    { 
        if (_content != null)
        {
            FreeExternal();
            _content.Dispose(); 
            _content = null; 
        }
    }
    
    private void UpdateLayout() { if (_content == null) return; if (_fill >= FillType.Scale) _content.SetSize(_width, _height); }

    public override void Setup_BeforeAdd(ByteBuffer buffer, int beginPos)
    {
        base.Setup_BeforeAdd(buffer, beginPos);
        buffer.Seek(beginPos, 5);
        _url = buffer.ReadS() ?? "";
        _align = (AlignType)buffer.ReadByte();
        _verticalAlign = (VertAlignType)buffer.ReadByte();
        _fill = (FillType)buffer.ReadByte();
        _shrinkOnly = buffer.ReadBool();
        _autoSize = buffer.ReadBool();
        if (!string.IsNullOrEmpty(_url)) LoadContent();
    }

    public override void Dispose() { ClearContent(); base.Dispose(); }
}

public class FGUIGroup : FGUIObject
{
    private GroupLayoutType _layout = GroupLayoutType.None;
    private int _lineGap, _columnGap;
    private bool _excludeInvisibles, _autoSizeDisabled;
    private bool _boundsChanged;

    public GroupLayoutType Layout { get => _layout; set => _layout = value; }
    public int LineGap { get => _lineGap; set { _lineGap = value; SetBoundsChangedFlag(); } }
    public int ColumnGap { get => _columnGap; set { _columnGap = value; SetBoundsChangedFlag(); } }
    public bool ExcludeInvisibles { get => _excludeInvisibles; set => _excludeInvisibles = value; }
    public void SetBoundsChangedFlag(bool positionChanged = false) => _boundsChanged = true;

    protected override void HandleVisibleChanged()
    {
        base.HandleVisibleChanged();
        // When Group visibility changes, notify all children that belong to this group
        if (Parent != null)
        {
            int cnt = Parent.NumChildren;
            for (int i = 0; i < cnt; i++)
            {
                var child = Parent.GetChildAt(i);
                if (child.Group == this)
                    Parent.ChildStateChanged(child);
            }
        }
    }

    public override void Setup_BeforeAdd(ByteBuffer buffer, int beginPos)
    {
        base.Setup_BeforeAdd(buffer, beginPos);
        buffer.Seek(beginPos, 5);
        _layout = (GroupLayoutType)buffer.ReadByte();
        _lineGap = buffer.ReadInt();
        _columnGap = buffer.ReadInt();
        if (buffer.Version >= 2) { _excludeInvisibles = buffer.ReadBool(); _autoSizeDisabled = buffer.ReadBool(); buffer.ReadShort(); }
    }
}

public class FGUIProgressBar : FGUIComponent
{
    private float _min, _max = 100, _value;
    private ProgressTitleType _titleType = ProgressTitleType.Percent;
    private bool _reverse;
    private FGUIObject? _titleObject;
    private FGUIObject? _barObject;

    public float Min { get => _min; set { _min = value; Update(); } }
    public float Max { get => _max; set { _max = value; Update(); } }
    public float Value { get => _value; set { _value = Math.Clamp(value, _min, _max); Update(); } }
    public double Percent => _max > _min ? (_value - _min) / (_max - _min) : 0;
    public ProgressTitleType TitleType { get => _titleType; set { _titleType = value; Update(); } }
    public bool Reverse { get => _reverse; set { _reverse = value; Update(); } }

    public override void ConstructFromResource()
    {
        base.ConstructFromResource();
        _titleObject = GetChild("title");
        _barObject = GetChild("bar");
        
        var buffer = PackageItem?.RawData;
        if (buffer != null)
        {
            buffer.Seek(0, 6);
            _titleType = (ProgressTitleType)buffer.ReadByte();
            _reverse = buffer.ReadBool();
        }
    }

    public override void Setup_AfterAdd(ByteBuffer buffer, int beginPos)
    {
        base.Setup_AfterAdd(buffer, beginPos);
        if (!buffer.Seek(beginPos, 6)) return;
        if ((ObjectType)buffer.ReadByte() != PackageItem?.ObjectType) return;
        _value = buffer.ReadInt();
        _max = buffer.ReadInt();
        Update();
    }

    private void Update()
    {
        float percent = (float)Percent;
        if (_titleObject != null)
        {
            _titleObject.Text = _titleType switch
            {
                ProgressTitleType.Percent => $"{(int)(percent * 100)}%",
                ProgressTitleType.ValueAndMax => $"{(int)_value}/{(int)_max}",
                ProgressTitleType.Value => $"{(int)_value}",
                ProgressTitleType.Max => $"{(int)_max}",
                _ => ""
            };
        }
        if (_barObject != null)
        {
            if (_reverse)
                _barObject.Width = (1 - percent) * Width;
            else
                _barObject.Width = percent * Width;
        }
    }
}

public class FGUISlider : FGUIComponent
{
    private float _min, _max = 100, _value;
    private bool _wholeNumbers, _reverse;
    private FGUIObject? _titleObject;
    private FGUIObject? _barObject;
    private FGUIObject? _gripObject;
    private ProgressTitleType _titleType;

    public float Min { get => _min; set { _min = value; Update(); } }
    public float Max { get => _max; set { _max = value; Update(); } }
    public float Value { get => _value; set { _value = Math.Clamp(value, _min, _max); if (_wholeNumbers) _value = MathF.Round(_value); Update(); DispatchEvent("onChanged", null); } }
    public bool WholeNumbers { get => _wholeNumbers; set => _wholeNumbers = value; }

    public override void ConstructFromResource()
    {
        base.ConstructFromResource();
        _titleObject = GetChild("title");
        _barObject = GetChild("bar");
        _gripObject = GetChild("grip");

        var buffer = PackageItem?.RawData;
        if (buffer != null)
        {
            buffer.Seek(0, 6);
            _titleType = (ProgressTitleType)buffer.ReadByte();
            _reverse = buffer.ReadBool();
            _wholeNumbers = buffer.ReadBool();
        }
    }

    public override void Setup_AfterAdd(ByteBuffer buffer, int beginPos)
    {
        base.Setup_AfterAdd(buffer, beginPos);
        if (!buffer.Seek(beginPos, 6)) return;
        if ((ObjectType)buffer.ReadByte() != PackageItem?.ObjectType) return;
        _value = buffer.ReadInt();
        _max = buffer.ReadInt();
        Update();
    }

    private void Update()
    {
        float percent = _max > _min ? (_value - _min) / (_max - _min) : 0;
        if (_titleObject != null)
            _titleObject.Text = $"{(int)(_reverse ? (1 - percent) * 100 : percent * 100)}%";
        if (_barObject != null)
            _barObject.Width = (_reverse ? 1 - percent : percent) * Width;
        if (_gripObject != null)
            _gripObject.X = (_reverse ? 1 - percent : percent) * (Width - _gripObject.Width);
    }
}

public class FGUIScrollBar : FGUIComponent
{
    private FGUIObject? _grip;
    private FGUIObject? _bar;
    private FGUIObject? _arrowButton1;
    private FGUIObject? _arrowButton2;
    private FGUIScrollPane? _target;
    private bool _vertical;
    private float _scrollPerc;
    private bool _fixedGripSize;
    private bool _gripDragging;
    private float _dragOffset;

    public bool GripDragging => _gripDragging;

    public void SetScrollPane(FGUIScrollPane target, bool vertical)
    {
        _target = target;
        _vertical = vertical;
    }

    public void SetDisplayPerc(float value)
    {
        if (_grip == null || _bar == null) return;
        
        if (_vertical)
        {
            if (!_fixedGripSize)
                _grip.Height = value * _bar.Height;
            _grip.Y = _bar.Y + (_bar.Height - _grip.Height) * _scrollPerc;
        }
        else
        {
            if (!_fixedGripSize)
                _grip.Width = value * _bar.Width;
            _grip.X = _bar.X + (_bar.Width - _grip.Width) * _scrollPerc;
        }
        _grip.Visible = value != 0 && value != 1;
    }

    public void SetScrollPerc(float value)
    {
        _scrollPerc = value;
        if (_grip == null || _bar == null) return;
        
        if (_vertical)
            _grip.Y = _bar.Y + (_bar.Height - _grip.Height) * _scrollPerc;
        else
            _grip.X = _bar.X + (_bar.Width - _grip.Width) * _scrollPerc;
    }

    public float MinSize => _vertical
        ? ((_arrowButton1?.Height ?? 0) + (_arrowButton2?.Height ?? 0))
        : ((_arrowButton1?.Width ?? 0) + (_arrowButton2?.Width ?? 0));

    protected override void ConstructExtension(ByteBuffer buffer)
    {
        buffer.Seek(0, 6);
        _fixedGripSize = buffer.ReadBool();
        
        _grip = GetChild("grip");
        _bar = GetChild("bar");
        _arrowButton1 = GetChild("arrow1");
        _arrowButton2 = GetChild("arrow2");
        
        if (_grip != null)
        {
            _grip.OnTouchBegin.Add(OnGripTouchBegin);
            _grip.OnTouchMove.Add(OnGripTouchMove);
            _grip.OnTouchEnd.Add(OnGripTouchEnd);
        }
    }

    private void OnGripTouchBegin(Event.EventContext ctx)
    {
        if (_bar == null || _target == null) return;
        ctx.StopPropagation();
        _gripDragging = true;
    }

    private void OnGripTouchMove(Event.EventContext ctx)
    {
        if (_bar == null || _target == null || _grip == null) return;
        // Touch move handling would require position data from ctx
    }

    private void OnGripTouchEnd(Event.EventContext ctx)
    {
        _gripDragging = false;
    }
}

public class FGUIComboBox : FGUIComponent
{
    private readonly List<string> _items = new();
    private readonly List<string> _values = new();
    private int _selectedIndex = -1;
    private string _title = "";
    private FGUIObject? _titleObject;
    private FGUIController? _buttonController;
    private bool _itemsUpdated;

    public IList<string> Items => _items;
    public IList<string> Values => _values;
    public int SelectedIndex { get => _selectedIndex; set { if (_selectedIndex != value && value >= -1 && value < _items.Count) { _selectedIndex = value; UpdateCurrentText(); DispatchEvent("onChanged", null); } } }
    public string? SelectedValue { get => _selectedIndex >= 0 && _selectedIndex < _values.Count ? _values[_selectedIndex] : null; set { int index = _values.IndexOf(value ?? ""); if (index >= 0) SelectedIndex = index; } }
    public override string? Text { get => _title; set { _title = value ?? ""; if (_titleObject != null) _titleObject.Text = _title; } }

    public override void ConstructFromResource()
    {
        base.ConstructFromResource();
        _titleObject = GetChild("title");
        _buttonController = GetController("button");

        var buffer = PackageItem?.RawData;
        if (buffer != null && buffer.Seek(0, 6))
        {
            if (buffer.Position + 3 > buffer.Length) return; // safety check
            buffer.ReadByte(); // visibleItemCount
            int itemCount = buffer.ReadShort();
            for (int i = 0; i < itemCount; i++)
            {
                if (buffer.Position >= buffer.Length) break; // safety check
                string? title = buffer.ReadS();
                string? value = buffer.ReadS();
                buffer.ReadS(); // icon
                if (title != null)
                {
                    _items.Add(title);
                    _values.Add(value ?? "");
                }
            }
        }
    }

    public override void Setup_AfterAdd(ByteBuffer buffer, int beginPos)
    {
        base.Setup_AfterAdd(buffer, beginPos);
        if (!buffer.Seek(beginPos, 6)) return;
        if ((ObjectType)buffer.ReadByte() != PackageItem?.ObjectType) return;
        
        string? str = buffer.ReadS();
        if (str != null) Text = str;
        
        int index = _items.IndexOf(_title);
        if (index >= 0) _selectedIndex = index;
    }

    private void UpdateCurrentText()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _items.Count)
            Text = _items[_selectedIndex];
    }
}

public class FGUITree : FGUIList
{
    public delegate void TreeNodeRenderDelegate(FGUITreeNode node, FGUIComponent obj);
    public delegate void TreeNodeWillExpandDelegate(FGUITreeNode node, bool expand);
    
    public TreeNodeRenderDelegate? TreeNodeRender;
    public TreeNodeWillExpandDelegate? TreeNodeWillExpand;
    
    private int _indent = 30;
    private FGUITreeNode _rootNode;
    private int _clickToExpand;

    public FGUITree()
    {
        _rootNode = new FGUITreeNode(true);
        _rootNode.SetTree(this);
        _rootNode.Expanded = true;
    }

    public FGUITreeNode RootNode => _rootNode;
    public int Indent { get => _indent; set => _indent = value; }
    public int ClickToExpand { get => _clickToExpand; set => _clickToExpand = value; }

    public FGUITreeNode? GetSelectedNode()
    {
        int i = SelectedIndex;
        return i >= 0 ? GetChildAt(i)?._treeNode : null;
    }

    public List<FGUITreeNode> GetSelectedNodes()
    {
        var result = new List<FGUITreeNode>();
        foreach (int i in SelectedIndices)
        {
            var node = GetChildAt(i)?._treeNode;
            if (node != null) result.Add(node);
        }
        return result;
    }

    public void SelectNode(FGUITreeNode node, bool scrollItToView = false)
    {
        var cell = node.Cell;
        if (cell == null) return;
        int index = GetChildIndex(cell);
        if (index >= 0) AddSelection(index, scrollItToView);
    }

    public void UnselectNode(FGUITreeNode node)
    {
        var cell = node.Cell;
        if (cell == null) return;
        int index = GetChildIndex(cell);
        if (index >= 0) RemoveSelection(index);
    }

    public void ExpandAll(FGUITreeNode? folderNode = null)
    {
        folderNode ??= _rootNode;
        folderNode.Expanded = true;
        foreach (var child in folderNode.Children)
            if (child.IsFolder) ExpandAll(child);
    }

    public void CollapseAll(FGUITreeNode? folderNode = null)
    {
        folderNode ??= _rootNode;
        if (folderNode != _rootNode) folderNode.Expanded = false;
        foreach (var child in folderNode.Children)
            if (child.IsFolder) CollapseAll(child);
    }

    internal void AfterInserted(FGUITreeNode node)
    {
        // Placeholder for node insertion handling
    }

    internal void AfterRemoved(FGUITreeNode node)
    {
        // Placeholder for node removal handling
    }

    internal void AfterExpanded(FGUITreeNode node)
    {
        // Placeholder for expansion handling
    }

    internal void AfterMoved(FGUITreeNode node)
    {
        // Placeholder for move handling
    }
}

public class FGUITreeNode
{
    public object? Data { get; set; }
    public FGUITreeNode? Parent { get; private set; }
    public FGUITree? Tree { get; private set; }
    
    private List<FGUITreeNode>? _children;
    private bool _expanded;
    private int _level;
    internal FGUIComponent? _cell;
    internal string? _resUrl;

    public FGUITreeNode(bool hasChild, string? resUrl = null)
    {
        if (hasChild) _children = new List<FGUITreeNode>();
        _resUrl = resUrl;
    }

    public FGUIComponent? Cell => _cell;
    public int Level => _level;
    public bool IsFolder => _children != null;
    public string? Text => _cell?.Text;
    public string? Icon => _cell?.Icon;
    
    public bool Expanded
    {
        get => _expanded;
        set
        {
            if (_children == null) return;
            if (_expanded == value) return;
            _expanded = value;
            if (Tree != null)
            {
                Tree.TreeNodeWillExpand?.Invoke(this, value);
                if (_cell != null)
                {
                    var controller = _cell.GetController("expanded");
                    if (controller != null) controller.SelectedIndex = value ? 1 : 0;
                }
                Tree.AfterExpanded(this);
            }
        }
    }

    public IReadOnlyList<FGUITreeNode> Children => _children ?? (IReadOnlyList<FGUITreeNode>)Array.Empty<FGUITreeNode>();
    public int NumChildren => _children?.Count ?? 0;

    public FGUITreeNode AddChild(FGUITreeNode child) => AddChildAt(child, _children?.Count ?? 0);

    public FGUITreeNode AddChildAt(FGUITreeNode child, int index)
    {
        if (_children == null) throw new InvalidOperationException("Not a folder node");
        if (child.Parent != null) child.Parent.RemoveChild(child);
        
        child.Parent = this;
        child._level = _level + 1;
        child.SetTree(Tree);
        
        _children.Insert(Math.Clamp(index, 0, _children.Count), child);
        Tree?.AfterInserted(child);
        return child;
    }

    public void RemoveChild(FGUITreeNode child)
    {
        if (_children == null || !_children.Contains(child)) return;
        _children.Remove(child);
        child.Parent = null;
        Tree?.AfterRemoved(child);
    }

    public void RemoveChildren(int beginIndex = 0, int endIndex = -1)
    {
        if (_children == null) return;
        if (endIndex < 0) endIndex = _children.Count;
        for (int i = endIndex - 1; i >= beginIndex; i--)
        {
            var child = _children[i];
            _children.RemoveAt(i);
            child.Parent = null;
            Tree?.AfterRemoved(child);
        }
    }

    public FGUITreeNode? GetChildAt(int index) => _children != null && index >= 0 && index < _children.Count ? _children[index] : null;
    public FGUITreeNode? GetPrevSibling() => Parent?._children?.ElementAtOrDefault((Parent._children.IndexOf(this)) - 1);
    public FGUITreeNode? GetNextSibling() => Parent?._children?.ElementAtOrDefault((Parent._children.IndexOf(this)) + 1);

    internal void SetTree(FGUITree? tree)
    {
        Tree = tree;
        if (_children != null)
            foreach (var child in _children)
                child.SetTree(tree);
    }
}

public class FGUIRoot : FGUIComponent
{
    private static FGUIRoot? _instance;
    private readonly List<FGUIObject> _popupStack = new();
    private readonly List<FGUIObject> _justClosedPopups = new();
    private FGUIGraph? _modalLayer;
    private FGUIObject? _modalWaitPane;
    private FGUIObject? _tooltipWin;
    
    public static FGUIRoot Instance => _instance ??= new FGUIRoot();
    
    public FGUIRoot() 
    { 
        Name = "FGUIRoot"; 
        Opaque = false;
    }
    
    /// <summary>
    /// 重写 AddToStage 以确保根控件正确适配屏幕
    /// </summary>
    public new void AddToStage()
    {
        // 在添加到舞台前更新尺寸
        ApplyContentScaleFactor();
        // 调用基类方法添加到舞台
        base.AddToStage();
    }
    
    /// <summary>
    /// 应用内容缩放因子，根据设计分辨率和屏幕尺寸计算缩放
    /// 类似于 Unity FairyGUI 的 GRoot.ApplyContentScaleFactor
    /// 
    /// Unity实现原理：
    /// this.SetSize(Stage.width / scaleFactor, Stage.height / scaleFactor);
    /// this.SetScale(scaleFactor, scaleFactor);
    /// 
    /// 星火实现（由于SCE不支持Scale，通过Stretch来适配）：
    /// 1. FGUIRoot逻辑尺寸设置为 屏幕尺寸/scaleFactor（等于设计分辨率）
    /// 2. 通过SCE的Stretch特性来填满屏幕
    /// 3. 子组件使用设计分辨率布局，SCE自动缩放
    /// </summary>
    public void ApplyContentScaleFactor()
    {
        // 更新缩放因子
        FGUIManager.UpdateContentScaleFactor();
        
        var screenSize = Render.SCERenderContext.Instance.Adapter?.GetScreenSize();
        float scaleFactor = FGUIManager.ContentScaleFactor;
        
        if (screenSize.HasValue && screenSize.Value.Width > 0 && screenSize.Value.Height > 0 && scaleFactor > 0)
        {
            // FGUIRoot的逻辑尺寸 = 屏幕尺寸 / scaleFactor
            // 这样逻辑尺寸就等于或接近设计分辨率
            float logicalWidth = screenSize.Value.Width / scaleFactor;
            float logicalHeight = screenSize.Value.Height / scaleFactor;
            
            base.SetSize(logicalWidth, logicalHeight);
            
            // 设置缩放因子（虽然SCE不直接支持Scale，但FGUI内部逻辑需要这个值）
            base.SetScale(scaleFactor, scaleFactor);
            
            Game.Logger.LogInformation($"[FGUIRoot] ApplyContentScaleFactor: logical size = {logicalWidth:F0}x{logicalHeight:F0}, scaleFactor = {scaleFactor:F3}, screen = {screenSize.Value.Width}x{screenSize.Value.Height}");
        }
        else
        {
            // 如果无法获取屏幕尺寸，使用设计分辨率
            base.SetSize(FGUIManager.DesignResolutionX, FGUIManager.DesignResolutionY);
            Game.Logger.LogWarning($"[FGUIRoot] ApplyContentScaleFactor: using design resolution {FGUIManager.DesignResolutionX}x{FGUIManager.DesignResolutionY}");
        }
    }
    
    public void SetRootSize(float width, float height) => base.SetSize(width, height);
    
    public void ShowWindow(FGUIWindow win)
    {
        AddChild(win);
        AdjustModalLayer();
        win.DoShow();
    }
    
    public void HideWindow(FGUIWindow win)
    {
        win.Hide();
    }
    
    public void HideWindowImmediately(FGUIWindow win)
    {
        if (win.Parent == this)
        {
            RemoveChild(win);
            AdjustModalLayer();
        }
    }
    
    public void BringToFront(FGUIWindow win)
    {
        int index = GetChildIndex(win);
        if (index >= 0 && index < NumChildren - 1)
        {
            SetChildIndex(win, NumChildren - 1);
            AdjustModalLayer();
        }
    }
    
    public void ShowModalWait()
    {
        if (_modalWaitPane == null)
        {
            // Create a simple modal wait pane
            _modalWaitPane = new FGUIGraph();
            _modalWaitPane.SetSize(Width, Height);
            ((FGUIGraph)_modalWaitPane).DrawRect(Width, Height, 0, Color.Transparent, Color.FromArgb(100, 0, 0, 0));
        }
        _modalWaitPane.Visible = true;
        AddChild(_modalWaitPane);
    }
    
    public void CloseModalWait()
    {
        if (_modalWaitPane != null && _modalWaitPane.Parent == this)
        {
            _modalWaitPane.RemoveFromParent();
        }
    }
    
    public bool HasModalWindow
    {
        get
        {
            foreach (var child in _children)
                if (child is FGUIWindow win && win.Modal && win.IsShowing)
                    return true;
            return false;
        }
    }
    
    public bool IsModalWaiting => _modalWaitPane != null && _modalWaitPane.Parent == this;
    
    public void ShowPopup(FGUIObject popup, FGUIObject? target = null, PopupDirection direction = PopupDirection.Auto)
    {
        if (!_popupStack.Contains(popup))
            _popupStack.Add(popup);
            
        AddChild(popup);
        AdjustPopupPosition(popup, target, direction);
    }
    
    public void TogglePopup(FGUIObject popup, FGUIObject? target = null, PopupDirection direction = PopupDirection.Auto)
    {
        if (_justClosedPopups.Contains(popup))
            return;
            
        if (popup.Parent != null)
            HidePopup(popup);
        else
            ShowPopup(popup, target, direction);
    }
    
    public void HidePopup(FGUIObject? popup = null)
    {
        if (popup != null)
        {
            int index = _popupStack.IndexOf(popup);
            if (index >= 0)
            {
                popup.RemoveFromParent();
                _popupStack.RemoveAt(index);
            }
        }
        else
        {
            // Hide all popups
            foreach (var p in _popupStack.ToArray())
            {
                p.RemoveFromParent();
            }
            _popupStack.Clear();
        }
    }
    
    private void AdjustPopupPosition(FGUIObject popup, FGUIObject? target, PopupDirection direction)
    {
        if (target == null)
        {
            popup.SetXY((Width - popup.Width) / 2, (Height - popup.Height) / 2);
            return;
        }
        
        float x = target.X;
        float y = target.Y;
        
        if (direction == PopupDirection.Down || direction == PopupDirection.Auto)
        {
            y = target.Y + target.Height;
            if (y + popup.Height > Height)
                y = target.Y - popup.Height;
        }
        else if (direction == PopupDirection.Up)
        {
            y = target.Y - popup.Height;
            if (y < 0)
                y = target.Y + target.Height;
        }
        
        x = Math.Clamp(x, 0, Width - popup.Width);
        y = Math.Clamp(y, 0, Height - popup.Height);
        
        popup.SetXY(x, y);
    }
    
    private void AdjustModalLayer()
    {
        // Find topmost modal window
        FGUIWindow? topModal = null;
        for (int i = NumChildren - 1; i >= 0; i--)
        {
            if (_children[i] is FGUIWindow win && win.Modal)
            {
                topModal = win;
                break;
            }
        }
        
        if (topModal != null)
        {
            if (_modalLayer == null)
            {
                _modalLayer = new FGUIGraph();
                _modalLayer.Name = "ModalLayer";
                _modalLayer.Touchable = true;
            }
            _modalLayer.SetSize(Width, Height);
            _modalLayer.DrawRect(Width, Height, 0, Color.Transparent, Color.FromArgb(100, 0, 0, 0));
            
            int index = GetChildIndex(topModal);
            if (_modalLayer.Parent != this)
                AddChildAt(_modalLayer, index);
            else
                SetChildIndex(_modalLayer, index);
        }
        else if (_modalLayer != null && _modalLayer.Parent == this)
        {
            RemoveChild(_modalLayer);
        }
    }
    
    public void ShowTooltips(string msg)
    {
        if (_tooltipWin == null)
        {
            // Create simple tooltip
            var label = new FGUITextField();
            label.Text = msg;
            label.SetXY(10, 10);
            _tooltipWin = label;
        }
        else if (_tooltipWin is FGUITextField tf)
        {
            tf.Text = msg;
        }
        AddChild(_tooltipWin);
    }
    
    public void HideTooltips()
    {
        _tooltipWin?.RemoveFromParent();
    }
}

public class FGUIWindow : FGUIComponent
{
    private FGUIComponent? _contentPane;
    private FGUIComponent? _frame;
    private FGUIObject? _closeButton;
    private FGUIObject? _dragArea;
    private bool _modal;
    private bool _inited;

    public FGUIWindow() { Name = "Window"; }

    public FGUIComponent? ContentPane
    {
        get => _contentPane;
        set
        {
            if (_contentPane != value)
            {
                if (_contentPane != null) RemoveChild(_contentPane);
                _contentPane = value;
                if (_contentPane != null)
                {
                    Name = "Window - " + _contentPane.Name;
                    AddChild(_contentPane);
                    SetSize(_contentPane.Width, _contentPane.Height);
                    _frame = _contentPane.GetChild("frame") as FGUIComponent;
                    if (_frame != null)
                    {
                        CloseButton = _frame.GetChild("closeButton");
                        DragArea = _frame.GetChild("dragArea");
                    }
                }
                else { _frame = null; Name = "Window"; }
            }
        }
    }

    public FGUIComponent? Frame => _frame;

    public FGUIObject? CloseButton
    {
        get => _closeButton;
        set
        {
            if (_closeButton != null) _closeButton.OnClick.Remove(OnCloseButtonClick);
            _closeButton = value;
            if (_closeButton != null) _closeButton.OnClick.Add(OnCloseButtonClick);
        }
    }

    public FGUIObject? DragArea
    {
        get => _dragArea;
        set
        {
            if (_dragArea != null) _dragArea.Draggable = false;
            _dragArea = value;
            if (_dragArea != null) _dragArea.Draggable = true;
        }
    }

    public bool Modal { get => _modal; set => _modal = value; }
    public bool IsShowing => Parent != null;

    public void Show() => FGUIRoot.Instance.ShowWindow(this);
    public void Hide() { if (IsShowing) DoHideAnimation(); }
    public void HideImmediately() => FGUIRoot.Instance.HideWindowImmediately(this);

    public void Center()
    {
        var root = FGUIRoot.Instance;
        SetXY((root.Width - Width) / 2, (root.Height - Height) / 2);
    }

    public void BringToFront() => FGUIRoot.Instance.BringToFront(this);

    internal void DoShow() { if (!_inited) Init(); else DoShowAnimation(); }

    public void Init() { if (_inited) return; _inited = true; OnInit(); if (IsShowing) DoShowAnimation(); }

    protected virtual void OnInit() { }
    protected virtual void OnShown() { }
    protected virtual void OnHide() { }
    protected virtual void DoShowAnimation() { OnShown(); }
    protected virtual void DoHideAnimation() { HideImmediately(); }

    private void OnCloseButtonClick(Event.EventContext ctx) => Hide();

    public override void Dispose() { _inited = false; base.Dispose(); }
}

public class PopupMenu
{
    private FGUIComponent? _contentPane;
    private FGUIList? _list;
    private FGUIObject? _popupTarget;
    private List<PopupMenuItem> _items = new();

    public FGUIComponent? ContentPane => _contentPane;
    public int ItemCount => _items.Count;

    public PopupMenu(string? resourceURL = null)
    {
        if (!string.IsNullOrEmpty(resourceURL))
        {
            _contentPane = FGUIManager.CreateObject(resourceURL) as FGUIComponent;
            if (_contentPane != null)
            {
                _contentPane.Visible = false;
                _list = _contentPane.GetChild("list") as FGUIList;
            }
        }
    }

    public PopupMenuItem AddItem(string caption, Action? callback = null)
    {
        var item = new PopupMenuItem { Caption = caption, Callback = callback };
        _items.Add(item);
        RefreshList();
        return item;
    }

    public PopupMenuItem AddItemAt(string caption, int index, Action? callback = null)
    {
        var item = new PopupMenuItem { Caption = caption, Callback = callback };
        _items.Insert(index, item);
        RefreshList();
        return item;
    }

    public void AddSeparator()
    {
        var item = new PopupMenuItem { IsSeparator = true };
        _items.Add(item);
        RefreshList();
    }

    public PopupMenuItem? GetItemAt(int index)
    {
        if (index < 0 || index >= _items.Count) return null;
        return _items[index];
    }

    public void SetItemText(string name, string caption)
    {
        var item = _items.FirstOrDefault(i => i.Name == name);
        if (item != null) item.Caption = caption;
        RefreshList();
    }

    public void SetItemVisible(string name, bool visible)
    {
        var item = _items.FirstOrDefault(i => i.Name == name);
        if (item != null) item.Visible = visible;
        RefreshList();
    }

    public void SetItemGrayed(string name, bool grayed)
    {
        var item = _items.FirstOrDefault(i => i.Name == name);
        if (item != null) item.Grayed = grayed;
        RefreshList();
    }

    public void SetItemCheckable(string name, bool checkable)
    {
        var item = _items.FirstOrDefault(i => i.Name == name);
        if (item != null) item.Checkable = checkable;
        RefreshList();
    }

    public void SetItemChecked(string name, bool check)
    {
        var item = _items.FirstOrDefault(i => i.Name == name);
        if (item != null) item.Checked = check;
        RefreshList();
    }

    public bool IsItemChecked(string name)
    {
        var item = _items.FirstOrDefault(i => i.Name == name);
        return item?.Checked ?? false;
    }

    public bool RemoveItem(string name)
    {
        var item = _items.FirstOrDefault(i => i.Name == name);
        if (item != null)
        {
            _items.Remove(item);
            RefreshList();
            return true;
        }
        return false;
    }

    public void ClearItems()
    {
        _items.Clear();
        if (_list != null) _list.RemoveChildren();
    }

    private void RefreshList()
    {
        if (_list == null) return;
        _list.NumItems = _items.Count(i => i.Visible);
    }

    public void Show(FGUIObject? target = null, PopupDirection direction = PopupDirection.Auto)
    {
        if (_contentPane == null) return;
        _popupTarget = target;
        
        // Position the popup
        if (target != null)
        {
            float x = target.X;
            float y = target.Y + target.Height;
            _contentPane.SetXY(x, y);
        }
        
        _contentPane.Visible = true;
        FGUIRoot.Instance.AddChild(_contentPane);
    }

    public void Hide()
    {
        if (_contentPane != null)
        {
            _contentPane.Visible = false;
            _contentPane.RemoveFromParent();
        }
    }

    public void Dispose()
    {
        _contentPane?.Dispose();
        _contentPane = null;
        _list = null;
        _items.Clear();
    }
}

public class PopupMenuItem
{
    public string Name { get; set; } = "";
    public string Caption { get; set; } = "";
    public Action? Callback { get; set; }
    public bool Visible { get; set; } = true;
    public bool Grayed { get; set; }
    public bool Checkable { get; set; }
    public bool Checked { get; set; }
    public bool IsSeparator { get; set; }
}

/// <summary>
/// Object pool for FGUIObject recycling
/// </summary>
public class FGUIObjectPool
{
    public delegate void InitCallback(FGUIObject obj);
    public InitCallback? OnInit;
    
    private readonly Dictionary<string, Queue<FGUIObject>> _pool = new();
    
    public int Count => _pool.Count;
    
    public FGUIObject? GetObject(string url)
    {
        url = FGUIPackage.NormalizeURL(url);
        if (string.IsNullOrEmpty(url)) return null;
        
        if (_pool.TryGetValue(url, out var queue) && queue.Count > 0)
        {
            return queue.Dequeue();
        }
        
        var obj = FGUIPackage.CreateObjectFromURL(url);
        if (obj != null)
        {
            OnInit?.Invoke(obj);
        }
        return obj;
    }
    
    public void ReturnObject(FGUIObject obj)
    {
        string? url = obj.ResourceUrl;
        if (string.IsNullOrEmpty(url)) return;
        
        obj.RemoveFromParent();
        
        if (!_pool.TryGetValue(url, out var queue))
        {
            queue = new Queue<FGUIObject>();
            _pool[url] = queue;
        }
        queue.Enqueue(obj);
    }
    
    public void Clear()
    {
        foreach (var kv in _pool)
        {
            foreach (var obj in kv.Value)
                obj.Dispose();
        }
        _pool.Clear();
    }
}

#endif
