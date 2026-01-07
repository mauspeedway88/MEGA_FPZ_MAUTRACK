using UnityEngine;

namespace Starter.Shooter
{
    /// <summary>
    /// Static class to persist game results between scenes.
    /// </summary>
    public static class GameResults
    {
        public static string WinnerName = "---";
        public static string LoserName = "---";

        public static void Reset()
        {
            WinnerName = "---";
            LoserName = "---";
        }
    }
}
