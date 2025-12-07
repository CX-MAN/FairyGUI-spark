#if CLIENT
using SCEFGUI.Core;
using SCEFGUI.Event;
using SCEFGUI.Utils;

namespace SCEFGUI.UI;

public class FGUIController : EventDispatcher
{
    public string Name { get; set; } = "";
    public FGUIComponent? Parent { get; internal set; }

    private int _selectedIndex = -1;
    private int _previousIndex = -1;
    private readonly List<string> _pageIds = new();
    private readonly List<string> _pageNames = new();
    private bool _changing;

    public int PageCount => _pageIds.Count;
    public bool Changing => _changing;

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex != value)
            {
                if (value >= _pageIds.Count) throw new ArgumentOutOfRangeException(nameof(value));
                _changing = true;
                _previousIndex = _selectedIndex;
                _selectedIndex = value;
                Game.Logger.LogInformation($"[FGUI] Controller '{Name}' changed: {_previousIndex} -> {_selectedIndex}, pageId={SelectedPageId}");
                Parent?.ApplyController(this);
                DispatchEvent("onChange", null);
                _changing = false;
            }
        }
    }

    public int PreviousIndex => _previousIndex;
    public string SelectedPage { get => _selectedIndex >= 0 && _selectedIndex < _pageNames.Count ? _pageNames[_selectedIndex] : ""; set { int index = _pageNames.IndexOf(value); if (index >= 0) SelectedIndex = index; } }
    public string PreviousPage => _previousIndex >= 0 && _previousIndex < _pageNames.Count ? _pageNames[_previousIndex] : "";
    public string SelectedPageId { get => _selectedIndex >= 0 && _selectedIndex < _pageIds.Count ? _pageIds[_selectedIndex] : ""; set { int index = _pageIds.IndexOf(value); if (index >= 0) SelectedIndex = index; } }

    public string GetPageName(int index) => index >= 0 && index < _pageNames.Count ? _pageNames[index] : "";
    public string GetPageId(int index) => index >= 0 && index < _pageIds.Count ? _pageIds[index] : "";
    public int GetPageIndexById(string id) => _pageIds.IndexOf(id);
    public int GetPageIndexByName(string name) => _pageNames.IndexOf(name);
    public void AddPage(string name = "") => AddPageAt(name, _pageIds.Count);

    public void AddPageAt(string name, int index)
    {
        string id = $"p{_pageIds.Count}";
        if (index < 0 || index > _pageIds.Count) index = _pageIds.Count;
        _pageIds.Insert(index, id);
        _pageNames.Insert(index, name);
    }

    public void RemovePage(string name) { int index = _pageNames.IndexOf(name); if (index >= 0) RemovePageAt(index); }
    public void RemovePageAt(int index) { _pageIds.RemoveAt(index); _pageNames.RemoveAt(index); if (_selectedIndex >= _pageIds.Count) SelectedIndex = _pageIds.Count - 1; }
    public void ClearPages() { _pageIds.Clear(); _pageNames.Clear(); _selectedIndex = -1; }
    public bool HasPage(string name) => _pageNames.Contains(name);

    public void Setup(ByteBuffer buffer)
    {
        int beginPos = buffer.Position;
        
        // Block 0: Name and autoRadioGroupDepth
        if (!buffer.Seek(beginPos, 0)) return;
        Name = buffer.ReadS() ?? "";
        buffer.ReadBool(); // autoRadioGroupDepth
        
        // Block 1: Pages
        if (!buffer.Seek(beginPos, 1)) return;
        int pageCount = buffer.ReadShort();
        for (int i = 0; i < pageCount; i++)
        {
            _pageIds.Add(buffer.ReadS() ?? "");
            _pageNames.Add(buffer.ReadS() ?? "");
        }
        
        // Determine home page index
        int homePageIndex = 0;
        if (buffer.Version >= 2)
        {
            int homePageType = buffer.ReadByte();
            switch (homePageType)
            {
                case 1: // Specific index
                    homePageIndex = buffer.ReadShort();
                    break;
                case 2: // By branch
                case 3: // By variable
                    buffer.ReadS(); // Skip
                    break;
            }
        }
        
        // Block 2: Actions
        if (buffer.Seek(beginPos, 2))
        {
            int actionCount = buffer.ReadShort();
            for (int i = 0; i < actionCount; i++)
            {
                int nextPos = buffer.ReadUshort() + buffer.Position;
                // Skip action data
                buffer.Position = nextPos;
            }
        }
        
        if (Parent != null && _pageIds.Count > 0)
            _selectedIndex = homePageIndex;
    }
}
#endif
