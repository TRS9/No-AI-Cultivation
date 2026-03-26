namespace CultivationGame.Core
{
    public enum GameState
    {
        Playing,
        Paused
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

    public enum MachineType
    {
        None,
        Furnace,
        Crusher,
        Mixer,
        Distiller,
        Condenser,
        PillPress,
        Storage,
        SpiritPipe,
        ResourceExtractor,
        Splitter,
        Merger,
        QiConduit
    }

    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Return,
        Dead
    }
}
