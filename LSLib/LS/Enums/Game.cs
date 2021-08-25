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
        public static bool IsFW3(this Game game)
        {
            return (game == Game.DivinityOriginalSin2 
                || game == Game.DivinityOriginalSin2DE
                || game == Game.BaldursGate3);
        }

        public static PackageVersion PAKVersion(this Game game)
        {
            switch (game)
            {
                case Game.DivinityOriginalSin: return PackageVersion.V7;
                case Game.DivinityOriginalSinEE: return PackageVersion.V9;
                case Game.DivinityOriginalSin2: return PackageVersion.V10;
                case Game.DivinityOriginalSin2DE: return PackageVersion.V13;
                case Game.BaldursGate3: return PackageVersion.V16;
                default: return PackageVersion.V16;
            }
        }

        public static LSFVersion LSFVersion(this Game game)
        {
            if (game == Game.DivinityOriginalSin || game == Game.DivinityOriginalSinEE)
            {
                return Enums.LSFVersion.VerChunkedCompress;
            }
            else if (game == Game.DivinityOriginalSin2 || game == Game.DivinityOriginalSin2DE)
            {
                return Enums.LSFVersion.VerExtendedNodes;
            }
            else
            {
                return Enums.LSFVersion.VerBG3ExtendedHeader;
            }
        }

        public static LSXVersion LSXVersion(this Game game)
        {
            if (game == Game.BaldursGate3)
            {
                return Enums.LSXVersion.V4;
            }
            else
            {
                return Enums.LSXVersion.V3;
            }
        }
    }
}
