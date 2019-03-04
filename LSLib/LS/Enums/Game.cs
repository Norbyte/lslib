namespace LSLib.LS.Enums
{
    public enum Game
    {
        DivinityOriginalSin = 0,
        DivinityOriginalSinEE = 1,
        DivinityOriginalSin2 = 2,
        DivinityOriginalSin2DE = 3,
    };

    public static class GameEnumExtensions
    {
        public static bool IsDOS2(this Game game)
        {
            return (game == Game.DivinityOriginalSin2 || game == Game.DivinityOriginalSin2DE);
        }
    }
    

}
