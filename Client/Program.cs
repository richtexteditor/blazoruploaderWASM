using BlazorUploaderDemoWasm.Client;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace BlazorUploaderDemoWasm.Client
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebAssemblyHostBuilder.CreateDefault(args);
			builder.RootComponents.Add<App>("#app");
			builder.RootComponents.Add<HeadOutlet>("head::after");

			builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

			// BlazorUploader.demo.lic is compiled into EmbededResource in BlazorUploaderDemoWasm.Client.dll
			using var licstream = typeof(Program).Assembly.GetManifestResourceStream("BlazorUploaderDemoWasm.Client.BlazorUploader.demo.lic");
			BlazorUploader.CoreUploader.InstallLicense(licstream);

			//max chunk size
			BlazorUploader.CoreUploader.MaxBufferSize = 512 * 1024;

			//Or install license like this way:
			//byte[] filedata = File.ReadAllBytes("PathToLicenseFolder/BlazorUploader.lic");
			//BlazorUploader.CoreUploader.InstallLicense(filedata);

			await builder.Build().RunAsync();
		}
	}
}
