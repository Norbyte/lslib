namespace LSLib.LS.Enums;
public enum Game
{
    DivinityOriginalSin = 0,
    DivinityOriginalSinEE = 1,
    DivinityOriginalSin2 = 2,
    DivinityOriginalSin2DE = 3,
    BaldursGate3 = 4,
    Unset = 5
};

public static class GameEnumExtensions
{
    public static bool IsFW3(this Game game)
    {
        return game != Game.DivinityOriginalSin
            && game != Game.DivinityOriginalSinEE;
    }

    public static PackageVersion PAKVersion(this Game game)
    {
        switch (game)
        {
            case Game.DivinityOriginalSin: return PackageVersion.V7;
            case Game.DivinityOriginalSinEE: return PackageVersion.V9;
            case Game.DivinityOriginalSin2: return PackageVersion.V10;
            case Game.DivinityOriginalSin2DE: return PackageVersion.V13;
            case Game.BaldursGate3: return PackageVersion.V18;
            default: return PackageVersion.V18;
        }
    }

    public static LSFVersion LSFVersion(this Game game)
    {
        switch (game)
        {
            case Game.DivinityOriginalSin: return Enums.LSFVersion.VerChunkedCompress;
            case Game.DivinityOriginalSinEE: return Enums.LSFVersion.VerChunkedCompress;
            case Game.DivinityOriginalSin2: return Enums.LSFVersion.VerExtendedNodes;
            case Game.DivinityOriginalSin2DE: return Enums.LSFVersion.VerExtendedNodes;
            case Game.BaldursGate3: return Enums.LSFVersion.VerBG3Patch3;
            default: return Enums.LSFVersion.VerBG3Patch3;
        }
    }

    public static LSXVersion LSXVersion(this Game game)
    {
        switch (game)
        {
            case Game.DivinityOriginalSin:
            case Game.DivinityOriginalSinEE:
            case Game.DivinityOriginalSin2:
            case Game.DivinityOriginalSin2DE: 
                return Enums.LSXVersion.V3;

            case Game.BaldursGate3: 
                return Enums.LSXVersion.V4;

            default: 
                return Enums.LSXVersion.V4;
        }
    }
}
