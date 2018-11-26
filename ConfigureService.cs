using Topshelf;

namespace TsConverter
{
	internal static class ConfigureService
	{
		internal static void Configure()
		{
			HostFactory.Run(configure =>
			{
				configure.Service<Converter>(converter =>
				{
					converter.ConstructUsing(s => new Converter());
					converter.WhenStarted(s => s.Start());
					converter.WhenStopped(s => s.Stop());
				});
				//Setup Account that window service use to run.  
				configure.RunAsLocalSystem();
				configure.SetServiceName("TsConverter");
				configure.SetDisplayName("TS Converter Service");
				configure.SetDescription("Converts TS files to MP4.");
				configure.StartAutomatically();
			});
		}
	}
}
