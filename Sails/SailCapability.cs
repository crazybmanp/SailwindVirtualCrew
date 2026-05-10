using System;

namespace SailwindVirtualCrew
{
    [Flags]
    public enum SailCapability
    {
        None        = 0,
        Halyard     = 1 << 0,
        SimpleSheet = 1 << 1,
        JibSheet    = 1 << 2,
        SquareSheet = 1 << 3,
        Trim        = 1 << 4,
        All         = Halyard | SimpleSheet | JibSheet | SquareSheet | Trim,
    }

    public static class SailCapabilityExtensions
    {
        public static SailCapability GetCapabilities(this ICommonSailActions sail)
        {
            if (sail is SimpleSail)
                return SailCapability.Halyard | SailCapability.SimpleSheet | SailCapability.Trim;
            if (sail is DualSheetSail ds)
            {
                if (ds.getSubtype() == DualSheetSail.DualSheetSailSubtype.Jib)
                    return SailCapability.Halyard | SailCapability.JibSheet | SailCapability.Trim;
                if (ds.getSubtype() == DualSheetSail.DualSheetSailSubtype.Square)
                    return SailCapability.Halyard | SailCapability.SquareSheet | SailCapability.Trim;
            }
            return SailCapability.Halyard;
        }
    }
}
