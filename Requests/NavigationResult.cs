using System;

namespace SailwindVirtualCrew
{
    public class NavigationResult
    {
        public string MethodLabel   { get; }
        public bool HasLatitude  { get; }
        public bool HasLongitude { get; }
        public string LatitudeText  { get; }
        public string LongitudeText { get; }

        public NavigationResult(NavigationMethod method, bool hasLat, float lat, bool hasLon, float lon)
        {
            MethodLabel   = method.ToString();
            HasLatitude  = hasLat;
            HasLongitude = hasLon;
            LatitudeText  = hasLat ? FormatLat(lat) : null;
            LongitudeText = hasLon ? FormatLon(lon) : null;
        }

        private static float RoundToQuarterDegree(float v) =>
            (float)Math.Round(v / 0.25) * 0.25f;

        private static string FormatLat(float lat)
        {
            lat = RoundToQuarterDegree(lat);
            string hemi = lat < 0 ? "S" : "N";
            lat = Math.Abs(lat);
            int deg = (int)lat;
            int min = (int)Math.Round((lat - deg) * 60);
            return $"{deg}° {min:D2}' {hemi}";
        }

        private static string FormatLon(float lon)
        {
            lon = RoundToQuarterDegree(lon);
            string hemi = lon < 0 ? "W" : "E";
            lon = Math.Abs(lon);
            int deg = (int)lon;
            int min = (int)Math.Round((lon - deg) * 60);
            return $"{deg}° {min:D2}' {hemi}";
        }
    }
}
