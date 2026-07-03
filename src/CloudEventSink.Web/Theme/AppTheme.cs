using MudBlazor;

namespace CloudEventSink.Web.Theme;

public static class AppTheme
{
    public static MudTheme Instance { get; } = Build();

    private static MudTheme Build()
    {
        return new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#2563EB",
                Secondary = "#7C3AED",
                Tertiary = "#0EA5E9",
                AppbarBackground = "#FFFFFF",
                AppbarText = "#111827",
                Background = "#F9FAFB",
                Surface = "#FFFFFF",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#374151",
                DrawerIcon = "#6B7280",
                TextPrimary = "#111827",
                TextSecondary = "#6B7280",
                ActionDefault = "#6B7280",
                LinesDefault = "#E5E7EB",
                LinesInputs = "#D1D5DB",
                TableLines = "#EEF0F2",
                TableStriped = "#FAFAFA",
                Divider = "#E5E7EB",
                Success = "#16A34A",
                Warning = "#D97706",
                Error = "#DC2626",
                Info = "#2563EB",
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "10px",
                AppbarHeight = "64px",
                DrawerWidthLeft = "240px",
            },
            Typography = new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily =
                    [
                        "Inter",
                        "-apple-system",
                        "BlinkMacSystemFont",
                        "Segoe UI",
                        "Roboto",
                        "Helvetica Neue",
                        "Arial",
                        "sans-serif",
                    ],
                },
            },
        };
    }
}
