using GameCore.ActorSystem.Data;
using GameCore.AISystem.Data.Enum;
using GameCore.CameraSystem.Enum;
using GameCore.GameSystem.Data;
using GameCore.GameSystem.Enum;
using GameCore.StatusBarSystem;

using GameUI.CameraSystem.Data;
using GameUI.Data;

using static GameCore.ScopeData;

namespace GameEntry;

internal partial class ProjectData : IGameClass
{
    public static void OnRegisterGameClass()
    {
        Game.OnGameDataInitialization += OnGameDataInitialization;
    }

    public static class Formular
    {
#if SERVER
        public static bool DefaultDamageNotificationPredicate(Player player, Damage damage)
        {
            // 仅当伤害值大于0时才通知
            if (damage.Current <= 0)
            {
                return false;
            }
            // 仅当玩家是伤害来源或伤害目标时才通知
            var playerCheck = damage.CasterPlayer == player || damage.Target?.Player == player;
            if (!playerCheck)
            {
                return false;
            }
            // 仅当伤害目标对玩家可见且处于同一场景时才通知
            var visibilityCheck = damage.Target != null && damage.Target.IsVisibleToTransient(player);
            return visibilityCheck;
        }
#endif
    }
    public static class Camera
    {
        public static readonly GameLink<GameDataCamera, GameDataCamera> DefaultCamera = new("DefaultCamera"u8);
        public static readonly GameLink<GameDataCamera, GameDataCamera> MoveableCamera = new("MoveableCamera"u8);
    }

