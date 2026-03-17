namespace CultivationGame.Core
{
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        Meditating,
        InBreakthrough,
        InCombat
    }

    public enum RealmSubStage
    {
        None,
        I, II, III, IV, V, VI, VII, VIII, IX,
        Initial, Middle, Peak
    }

    public enum DaoType
    {
        Martial,
        Sword,
        Alchemy,
        Arrays,
        Lightning,
        Wind,
        Beast,
        Divination,
        Time,
        Space,
        Fate,
        Chaos,
        Creation
    }

    public enum DaoCategory
    {
        Basic,
        Advanced
    }

    public enum BiomeType
    {
        DeepVoid,
        StarSea,
        BarrenWastes,
        NebulaBeach,
        Plains,
        StoneMountain,
        SpiritForest,
        CelestialPeak
    }

    public enum POIType
    {
        Sect,
        Ruin,
        SpiritBeastDen,
        TreasureCache,
        MysticPortal,
        AncientStele,
        Resource,
        BuildingDiscovery
    }

    public enum TerrainType
    {
        Water,
        Land,
        Mountain
    }

    public enum InnerWorldTileType
    {
        Void,
        DivineSea,
        SpiritLake,
        Water,
        SpiritSoil,
        FertileEarth,
        SacredGrove,
        Land,
        StonePeak,
        CelestialPeak,
        Mountain
    }

    public enum BuildingTerrain
    {
        Water,
        Land,
        Mountain
    }

    public enum BuildingRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum SectRank
    {
        OuterDisciple,
        InnerDisciple,
        CoreDisciple,
        Elder,
        GrandElder
    }

    public enum BuffType
    {
        QiBoost,
        Speed,
        Vision
    }

    public enum MachineType
    {
        None,
        Furnace,
        Crusher,
        Mixer,
        Distiller,
        Condenser,
        PillPress,
        Storage
    }
}
