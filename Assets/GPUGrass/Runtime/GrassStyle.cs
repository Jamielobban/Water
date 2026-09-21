using UnityEngine;

namespace GPUGrass
{
    public enum GrassStyle { SoftMeadow, ClassicMeadow, Storybook, LowPoly, DryPrairie, MoonlitFantasy, CelField, CurledRibbons, ReedBed, CrystalGarden, AnimeMeadow }

    public readonly struct GrassStyleSettings
    {
        public readonly Color root, tip;
        public readonly float height, width, curve, windSpeed, windScale, toon, emission;
        public GrassStyleSettings(Color root, Color tip, float height, float width, float curve, float speed, float wind, float toon = 0, float emission = 0)
        { this.root = root; this.tip = tip; this.height = height; this.width = width; this.curve = curve; windSpeed = speed; windScale = wind; this.toon = toon; this.emission = emission; }
        public static string Label(GrassStyle style)
        {
            switch (style)
            {
                case GrassStyle.ClassicMeadow: return "Classic Meadow";
                case GrassStyle.Storybook: return "Pastel Storybook";
                case GrassStyle.LowPoly: return "Low Poly";
                case GrassStyle.DryPrairie: return "Dry Prairie";
                case GrassStyle.MoonlitFantasy: return "Moonlit Fantasy";
                case GrassStyle.CelField: return "Cel Field";
                case GrassStyle.CurledRibbons: return "Curled Ribbons";
                case GrassStyle.ReedBed: return "Reed Bed";
                case GrassStyle.CrystalGarden: return "Crystal Garden";
                case GrassStyle.AnimeMeadow: return "Anime Meadow";
                default: return "Soft Meadow";
            }
        }
        public static GrassStyleSettings Get(GrassStyle style)
        {
            switch (style)
            {
                case GrassStyle.ClassicMeadow: return new GrassStyleSettings(new Color(.055f,.18f,.025f), new Color(.48f,.69f,.17f), 1.2f,.7f,.7f,1.5f,1.2f);
                case GrassStyle.Storybook: return new GrassStyleSettings(new Color(.19f,.34f,.23f), new Color(.76f,.84f,.57f), .9f,1.4f,1.3f,.65f,.7f);
                case GrassStyle.LowPoly: return new GrassStyleSettings(new Color(.08f,.25f,.10f), new Color(.49f,.76f,.24f), .85f,1.55f,.35f,.8f,.6f,1);
                case GrassStyle.DryPrairie: return new GrassStyleSettings(new Color(.27f,.19f,.075f), new Color(.86f,.69f,.34f), 1.55f,.7f,1.2f,.6f,1.3f);
                case GrassStyle.MoonlitFantasy: return new GrassStyleSettings(new Color(.055f,.10f,.24f), new Color(.25f,.84f,.85f), 1.15f,1.1f,1.4f,.55f,.8f,0,.4f);
                case GrassStyle.CelField: return new GrassStyleSettings(new Color(.035f,.16f,.14f), new Color(.72f,.95f,.22f), .85f,2.2f,.35f,.85f,.7f,1);
                case GrassStyle.CurledRibbons: return new GrassStyleSettings(new Color(.18f,.13f,.34f), new Color(.95f,.52f,.67f), 1.6f,3.2f,1,.6f,.65f);
                case GrassStyle.ReedBed: return new GrassStyleSettings(new Color(.17f,.23f,.08f), new Color(.64f,.59f,.22f), 2.1f,.5f,.2f,.5f,.5f);
                case GrassStyle.CrystalGarden: return new GrassStyleSettings(new Color(.055f,.09f,.26f), new Color(.24f,.9f,1), 1.3f,2.7f,0,1,0,0,.65f);
                case GrassStyle.AnimeMeadow: return new GrassStyleSettings(new Color(.065f,.27f,.19f), new Color(.66f,.83f,.35f), 1.15f,.65f,1,.75f,1.35f,1);
                default: return new GrassStyleSettings(new Color(.11f,.27f,.12f), new Color(.58f,.73f,.36f), 1,1,1,1,1);
            }
        }
    }
}
