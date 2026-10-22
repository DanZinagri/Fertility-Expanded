using System;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace FertilityExpanded.Patches.Compatibility;

internal static class LactationExpansionCompat
{
    private static bool lookedForPawnIsLactating;
    private static MethodInfo pawnIsLactatingMethod;

    public static bool HasActiveLactation(Pawn pawn)
    {
        if (!ModDetection.LactationExpansionActive || pawn == null)
        {
            return false;
        }

        MethodInfo method = PawnIsLactatingMethod;
        if (method == null)
        {
            return false;
        }

        try
        {
            return method.Invoke(null, new object[] { pawn }) is bool result && result;
        }
        catch (Exception ex)
        {
            Log.WarningOnce("[Fertility Expanded] Lactation Expansion compatibility failed while checking active lactation: " + ex.Message, 17493201);
            return false;
        }
    }

    private static MethodInfo PawnIsLactatingMethod
    {
        get
        {
            if (!lookedForPawnIsLactating)
            {
                lookedForPawnIsLactating = true;
                Type type = AccessTools.TypeByName("EuterpeMilkyTitfuck.LactationUtilities");
                pawnIsLactatingMethod = type == null ? null : AccessTools.Method(type, "PawnIsLactating", new[] { typeof(Pawn) });
                if (pawnIsLactatingMethod == null)
                {
                    Log.WarningOnce("[Fertility Expanded] Lactation Expansion is active, but EuterpeMilkyTitfuck.LactationUtilities.PawnIsLactating(Pawn) was not found. Vanilla lactation support will still work, but Lactation Expansion lactation states will not count for Fertility Expanded support checks.", 17493202);
                }
            }

            return pawnIsLactatingMethod;
        }
    }
}
