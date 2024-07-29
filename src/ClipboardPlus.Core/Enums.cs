namespace ClipboardPlus.Core;

public enum CbContentType
{
    Text,
    Image,
    Files,
    Other,
}

public enum CbOrders
{
    Score,
    CreateTime,
    SourceApplication,
    Type,
}

public enum CbResultType
{
    Record,
    Clear,
}

public enum KeepTime
{
    Always,
    Hour1,
    Hours12,
    Hours24,
    Days3,
    Days7,
    Month1,
    Months6,
    Year1
}

public class CbColors
{
    public const string Transparent = "transparent";
    public const string Black = "#000";
    public const string White = "#fff";
    public const string Slate50 = "#f8fafc";
    public const string Slate100 = "#f1f5f9";
    public const string Slate200 = "#e2e8f0";
    public const string Slate300 = "#cbd5e1";
    public const string Slate400 = "#94a3b8";
    public const string Slate500 = "#64748b";
    public const string Slate600 = "#475569";
    public const string Slate700 = "#334155";
    public const string Slate800 = "#1e293b";
    public const string Slate900 = "#0f172a";
    public const string Slate950 = "#020617";
    public const string Gray50 = "#f9fafb";
    public const string Gray100 = "#f3f4f6";
    public const string Gray200 = "#e5e7eb";
    public const string Gray300 = "#d1d5db";
    public const string Gray400 = "#9ca3af";
    public const string Gray500 = "#6b7280";
    public const string Gray600 = "#4b5563";
    public const string Gray700 = "#374151";
    public const string Gray800 = "#1f2937";
    public const string Gray900 = "#111827";
    public const string Gray950 = "#030712";
    public const string Zinc50 = "#fafafa";
    public const string Zinc100 = "#f4f4f5";
    public const string Zinc200 = "#e4e4e7";
    public const string Zinc300 = "#d4d4d8";
    public const string Zinc400 = "#a1a1aa";
    public const string Zinc500 = "#71717a";
    public const string Zinc600 = "#52525b";
    public const string Zinc700 = "#3f3f46";
    public const string Zinc800 = "#27272a";
    public const string Zinc900 = "#18181b";
    public const string Zinc950 = "#09090b";
    public const string Neutral50 = "#fafafa";
    public const string Neutral100 = "#f5f5f5";
    public const string Neutral200 = "#e5e5e5";
    public const string Neutral300 = "#d4d4d4";
    public const string Neutral400 = "#a3a3a3";
    public const string Neutral500 = "#737373";
    public const string Neutral600 = "#525252";
    public const string Neutral700 = "#404040";
    public const string Neutral800 = "#262626";
    public const string Neutral900 = "#171717";
    public const string Neutral950 = "#0a0a0a";
    public const string Stone50 = "#fafaf9";
    public const string Stone100 = "#f5f5f4";
    public const string Stone200 = "#e7e5e4";
    public const string Stone300 = "#d6d3d1";
    public const string Stone400 = "#a8a29e";
    public const string Stone500 = "#78716c";
    public const string Stone600 = "#57534e";
    public const string Stone700 = "#44403c";
    public const string Stone800 = "#292524";
    public const string Stone900 = "#1c1917";
    public const string Stone950 = "#0c0a09";
    public const string Red50 = "#fef2f2";
    public const string Red100 = "#fee2e2";
    public const string Red200 = "#fecaca";
    public const string Red300 = "#fca5a5";
    public const string Red400 = "#f87171";
    public const string Red500 = "#ef4444";
    public const string Red600 = "#dc2626";
    public const string Red700 = "#b91c1c";
    public const string Red800 = "#991b1b";
    public const string Red900 = "#7f1d1d";
    public const string Red950 = "#450a0a";
    public const string Orange50 = "#fff7ed";
    public const string Orange100 = "#ffedd5";
    public const string Orange200 = "#fed7aa";
    public const string Orange300 = "#fdba74";
    public const string Orange400 = "#fb923c";
    public const string Orange500 = "#f97316";
    public const string Orange600 = "#ea580c";
    public const string Orange700 = "#c2410c";
    public const string Orange800 = "#9a3412";
    public const string Orange900 = "#7c2d12";
    public const string Orange950 = "#431407";
    public const string Amber50 = "#fffbeb";
    public const string Amber100 = "#fef3c7";
    public const string Amber200 = "#fde68a";
    public const string Amber300 = "#fcd34d";
    public const string Amber400 = "#fbbf24";
    public const string Amber500 = "#f59e0b";
    public const string Amber600 = "#d97706";
    public const string Amber700 = "#b45309";
    public const string Amber800 = "#92400e";
    public const string Amber900 = "#78350f";
    public const string Amber950 = "#451a03";
    public const string Yellow50 = "#fefce8";
    public const string Yellow100 = "#fef9c3";
    public const string Yellow200 = "#fef08a";
    public const string Yellow300 = "#fde047";
    public const string Yellow400 = "#facc15";
    public const string Yellow500 = "#eab308";
    public const string Yellow600 = "#ca8a04";
    public const string Yellow700 = "#a16207";
    public const string Yellow800 = "#854d0e";
    public const string Yellow900 = "#713f12";
    public const string Yellow950 = "#422006";
    public const string Lime50 = "#f7fee7";
    public const string Lime100 = "#ecfccb";
    public const string Lime200 = "#d9f99d";
    public const string Lime300 = "#bef264";
    public const string Lime400 = "#a3e635";
    public const string Lime500 = "#84cc16";
    public const string Lime600 = "#65a30d";
    public const string Lime700 = "#4d7c0f";
    public const string Lime800 = "#3f6212";
    public const string Lime900 = "#365314";
    public const string Lime950 = "#1a2e05";
    public const string Green50 = "#f0fdf4";
    public const string Green100 = "#dcfce7";
    public const string Green200 = "#bbf7d0";
    public const string Green300 = "#86efac";
    public const string Green400 = "#4ade80";
    public const string Green500 = "#22c55e";
    public const string Green600 = "#16a34a";
    public const string Green700 = "#15803d";
    public const string Green800 = "#166534";
    public const string Green900 = "#14532d";
    public const string Green950 = "#052e16";
    public const string Emerald50 = "#ecfdf5";
    public const string Emerald100 = "#d1fae5";
    public const string Emerald200 = "#a7f3d0";
    public const string Emerald300 = "#6ee7b7";
    public const string Emerald400 = "#34d399";
    public const string Emerald500 = "#10b981";
    public const string Emerald600 = "#059669";
    public const string Emerald700 = "#047857";
    public const string Emerald800 = "#065f46";
    public const string Emerald900 = "#064e3b";
    public const string Emerald950 = "#022c22";
    public const string Teal50 = "#f0fdfa";
    public const string Teal100 = "#ccfbf1";
    public const string Teal200 = "#99f6e4";
    public const string Teal300 = "#5eead4";
    public const string Teal400 = "#2dd4bf";
    public const string Teal500 = "#14b8a6";
    public const string Teal600 = "#0d9488";
    public const string Teal700 = "#0f766e";
    public const string Teal800 = "#115e59";
    public const string Teal900 = "#134e4a";
    public const string Teal950 = "#042f2e";
    public const string Cyan50 = "#ecfeff";
    public const string Cyan100 = "#cffafe";
    public const string Cyan200 = "#a5f3fc";
    public const string Cyan300 = "#67e8f9";
    public const string Cyan400 = "#22d3ee";
    public const string Cyan500 = "#06b6d4";
    public const string Cyan600 = "#0891b2";
    public const string Cyan700 = "#0e7490";
    public const string Cyan800 = "#155e75";
    public const string Cyan900 = "#164e63";
    public const string Cyan950 = "#083344";
    public const string Sky50 = "#f0f9ff";
    public const string Sky100 = "#e0f2fe";
    public const string Sky200 = "#bae6fd";
    public const string Sky300 = "#7dd3fc";
    public const string Sky400 = "#38bdf8";
    public const string Sky500 = "#0ea5e9";
    public const string Sky600 = "#0284c7";
    public const string Sky700 = "#0369a1";
    public const string Sky800 = "#075985";
    public const string Sky900 = "#0c4a6e";
    public const string Sky950 = "#082f49";
    public const string Blue50 = "#eff6ff";
    public const string Blue100 = "#dbeafe";
    public const string Blue200 = "#bfdbfe";
    public const string Blue300 = "#93c5fd";
    public const string Blue400 = "#60a5fa";
    public const string Blue500 = "#3b82f6";
    public const string Blue600 = "#2563eb";
    public const string Blue700 = "#1d4ed8";
    public const string Blue800 = "#1e40af";
    public const string Blue900 = "#1e3a8a";
    public const string Blue950 = "#172554";
    public const string Indigo50 = "#eef2ff";
    public const string Indigo100 = "#e0e7ff";
    public const string Indigo200 = "#c7d2fe";
    public const string Indigo300 = "#a5b4fc";
    public const string Indigo400 = "#818cf8";
    public const string Indigo500 = "#6366f1";
    public const string Indigo600 = "#4f46e5";
    public const string Indigo700 = "#4338ca";
    public const string Indigo800 = "#3730a3";
    public const string Indigo900 = "#312e81";
    public const string Indigo950 = "#1e1b4b";
    public const string Violet50 = "#f5f3ff";
    public const string Violet100 = "#ede9fe";
    public const string Violet200 = "#ddd6fe";
    public const string Violet300 = "#c4b5fd";
    public const string Violet400 = "#a78bfa";
    public const string Violet500 = "#8b5cf6";
    public const string Violet600 = "#7c3aed";
    public const string Violet700 = "#6d28d9";
    public const string Violet800 = "#5b21b6";
    public const string Violet900 = "#4c1d95";
    public const string Violet950 = "#2e1065";
    public const string Purple50 = "#faf5ff";
    public const string Purple100 = "#f3e8ff";
    public const string Purple200 = "#e9d5ff";
    public const string Purple300 = "#d8b4fe";
    public const string Purple400 = "#c084fc";
    public const string Purple500 = "#a855f7";
    public const string Purple600 = "#9333ea";
    public const string Purple700 = "#7e22ce";
    public const string Purple800 = "#6b21a8";
    public const string Purple900 = "#581c87";
    public const string Purple950 = "#3b0764";
    public const string Fuchsia50 = "#fdf4ff";
    public const string Fuchsia100 = "#fae8ff";
    public const string Fuchsia200 = "#f5d0fe";
    public const string Fuchsia300 = "#f0abfc";
    public const string Fuchsia400 = "#e879f9";
    public const string Fuchsia500 = "#d946ef";
    public const string Fuchsia600 = "#c026d3";
    public const string Fuchsia700 = "#a21caf";
    public const string Fuchsia800 = "#86198f";
    public const string Fuchsia900 = "#701a75";
    public const string Fuchsia950 = "#4a044e";
    public const string Pink50 = "#fdf2f8";
    public const string Pink100 = "#fce7f3";
    public const string Pink200 = "#fbcfe8";
    public const string Pink300 = "#f9a8d4";
    public const string Pink400 = "#f472b6";
    public const string Pink500 = "#ec4899";
    public const string Pink600 = "#db2777";
    public const string Pink700 = "#be185d";
    public const string Pink800 = "#9d174d";
    public const string Pink900 = "#831843";
    public const string Pink950 = "#500724";
    public const string Rose50 = "#fff1f2";
    public const string Rose100 = "#ffe4e6";
    public const string Rose200 = "#fecdd3";
    public const string Rose300 = "#fda4af";
    public const string Rose400 = "#fb7185";
    public const string Rose500 = "#f43f5e";
    public const string Rose600 = "#e11d48";
    public const string Rose700 = "#be123c";
    public const string Rose800 = "#9f1239";
    public const string Rose900 = "#881337";
    public const string Rose950 = "#4c0519";
}
