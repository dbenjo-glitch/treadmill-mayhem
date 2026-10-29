namespace TreadmillMayhem
{
    public enum GameMode { Race, ObstacleFree, ObstacleLevels }

    /// <summary>What the player picked on the menu. Survives scene reloads (Retry / Main Menu).</summary>
    public static class Session
    {
        public static GameMode Mode = GameMode.Race;
        public static int Level = 1;

        public static bool IsObstacle => Mode != GameMode.Race;

        public static string ModeLabel => Mode switch
        {
            GameMode.ObstacleFree => "OBSTACLE  •  FREE PLAY",
            GameMode.ObstacleLevels => "OBSTACLE  •  LEVEL " + Level,
            _ => "RACE",
        };
    }
}
