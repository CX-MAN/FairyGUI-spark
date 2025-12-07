using GameCore.GameSystem.Data;
#if CLIENT
using SCEFGUI;
using SCEFGUI.Render;
#endif

namespace GameEntry;

public class GlobalConfig : IGameClass
{
    public static void OnRegisterGameClass()
    {
#if CLIENT
        // Initialize FGUI system with SCE adapter
        Game.Logger.LogInformation("[FGUI] Initializing FGUIManager with SCEAdapter...");
        FGUIManager.Initialize(new SCEAdapter(), 1136, 640);
        Game.Logger.LogInformation($"[FGUI] FGUIManager initialized, Adapter: {SCERenderContext.Instance.Adapter != null}");
#endif
        
        /*// Register the game mod for the game system.
        // in non-testing (online) mode, the server will send game mode strings to the engine,
        // and the engine will use this to determine which game mode to use.
        GameDataGlobalConfig.AvailableGameModes = new()
        {
            // 默认游戏模式
            {"", GameCore.ScopeData.GameMode.Default},
        };
        GameDataGlobalConfig.TestGameMode = GameCore.ScopeData.GameMode.Default;
        GameDataGlobalConfig.SinglePlayerTestSlotId = 1;*/
    }
}
