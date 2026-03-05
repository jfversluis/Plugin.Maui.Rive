using Plugin.Maui.Rive;
#if !WINDOWS
using IconFont.Maui.FluentIcons;
#endif

namespace Plugin.Maui.Rive.Sample;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseRive()
#if !WINDOWS
			.UseFluentIcons()
#endif
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		return builder.Build();
	}
}