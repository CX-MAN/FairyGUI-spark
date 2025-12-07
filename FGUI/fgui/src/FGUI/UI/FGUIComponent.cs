#if CLIENT
using System.Drawing;
using SCEFGUI.Core;
using SCEFGUI.Utils;

namespace SCEFGUI.UI;

public class FGUIComponent : FGUIObject
{
    protected readonly List<FGUIObject> _children = new();
    protected readonly List<FGUIController> _controllers = new();
    protected readonly List<FGUITransition> _transitions = new();
    protected int _sortingChildCount;
    protected ChildrenRenderOrder _childrenRenderOrder = ChildrenRenderOrder.Ascent;
    protected int _apexIndex;
    protected bool _boundsChanged;
    protected FGUIScrollPane? _scrollPane;
    protected RectangleF? _clipRect;
    protected OverflowType _overflow = OverflowType.Visible;
    protected Margin _margin;
    internal int BuildingDisplayList;
    internal FGUIController? _applyingController;

    public int NumChildren => _children.Count;
    public FGUIScrollPane? ScrollPane => _scrollPane;
    public OverflowType Overflow { get => _overflow; set => _overflow = value; }
    public Margin Margin { get => _margin; set { _margin = value; SetBoundsChangedFlag(); } }
    public bool Opaque { get; set; } = true;

    public FGUIObject AddChild(FGUIObject child) => AddChildAt(child, _children.Count);

    public FGUIObject AddChildAt(FGUIObject child, int index)
    {
        if (child.Parent == this) { SetChildIndex(child, index); }
        else
        {
            child.RemoveFromParent();
            child.Parent = this;
            int count = _children.Count;
            if (child.SortingOrder != 0) { _sortingChildCount++; index = GetInsertPosForSortingChild(child); }
            else if (_sortingChildCount > 0 && index > count - _sortingChildCount) index = count - _sortingChildCount;
            if (index > count) index = count;
            _children.Insert(index, child);
            ChildStateChanged(child);
            SetBoundsChangedFlag();
        }
        return child;
    }
    
    internal void ChildStateChanged(FGUIObject child)
    {
        if (NativeObject != null)
        {
            bool finalVisible = child.FinalVisible;
            if (child.NativeObject == null && finalVisible)
                Render.SCERenderContext.Instance.CreateNativeControl(child);
            if (child.NativeObject != null)
            {
                // Use AddChild/RemoveFromParent to control visibility (more reliable than Show/Hide in SCE)
                if (finalVisible)
                    Render.SCERenderContext.Instance.Adapter?.AddChild(NativeObject, child.NativeObject);
                else
                    Render.SCERenderContext.Instance.Adapter?.RemoveFromParent(child.NativeObject);
            }
        }
    }

    private int GetInsertPosForSortingChild(FGUIObject child)
    {
        for (int i = 0; i < _children.Count; i++)
            if (_children[i].SortingOrder > child.SortingOrder) return i;
        return _children.Count;
    }

    public FGUIObject RemoveChild(FGUIObject child, bool dispose = false)
    {
        int index = _children.IndexOf(child);
        if (index >= 0) return RemoveChildAt(index, dispose);
        return child;
    }

    public FGUIObject RemoveChildAt(int index, bool dispose = false)
    {
        if (index < 0 || index >= _children.Count) throw new ArgumentOutOfRangeException(nameof(index));
        var child = _children[index];
        child.Parent = null;
        if (child.SortingOrder != 0) _sortingChildCount--;
        _children.RemoveAt(index);
        if (child.NativeObject != null)
            Render.SCERenderContext.Instance.RemoveFromParent(child);
        SetBoundsChangedFlag();
        if (dispose) child.Dispose();
        return child;
    }

    public void RemoveChildren(int beginIndex = 0, int endIndex = -1, bool dispose = false)
    {
        if (endIndex < 0 || endIndex >= _children.Count) endIndex = _children.Count - 1;
        for (int i = endIndex; i >= beginIndex; i--) RemoveChildAt(i, dispose);
    }

    public FGUIObject GetChildAt(int index)
    {
        if (index < 0 || index >= _children.Count) throw new ArgumentOutOfRangeException(nameof(index));
        return _children[index];
    }

    public FGUIObject? GetChild(string name) => _children.FirstOrDefault(c => c.Name == name);
    public FGUIObject? GetChildById(string id) => _children.FirstOrDefault(c => c.Id == id);

