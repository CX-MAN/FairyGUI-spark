using GameCore.GameSystem.Data;

namespace GameEntry;

public class TestTriggers : IGameClass
{
    private static void Game_OnGameTriggerInitialization()
    {
        Trigger<EventGameStart> trigger = new(static async (s, d) =>
        {
            Game.Logger.LogInformation("Hello World!");
#if SERVER
            Game.Logger.LogInformation("This is running on the server side.");
#elif CLIENT
            Game.Logger.LogInformation("This is running on the client side.");
            
            // Start FGUI Demo (Basics sample)
            Game.Logger.LogInformation("Starting FGUI Demo...");
            new SCEFGUI.Samples.BasicsMain().Start();
#endif
            return true;
        }, keepReference: true);
        trigger.Register(Game.Instance);
    }
    public static void OnRegisterGameClass()
    {
        // 如果游戏模式不是默认模式，则不注册触发器
        if (GameDataGlobalConfig.TestGameMode != GameCore.ScopeData.GameMode.Default)
        {
            return;
        }
        Game.OnGameTriggerInitialization += Game_OnGameTriggerInitialization;
    }
}
