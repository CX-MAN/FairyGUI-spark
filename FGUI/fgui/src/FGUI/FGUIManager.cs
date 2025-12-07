#if CLIENT
using SCEFGUI.Core;
using SCEFGUI.Render;
using SCEFGUI.UI;

namespace SCEFGUI;

/// <summary>
/// 屏幕适配模式
/// </summary>
public enum ScreenMatchMode
{
    /// <summary>
    /// 取宽度和高度缩放比的较小值，保证UI完全显示在屏幕内
    /// </summary>
    MatchWidthOrHeight,
    /// <summary>
    /// 按宽度缩放
    /// </summary>
    MatchWidth,
    /// <summary>
    /// 按高度缩放
    /// </summary>
    MatchHeight
}

public static class FGUIManager
{
    private static bool _initialized;
    
    /// <summary>
    /// 设计分辨率宽度
    /// </summary>
    public static float DesignResolutionX { get; private set; } = 1136;
    
    /// <summary>
    /// 设计分辨率高度
    /// </summary>
    public static float DesignResolutionY { get; private set; } = 640;
    
    /// <summary>
    /// 屏幕适配模式
    /// </summary>
    public static ScreenMatchMode MatchMode { get; private set; } = ScreenMatchMode.MatchWidthOrHeight;
    
    /// <summary>
    /// 内容缩放因子（实际屏幕尺寸 / 设计分辨率）
    /// </summary>
    public static float ContentScaleFactor { get; private set; } = 1f;

    public static void Initialize(float designWidth = 1136, float designHeight = 640)
    {
        if (_initialized) return;
        DesignResolutionX = designWidth;
        DesignResolutionY = designHeight;
        UpdateContentScaleFactor();
        FGUIRoot.Instance.ApplyContentScaleFactor();
        _initialized = true;
    }

    public static void Initialize(ISCEAdapter adapter, float designWidth = 1136, float designHeight = 640)
    {
        SCERenderContext.Instance.Initialize(adapter);
        Initialize(designWidth, designHeight);
    }
    
    /// <summary>
    /// 设置内容缩放因子（设计分辨率适配）
    /// </summary>
    /// <param name="designResolutionX">设计分辨率宽度</param>
    /// <param name="designResolutionY">设计分辨率高度</param>
    /// <param name="matchMode">屏幕适配模式</param>
    public static void SetContentScaleFactor(float designResolutionX, float designResolutionY, 
        ScreenMatchMode matchMode = ScreenMatchMode.MatchWidthOrHeight)
    {
        DesignResolutionX = designResolutionX;
        DesignResolutionY = designResolutionY;
        MatchMode = matchMode;
        UpdateContentScaleFactor();
        FGUIRoot.Instance.ApplyContentScaleFactor();
    }
    
    /// <summary>
    /// 更新内容缩放因子
    /// </summary>
    internal static void UpdateContentScaleFactor()
    {
        var screenSize = SCERenderContext.Instance.Adapter?.GetScreenSize();
        if (!screenSize.HasValue || screenSize.Value.Width <= 0 || screenSize.Value.Height <= 0)
        {
            ContentScaleFactor = 1f;
            return;
        }
        
        float screenWidth = screenSize.Value.Width;
        float screenHeight = screenSize.Value.Height;
        
        if (DesignResolutionX <= 0 || DesignResolutionY <= 0)
        {
            ContentScaleFactor = 1f;
            return;
        }
        
        float s1 = screenWidth / DesignResolutionX;
        float s2 = screenHeight / DesignResolutionY;
        
        ContentScaleFactor = MatchMode switch
        {
            ScreenMatchMode.MatchWidth => s1,
            ScreenMatchMode.MatchHeight => s2,
            _ => Math.Min(s1, s2) // MatchWidthOrHeight
        };
        
        // 防止缩放因子过大
        if (ContentScaleFactor > 10)
            ContentScaleFactor = 10;
            
        Game.Logger.LogInformation($"[FGUI] ContentScaleFactor updated: {ContentScaleFactor:F3} (screen: {screenWidth}x{screenHeight}, design: {DesignResolutionX}x{DesignResolutionY})");
    }

    public static ISCEAdapter? Adapter
    {
        get => SCERenderContext.Instance.Adapter;
        set => SCERenderContext.Instance.Adapter = value;
    }

    public static FGUIPackage? AddPackage(string filePath, Func<string, string, byte[]?> loadFunc) =>
        FGUIPackage.AddPackage(filePath, (name, ext) => loadFunc(name, ext));

    public static FGUIPackage? AddPackage(byte[] data, string assetPrefix, Func<string, string, byte[]?> loadFunc) =>
        FGUIPackage.AddPackage(data, assetPrefix, (name, ext) => loadFunc(name, ext));

    public static FGUIPackage? GetPackage(string name) => FGUIPackage.GetByName(name);

    public static FGUIObject? CreateObject(string packageName, string componentName) =>
        FGUIPackage.GetByName(packageName)?.CreateObject(componentName);

    public static FGUIObject? CreateObject(string url)
    {
        // If it's a URL format, parse it
        if (url.StartsWith(FGUIPackage.URL_PREFIX))
        {
            var item = FGUIPackage.GetItemByURL(url);
            return item?.Owner?.CreateObject(item);
        }
        // Otherwise try to find package/component by name
        return null;
    }

    public static FGUIObject? CreateObjectFromURL(string url) =>
        FGUIPackage.GetItemByURL(url)?.Owner?.CreateObject(FGUIPackage.GetItemByURL(url)!);

    public static void RemovePackage(string packageIdOrName) => FGUIPackage.RemovePackage(packageIdOrName);
    public static void RemoveAllPackages() => FGUIPackage.RemoveAllPackages();
    public static FGUIRoot Root => FGUIRoot.Instance;
    
    /// <summary>
    /// Get item URL from package
    /// </summary>
    public static string? GetItemURL(string packageName, string itemName)
    {
        var pkg = FGUIPackage.GetByName(packageName);
        if (pkg == null) return null;
        var item = pkg.GetItemByName(itemName);
        if (item == null) return null;
        return FGUIPackage.URL_PREFIX + pkg.Id + item.Id;
    }
}
#endif
