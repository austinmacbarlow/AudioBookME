namespace AudiobookMaker;

static class Palette
{
    // Backgrounds
    public static readonly Color Base    = Color.FromArgb(248, 250, 248); // near-white
    public static readonly Color Surface = Color.FromArgb(237, 245, 239); // soft green tint
    public static readonly Color Input   = Color.White;
    public static readonly Color Border  = Color.FromArgb(188, 210, 194); // muted green-gray

    // Text
    public static readonly Color Text    = Color.FromArgb(22,  40,  28);  // very dark green
    public static readonly Color Subtext = Color.FromArgb(90,  110, 96);  // muted

    // Pine green family (buttons / accents)
    public static readonly Color Pine       = Color.FromArgb(45,  106, 79);  // main accent
    public static readonly Color PineDark   = Color.FromArgb(27,  67,  50);  // hover / pressed
    public static readonly Color PineLight  = Color.FromArgb(82,  183, 136); // secondary
    public static readonly Color PaleMint   = Color.FromArgb(212, 237, 218); // subtle highlight

    // Status
    public static readonly Color Red  = Color.FromArgb(185, 28, 28);
    public static readonly Color RedHover = Color.FromArgb(153, 27, 27);

    public static Color Hover(Color c) => Color.FromArgb(
        Math.Max(0, c.R - 18), Math.Max(0, c.G - 18), Math.Max(0, c.B - 18));
}
