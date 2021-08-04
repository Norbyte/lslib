namespace LSLib.LS.Enums
{
    public enum Game
    {
        DivinityOriginalSin = 0,
        DivinityOriginalSinEE = 1,
        DivinityOriginalSin2 = 2,
        DivinityOriginalSin2DE = 3,
        BaldursGate3 = 4
    };

    public static class GameEnumExtensions
    {
        public static bool IsDOS2(this Game game)
        {
            return (game == Game.DivinityOriginalSin2 || game == Game.DivinityOriginalSin2DE);
        }

        public static bool IsFW3(this Game game)
        {
            return (game == Game.DivinityOriginalSin2 
                || game == Game.DivinityOriginalSin2DE
                || game == Game.BaldursGate3);
        }

        public static FileVersion LSFVersion(this Game game)
        {
            if (game == Game.DivinityOriginalSin || game == Game.DivinityOriginalSinEE)
            {
                return FileVersion.VerChunkedCompress;
            }
            else if (game == Game.DivinityOriginalSin2 || game == Game.DivinityOriginalSin2DE)
            {
                return FileVersion.VerExtendedNodes;
            }
            else
            {
                return FileVersion.VerExtendedHeader;
            }
        }
    }
    

}
