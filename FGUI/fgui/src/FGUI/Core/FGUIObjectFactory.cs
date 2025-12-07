#if CLIENT
using SCEFGUI.UI;

namespace SCEFGUI.Core;

public static class FGUIObjectFactory
{
    private static readonly Dictionary<string, Func<FGUIComponent>> _extensions = new();
    private static readonly Dictionary<string, Type> _packageItemExtensions = new();
    private static Type? _loaderExtension;

    /// <summary>
    /// Set extension for a component URL
    /// </summary>
    public static void SetExtension(string url, Func<FGUIComponent> creator) => _extensions[url] = creator;

    /// <summary>
    /// Set extension class for a package item URL (like MailItem for virtual list)
    /// </summary>
    public static void SetPackageItemExtension(string url, Type type) => _packageItemExtensions[url] = type;

    /// <summary>
    /// Set custom loader extension class
    /// </summary>
    public static void SetLoaderExtension<T>() where T : FGUILoader, new() => _loaderExtension = typeof(T);

    /// <summary>
    /// Set custom loader extension class by type
    /// </summary>
    public static void SetLoaderExtension(Type type) => _loaderExtension = type;

    /// <summary>
    /// Clear loader extension
    /// </summary>
    public static void ClearLoaderExtension() => _loaderExtension = null;

    /// <summary>
    /// Try to get extension for a URL
    /// </summary>
    public static bool TryGetExtension(string url, out Func<FGUIComponent>? creator) => _extensions.TryGetValue(url, out creator);

    /// <summary>
    /// Try to create object from package item extension
    /// </summary>
    public static FGUIObject? TryCreateFromExtension(string url)
    {
        if (_packageItemExtensions.TryGetValue(url, out var type))
        {
            try
            {
                return Activator.CreateInstance(type) as FGUIObject;
            }
            catch { }
        }
        return null;
    }

    public static FGUIObject? NewObject(PackageItem item)
    {
        // Check if there's a package item extension for this URL
        string url = $"ui://{item.Owner?.Name}/{item.Name}";
        var extObj = TryCreateFromExtension(url);
        if (extObj != null)
            return extObj;

        return NewObject(item.ObjectType);
    }

    public static FGUIObject? NewObject(ObjectType type)
    {
        return type switch
        {
            ObjectType.Image => new FGUIImage(),
            ObjectType.MovieClip => new FGUIMovieClip(),
            ObjectType.Component => new FGUIComponent(),
            ObjectType.Text => new FGUITextField(),
            ObjectType.RichText => new FGUIRichTextField(),
            ObjectType.InputText => new FGUITextInput(),
            ObjectType.Group => new FGUIGroup(),
            ObjectType.List => new FGUIList(),
            ObjectType.Graph => new FGUIGraph(),
            ObjectType.Loader => CreateLoader(),
            ObjectType.Button => new FGUIButton(),
            ObjectType.Label => new FGUILabel(),
            ObjectType.ProgressBar => new FGUIProgressBar(),
            ObjectType.Slider => new FGUISlider(),
            ObjectType.ScrollBar => new FGUIScrollBar(),
            ObjectType.ComboBox => new FGUIComboBox(),
            ObjectType.Tree => new FGUITree(),
            _ => null
        };
    }

    private static FGUILoader CreateLoader()
    {
        if (_loaderExtension != null)
        {
            try
            {
                var loader = Activator.CreateInstance(_loaderExtension) as FGUILoader;
                if (loader != null)
                    return loader;
            }
            catch { }
        }
        return new FGUILoader();
    }
}
#endif