    public FGUIObject? GetChildByPath(string path)
    {
        var parts = path.Split('.');
        FGUIComponent? current = this;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            current = current?.GetChild(parts[i]) as FGUIComponent;
            if (current == null) return null;
        }
        return current?.GetChild(parts[^1]);
    }

    public int GetChildIndex(FGUIObject child) => _children.IndexOf(child);

    public void SetChildIndex(FGUIObject child, int index)
    {
        int oldIndex = _children.IndexOf(child);
        if (oldIndex == -1) throw new ArgumentException("Not a child", nameof(child));
        if (child.SortingOrder != 0) return;
        int count = _children.Count;
        if (_sortingChildCount > 0 && index > count - _sortingChildCount - 1) index = count - _sortingChildCount - 1;
        if (oldIndex == index) return;
        _children.RemoveAt(oldIndex);
        if (index > _children.Count) index = _children.Count;
        _children.Insert(index, child);
        SetBoundsChangedFlag();
    }

    public void SwapChildren(FGUIObject child1, FGUIObject child2)
    {
        int index1 = _children.IndexOf(child1);
        int index2 = _children.IndexOf(child2);
        if (index1 < 0 || index2 < 0) throw new ArgumentException("Not a child");
        SwapChildrenAt(index1, index2);
    }

    public void SwapChildrenAt(int index1, int index2)
    {
        var child1 = _children[index1];
        var child2 = _children[index2];
        SetChildIndex(child1, index2);
        SetChildIndex(child2, index1);
    }

    public IReadOnlyList<FGUIObject> Children => _children.AsReadOnly();

    public int NumControllers => _controllers.Count;
    public FGUIController? GetController(string name) => _controllers.FirstOrDefault(c => c.Name == name);
    public FGUIController? GetControllerAt(int index) => (index >= 0 && index < _controllers.Count) ? _controllers[index] : null;
    public void AddController(FGUIController controller) { _controllers.Add(controller); controller.Parent = this; }
    public void RemoveController(FGUIController controller) { int index = _controllers.IndexOf(controller); if (index >= 0) { controller.Parent = null; _controllers.RemoveAt(index); } }

    public FGUITransition? GetTransition(string name) => _transitions.FirstOrDefault(t => t.Name == name);
    public FGUITransition? GetTransitionAt(int index) => (index >= 0 && index < _transitions.Count) ? _transitions[index] : null;

    public void ApplyController(FGUIController controller)
    {
        _applyingController = controller;
        foreach (var child in _children)
            child.HandleControllerChanged(controller);
        _applyingController = null;
    }

    public void ApplyAllControllers()
    {
        foreach (var controller in _controllers)
            ApplyController(controller);
    }

    public void SetBoundsChangedFlag() { if (!_boundsChanged) _boundsChanged = true; }
    public void EnsureBoundsCorrect() { if (_boundsChanged) UpdateBounds(); }
    protected virtual void UpdateBounds() => _boundsChanged = false;

    internal void ChildSortingOrderChanged(FGUIObject child, int oldValue, int newValue)
    {
        if (oldValue == 0) _sortingChildCount++;
        else if (newValue == 0) _sortingChildCount--;
        int oldIndex = _children.IndexOf(child);
        int index = GetInsertPosForSortingChild(child);
        if (oldIndex < index) index--;
        _children.RemoveAt(oldIndex);
        _children.Insert(index, child);
    }

    public override void ConstructFromResource()
    {
        var packageItem = PackageItem;
        if (packageItem?.RawData == null || packageItem.Owner == null) return;
        
        ByteBuffer buffer = packageItem.RawData;
        buffer.Seek(0, 0);
        UnderConstruct = true;
        
        // Block 0: Basic info
        SourceWidth = buffer.ReadInt();
        SourceHeight = buffer.ReadInt();
        InitWidth = SourceWidth;
        InitHeight = SourceHeight;
        SetSize(SourceWidth, SourceHeight);
        
        if (buffer.ReadBool()) { MinWidth = buffer.ReadInt(); MaxWidth = buffer.ReadInt(); MinHeight = buffer.ReadInt(); MaxHeight = buffer.ReadInt(); }
        if (buffer.ReadBool()) { float f1 = buffer.ReadFloat(); float f2 = buffer.ReadFloat(); SetPivot(f1, f2, buffer.ReadBool()); }
        if (buffer.ReadBool()) { _margin.Top = buffer.ReadInt(); _margin.Bottom = buffer.ReadInt(); _margin.Left = buffer.ReadInt(); _margin.Right = buffer.ReadInt(); }
        
        OverflowType overflow = (OverflowType)buffer.ReadByte();
        if (overflow == OverflowType.Scroll) { int savedPos = buffer.Position; buffer.Seek(0, 7); SetupScroll(buffer); buffer.Position = savedPos; }
        else SetupOverflowAndClip(overflow);
        if (buffer.ReadBool()) buffer.Skip(8);
        
        BuildingDisplayList = 1;

        // Block 1: Controllers
        buffer.Seek(0, 1);
        int controllerCount = buffer.ReadShort();
        for (int i = 0; i < controllerCount; i++)
        {
            int nextPos = buffer.ReadUshort() + buffer.Position;
            var controller = new FGUIController();
            _controllers.Add(controller);
            controller.Parent = this;
            controller.Setup(buffer);
            buffer.Position = nextPos;
        }

        // Block 2: Children - Setup_BeforeAdd
        buffer.Seek(0, 2);
        int childCount = buffer.ReadShort();
        for (int i = 0; i < childCount; i++)
        {
            int dataLen = buffer.ReadShort();
            int curPos = buffer.Position;
            buffer.Seek(curPos, 0);
            ObjectType type = (ObjectType)buffer.ReadByte();
            string? src = buffer.ReadS();
            string? pkgId = buffer.ReadS();
            PackageItem? pi = null;
            if (src != null)
            {
                FGUIPackage? pkg = pkgId != null ? FGUIPackage.GetById(pkgId) : packageItem.Owner;
                pi = pkg?.GetItem(src);
            }
            FGUIObject? child = pi != null ? FGUIObjectFactory.NewObject(pi) : FGUIObjectFactory.NewObject(type);
            if (child != null)
            {
                if (pi != null) { child.PackageItem = pi; pi.Owner?.GetItemAsset(pi); }
                child.UnderConstruct = true;
                child.Setup_BeforeAdd(buffer, curPos);
                child.Parent = this;
                _children.Add(child);
            }
            buffer.Position = curPos + dataLen;
        }

        // Block 3: Component's own relations (parent to child)
        buffer.Seek(0, 3);
        InitRelations();
        Relations!.Setup(buffer, true);

        // Read child relations from Block 2's sub-block 3
        buffer.Seek(0, 2);
        buffer.Skip(2);
        for (int i = 0; i < childCount; i++)
        {
            int nextPos = buffer.ReadUshort();
            nextPos += buffer.Position;
            
            // Seek to sub-block 3 within this child's data (child relations)
            buffer.Seek(buffer.Position, 3);
            _children[i].InitRelations();
            _children[i].Relations!.Setup(buffer, false);
            
            buffer.Position = nextPos;
        }

        // Children - Setup_AfterAdd
        buffer.Seek(0, 2);
        buffer.Skip(2);
        for (int i = 0; i < childCount; i++)
        {
            int nextPos = buffer.ReadUshort();
            nextPos += buffer.Position;
            _children[i].Setup_AfterAdd(buffer, buffer.Position);
            _children[i].UnderConstruct = false;
            buffer.Position = nextPos;
        }

        // Block 4: Mask, custom data, opaque
        buffer.Seek(0, 4);
        buffer.Skip(2);
        Opaque = buffer.ReadBool();
        
        // Block 5: Transitions
        if (buffer.Seek(0, 5))
        {
            int transitionCount = buffer.ReadShort();
            for (int i = 0; i < transitionCount; i++)
            {
                int nextPos = buffer.ReadUshort() + buffer.Position;
                if (nextPos > buffer.Length) break;
                var transition = new FGUITransition();
                transition.Owner = this;
                transition.Setup(buffer);
                _transitions.Add(transition);
                buffer.Position = nextPos;
            }
        }
        
        // Apply all controllers
        ApplyAllControllers();
        
        BuildingDisplayList = 0;
        UnderConstruct = false;
        
        SetBoundsChangedFlag();
        EnsureBoundsCorrect();

        // Call ConstructExtension for extended types (Button, Label, etc.)
        // This is called AFTER all children are set up, so we can access their properties
        if (packageItem.ObjectType != ObjectType.Component)
            ConstructExtension(buffer);

        // Recursively construct children that are components
        foreach (var child in _children)
        {
            if (child is FGUIComponent comp)
                comp.ConstructFromResource();
        }
        
        Game.Logger.LogInformation($"[FGUI] Component parsed: {Name ?? PackageItem?.Name}, Size: {_width}x{_height}, Children: {_children.Count}, Controllers: {_controllers.Count}");
    }

    protected virtual void ConstructExtension(ByteBuffer buffer) { }

    protected void SetupScroll(ByteBuffer buffer)
    {
        _scrollPane = new FGUIScrollPane { Owner = this };
        _scrollPane.Setup(buffer);
    }

    protected void SetupOverflowAndClip(OverflowType overflow)
    {
        _overflow = overflow;
        if (overflow == OverflowType.Hidden) _clipRect = new RectangleF(0, 0, _width, _height);
    }

    public override void Dispose()
    {
        foreach (var child in _children) child.Dispose();
        _children.Clear();
        _scrollPane?.Dispose();
        _scrollPane = null;
        foreach (var transition in _transitions) transition.Dispose();
        _transitions.Clear();
        base.Dispose();
    }
}

public struct Margin
{
    public int Left, Right, Top, Bottom;
    public Margin(int left, int right, int top, int bottom) { Left = left; Right = right; Top = top; Bottom = bottom; }
}
#endif