    private static void OnGameDataInitialization()
    {
        _ = new GameDataActorScope(ActorScope.Default)
        {
            Name = "Default Actor Scope",
        };
        _ = new GameDataUnitProperty(UnitProperty.LifeMax)
        {
            Name = "Life Max",
        };
        _ = new GameDataUnitProperty(UnitProperty.ManaMax)
        {
            Name = "Mana Max",
        };
        _ = new GameDataUnitProperty(UnitProperty.AttackRange)
        {
            Name = "Attack Range",
        };
        _ = new GameDataUnitProperty(UnitProperty.AttackDamage)
        {
            Name = "Attack Damage",
        };
        _ = new GameDataUnitProperty(UnitProperty.Sight)
        {
            Name = "Sight Range",
        };
        _ = new GameDataUnitProperty(UnitProperty.Armor)
        {
            Name = "Armor",
        };
        _ = new GameDataUnitProperty(UnitProperty.MagicResistance)
        {
            Name = "Magic Resistance",
        };
        _ = new GameDataUnitProperty(UnitProperty.MoveSpeed)
        {
            Name = "Move Speed",
        };
        _ = new GameDataUnitProperty(UnitProperty.ShrubSight)
        {
            Name = "Shrub Sight",
        };
        _ = new GameDataUnitProperty(UnitProperty.Height)
        {
            Name = "Height",
        };
        _ = new GameDataUnitProperty(UnitProperty.SightBlockRadius)
        {
            Name = "Sight Block Radius",
        };
        _ = new GameDataUnitProperty(UnitProperty.TurningSpeed)
        {
            Name = "Turning Speed",
        };
        _ = new GameDataUnitProperty(UnitProperty.InventoryPickUpRange)
        {
            Name = "Inventory Pick Up Range"
        };
        _ = new GameDataUnitProperty(UnitProperty.LevelMax)
        {
            Name = "Attackable Radius",
        };
        _ = new GameDataUnitProperty(UnitProperty.ExperienceDistributionMultiplier)
        {
            Name = "Experience Distribution Multiplier",
        };
        _ = new GameDataCamera(Camera.DefaultCamera)
        {
            Name = "Default Camera",
            TargetZOffset = 10,
            Rotation = new(-90, -70, 0),
            TargetX = 2500,
            TargetY = 2500,
            FollowMainUnitByDefault = true,
            DisplayDebugInfo = true,
        };
        _ = new GameDataCamera(Camera.MoveableCamera)
        {
            Name = "Moveable Camera",
            TargetZOffset = 10,
            Rotation = new(-90, -70, 0),
            TargetX = 2500,
            TargetY = 2500,
            FollowMainUnitByDefault = false,
            DisplayDebugInfo = true,
            TargetingMode = CameraTargetingMode.Gesture,
        };
        _ = new GameDataGameMode(GameMode.Default)
        {
            Name = "Default Mode",
            Gameplay = Gameplay.Default,
            PlayerSettings = ScopeData.GameDataPlayerSettings.PlayerSettings,
            SceneList = [
            ],
            GameUI       = GameUI.ScopeData.GameUI.Default,
            DefaultScene = null,
        };

        _ = new GameDataDamageType(DamageType.Physical)
        {
            Name = "Physical Damage",
#if SERVER
            // 自定义伤害公式
            // CustomFormular =
            // 自定义暴击公式
            // CustomFormularIsCritical
            NotificationPredicate = Formular.DefaultDamageNotificationPredicate,
#endif
            FloatingTextDealt = GameCore.ScopeData.FloatingText.PhysicalDamage,
            FloatingTextDealtCritical = GameCore.ScopeData.FloatingText.CriticalPhysicalDamage,
            FloatingTextReceived = GameCore.ScopeData.FloatingText.DamageReceived,
        };
        _ = new GameDataDamageType(DamageType.Magical)
        {
            Name = "Magical Damage",
#if SERVER
            NotificationPredicate = Formular.DefaultDamageNotificationPredicate,
#endif
            FloatingTextDealt = GameCore.ScopeData.FloatingText.MagicDamage,
            FloatingTextDealtCritical = GameCore.ScopeData.FloatingText.CriticalMagicDamage,
            FloatingTextReceived = GameCore.ScopeData.FloatingText.DamageReceived,
        };
        _ = new GameDataDamageType(DamageType.Pure)
        {
            Name = "Pure Damage",
#if SERVER
            NotificationPredicate = Formular.DefaultDamageNotificationPredicate,
#endif
            FloatingTextDealt = GameCore.ScopeData.FloatingText.PureDamage,
            FloatingTextReceived = GameCore.ScopeData.FloatingText.DamageReceived,
        };
        // 浮动文本，暂时无法通过代码生成，需要手动创建
        _ = new GameDataFloatingText(GameCore.ScopeData.FloatingText.PhysicalDamage)
        {
            Name = "Physical Damage",
        };
        _ = new GameDataFloatingText(GameCore.ScopeData.FloatingText.CriticalPhysicalDamage)
        {
            Name = "Critical Physical Damage",
        };
        _ = new GameDataFloatingText(GameCore.ScopeData.FloatingText.PhysicalAccumulated)
        {
            Name = "Physical Accumulated",
        };
        _ = new GameDataFloatingText(GameCore.ScopeData.FloatingText.MagicDamage)
        {
            Name = "Magic Damage",
        };
        _ = new GameDataFloatingText(GameCore.ScopeData.FloatingText.CriticalMagicDamage)
        {
            Name = "Critical Magic Damage",
        };
        _ = new GameDataFloatingText(GameCore.ScopeData.FloatingText.MagicAccumulated)
        {
            Name = "Magic Accumulated",
        };
        _ = new GameDataFloatingText(GameCore.ScopeData.FloatingText.PureDamage)
        {
            Name = "Pure Damage",
        };
        _ = new GameDataFloatingText(GameCore.ScopeData.FloatingText.DamageReceived)
        {
            Name = "Damage Received",
        };
        _ = new GameDataFloatingText(GameCore.ScopeData.FloatingText.Heal)
        {
            Name = "Heal",
        };
        _ = new GameDataFloatingText(GameCore.ScopeData.FloatingText.ManaSpent)
        {
            Name = "Mana Cost",
        };
        _ = new GameDataFloatingText(GameCore.ScopeData.FloatingText.Gold)
        {
            Name = "Gold",
        };
        _ = new GameDataFloatingText(GameCore.ScopeData.FloatingText.Exp)
        {
            Name = "Exp",
        };
        _ = new GameDataFloatingText(GameCore.ScopeData.FloatingText.Missed)
        {
            Name = "Missed",
        };

        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.EnemyHeroNone)
        {
            Name = "Enemy Hero None",
        };
        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.NeutralHeroNone)
        {
            Name = "Neutral Hero None",
        };
        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.MainHeroNone)
        {
            Name = "Main Hero None",
        };
        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.AllyHeroNone)
        {
            Name = "Ally Hero None",
        };
        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.EnemyHeroMana)
        {
            Name = "Enemy Hero Mana",
        };
        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.NeutralHeroMana)
        {
            Name = "Neutral Hero Mana",
        };
        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.MainHeroMana)
        {
            Name = "Main Hero Mana",
        };
        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.AllyHeroMana)
        {
            Name = "Ally Hero Mana",
        };
        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.EnemyHeroExp)
        {
            Name = "Enemy Hero Exp",
        };
        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.NeutralHeroExp)
        {
            Name = "Neutral Hero Exp",
        };
        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.MainHeroExp)
        {
            Name = "Main Hero Exp",
        };
        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.AllyHeroExp)
        {
            Name = "Ally Hero Exp",
        };
        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.AllyHeroAnger)
        {
            Name = "Ally Hero Anger",
        };
        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.AllyHeroEnergy)
        {
            Name = "Ally Hero Energy",
        };
        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.EnemyHeroAnger)
        {
            Name = "Enemy Hero Anger",
        };
        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.EnemyHeroEnergy)
        {
            Name = "Enemy Hero Energy",
        };
        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.NeutralHeroAnger)
        {
            Name = "Neutral Hero Anger",
        };
        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.NeutralHeroEnergy)
        {
            Name = "Neutral Hero Energy",
        };
        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.MainHeroAnger)
        {
            Name = "Main Hero Anger",
        };
        _ = new GameDataStatusBar(GameCore.ScopeData.StatusBar.MainHeroEnergy)
        {
            Name = "Main Hero Energy",
        };
        _ = new GameDataGameUI(GameUI.ScopeData.GameUI.Default)
        {
            Name = "Default Game UI",
            StandardUIRenderingOrder = [
                // 基础游戏控制层 - 最底层，基础交互元素
                StandardUIType.Joystick,
                StandardUIType.Map,
                
                // 游戏内常驻UI层 - 游戏进行中始终可见的UI元素
                StandardUIType.Minimap,
                StandardUIType.Hotbar,
                StandardUIType.StatusBar,
                
                // 信息面板层 - 游戏核心信息界面
                StandardUIType.CharacterSheet,
                StandardUIType.Inventory,
                StandardUIType.Quest,
                StandardUIType.QuestLog,
                StandardUIType.Crafting,
                StandardUIType.TalentTree,
                
                // 系统功能层 - 系统级功能界面
                StandardUIType.Leaderboards,
                StandardUIType.Settings,
                StandardUIType.Shop,
                StandardUIType.Party,
                StandardUIType.Social,
                StandardUIType.Achievement,
                
                // 交互反馈层 - 即时交互和反馈
                StandardUIType.Chat,
                StandardUIType.Dialogue,
                
                // 通知提示层 - 重要信息通知
                StandardUIType.Notifications,
                
                // 教程引导层 - 新手引导和帮助
                StandardUIType.Tutorial,
                
                // 游戏内模态层 - 需要暂停游戏的选择界面
                StandardUIType.Reward,
                
                // 全屏模态层 - 覆盖所有游戏内容的界面
                StandardUIType.MainMenu,
                
                // 最高优先级层 - 系统级对话框，不应被任何UI遮挡
                StandardUIType.ConfirmDialog,
                StandardUIType.ErrorDialog,
                StandardUIType.Loading,
                ],
            StandardUIBaseZIndex = 0,
            StandardUIZIndexStep = 100,
        };
        var defaultWaveAI = new GameLink<GameDataWaveAI, GameDataWaveAI>("default"u8);
        _ = new GameDataWaveAI(defaultWaveAI)
        {
            // ⚔️ 战斗配置 - 启用AI战斗功能
            EnableCombat = true,

            // 🔍 扫描和攻击范围配置
            MinimalScanRange = 500f,
            MaximalScanRange = 1000f,
            MinimalApproachRange = 200f,

            // 🏃‍♂️ 牵引和撤退配置
            CombatLeash = 1500f,
            CombatResetRange = 1800f,

            // ⏱️ 战斗持续时间
            InCombatMinimalDuration = TimeSpan.FromSeconds(2),

            // 🌊 群体AI配置
            EnableWaveFormation = false, // 个体战斗不需要编队
            EnableLinkedAggro = true,    // 启用连锁仇恨

            // 🔄 AI生命周期
            AutoDisposeOnEmpty = true,

            // 📍 默认行为类型
            Type = WaveType.Guard,
        };
        _ = new GameDataGameplay(Gameplay.Default)
        {
            Name = "Default Gameplay",
            DefaultWaveAI = defaultWaveAI,
            ItemQualityList = [
                ItemQuality.Poor,
                ItemQuality.Common,
                ItemQuality.Uncommon,
                ItemQuality.Rare,
                ItemQuality.Epic,
                ItemQuality.Legendary,
                ItemQuality.Mythic,
            ],
        };
    }
}
