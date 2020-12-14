//using Blazorise;
//using Blazorise.AntDesign;
//using Blazorise.Icons.FontAwesome;
//using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Net.Http;
//using System.Text;
//using System.Threading.Tasks;

//namespace ui_components.Client
//{
//    public class Program
//    {
//        public static async Task Main(string[] args)
//        {
//            var builder = WebAssemblyHostBuilder.CreateDefault(args);

//            builder.Services.AddBlazorise(
//                options =>
//                {
//                    options.ChangeTextOnKeyPress = true;
//                })
//                .AddAntDesignProviders()
//                .AddFontAwesomeIcons();

//            builder.Services.AddSingleton(new HttpClient
//            {
//                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
//            });

//            var host = builder.Build();

//            host.Services
//              .UseAntDesignProviders()
//              .UseFontAwesomeIcons();

//            await host.RunAsync();
//        }
//    }
//}
