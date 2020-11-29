using ElectronNET.API;
using ElectronNET.API.Entities;
using lib;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using ui_agent.Services;

namespace ui_agent
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public ILogger<Startup> Logger { get; }

        public Startup(ILogger<Startup> logger, IConfiguration configuration)
        {
            this.Configuration = configuration;
            this.Logger = logger;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddTokenGetters()
                .AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger, IConfiguration configuration, ITokenGetters tokenGetters)
        {
            app.UseExceptionHandler("/item/error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Item}/{action=Welcome}/{id?}");
            });

            var bifrostURL = configuration["Bifrost:Service"];
            logger.LogInformation($"will use the following bifrost service: {bifrostURL}");

            if (HybridSupport.IsElectronActive)
            {
                ElectronBootstrap(logger, tokenGetters);
            }
        }

        public async void ElectronBootstrap(ILogger logger, ITokenGetters tokenGetters)
        {
            var browserWindow = await Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions
            {

            });

            await browserWindow.WebContents.Session.ClearCacheAsync();

            browserWindow.OnReadyToShow += () => browserWindow.Show();
            browserWindow.SetTitle("TreeCat");

            logger.LogInformation("window title set");

            browserWindow.WebContents.OnDidFinishLoad += async () =>
            {
                browserWindow.WebContents.Session.SetUserAgent("Chrome");

                var url = await browserWindow.WebContents.GetUrl();
                logger.LogDebug($"navigated to {url}");

                var cleanUrl = new Uri(url);
                if (cleanUrl.Host == "poshmark.com" && cleanUrl.AbsolutePath == "/feed")
                {
                    logger.LogInformation("user logged into poshmark");

                    var cookies = await browserWindow.WebContents.Session.Cookies.GetAsync(new CookieFilter());
                    var jwtCookie = cookies.FirstOrDefault(c => c.Name == "jwt");

                    if (jwtCookie != null)
                    {
                        browserWindow.LoadURL($"https://127.0.0.1:19872/auth/poshmarkaccept?cookie={jwtCookie.Value}");
                    }
                    else
                    {
                        logger.LogError("failed to read jwt cookie");
                    }
                }
            };

            SetupMenus(tokenGetters);
        }


        private void SetupMenus(ITokenGetters tokenGetters)
        {
            if (HybridSupport.IsElectronActive)
            {
                var menu = new MenuItem[] {
                    new MenuItem { Label = "Edit", Type = MenuType.submenu, Submenu = new MenuItem[] {
                        new MenuItem { Label = "Undo", Accelerator = "CmdOrCtrl+Z", Role = MenuRole.undo },
                        new MenuItem { Label = "Redo", Accelerator = "Shift+CmdOrCtrl+Z", Role = MenuRole.redo },
                        new MenuItem { Type = MenuType.separator },
                        new MenuItem { Label = "Cut", Accelerator = "CmdOrCtrl+X", Role = MenuRole.cut },
                        new MenuItem { Label = "Copy", Accelerator = "CmdOrCtrl+C", Role = MenuRole.copy },
                        new MenuItem { Label = "Paste", Accelerator = "CmdOrCtrl+V", Role = MenuRole.paste },
                        new MenuItem { Label = "Select All", Accelerator = "CmdOrCtrl+A", Role = MenuRole.selectall }
                    }
                    },
                    new MenuItem { Label = "View", Type = MenuType.submenu, Submenu = new MenuItem[] {
                        new MenuItem
                        {
                            Label = "Reload",
                            Accelerator = "CmdOrCtrl+R",
                            Click = () =>
                            {
                                // on reload, start fresh and close any old
                                // open secondary windows
                                var mainWindowId = Electron.WindowManager.BrowserWindows.ToList().First().Id;
                                Electron.WindowManager.BrowserWindows.ToList().ForEach(browserWindow => {
                                    if(browserWindow.Id != mainWindowId)
                                    {
                                        browserWindow.Close();
                                    }
                                    else
                                    {
                                        browserWindow.Reload();
                                    }
                                });
                            }
                        },
                        new MenuItem
                        {
                            Label = "Toggle Full Screen",
                            Accelerator = "CmdOrCtrl+F",
                            Click = async () =>
                            {
                                bool isFullScreen = await Electron.WindowManager.BrowserWindows.First().IsFullScreenAsync();
                                Electron.WindowManager.BrowserWindows.First().SetFullScreen(!isFullScreen);
                            }
                        },
                        new MenuItem
                        {
                            Label = "Open Developer Tools",
                            Accelerator = "CmdOrCtrl+I",
                            Click = () => Electron.WindowManager.BrowserWindows.First().WebContents.OpenDevTools()
                        },
                        new MenuItem
                        {
                            Type = MenuType.separator
                        },
                        new MenuItem
                        {
                            Label = "Clear All Data",
                            Click = async () => {
                                var options = new MessageBoxOptions("Are you sure you want to clear all local data? This includes authentication cookies and tokens, but all your items are safe in the cloud.");
                                options.Type = MessageBoxType.question;
                                options.Title = "Please Confirm";
                                options.Buttons = new string[] {"Yes, delete all data", "Cancel"};
                                var result = await Electron.Dialog.ShowMessageBoxAsync(options);

                                if (result.Response == 0)
                                {
                                    tokenGetters.ClearAllData();
                                    var mainWindowId = Electron.WindowManager.BrowserWindows.ToList().First().Id;
                                    Electron.WindowManager.BrowserWindows.ToList().ForEach(async browserWindow => {
                                        await browserWindow.WebContents.Session.ClearStorageDataAsync();

                                        if(browserWindow.Id != mainWindowId)
                                        {
                                            browserWindow.Close();
                                        }
                                        else
                                        {
                                            browserWindow.Reload();
                                        }
                                    });
                                }

                                Electron.WindowManager.BrowserWindows.First().WebContents.LoadURLAsync("https://127.0.0.1:19872/item/welcome");
                            }
                        }
                    }
                    },
                    new MenuItem { Label = "Window", Role = MenuRole.window, Type = MenuType.submenu, Submenu = new MenuItem[] {
                         new MenuItem { Label = "Minimize", Accelerator = "CmdOrCtrl+M", Role = MenuRole.minimize },
                         new MenuItem { Label = "Close", Accelerator = "CmdOrCtrl+W", Role = MenuRole.close }
                    }
                    },
                    new MenuItem { Label = "Help", Role = MenuRole.help, Type = MenuType.submenu, Submenu = new MenuItem[] {
                        new MenuItem
                        {
                            Label = "Learn More",
                            Click = async () => await Electron.Shell.OpenExternalAsync("https://github.com/ElectronNET")
                        }
                    }
                    }
                };

                Electron.Menu.SetApplicationMenu(menu);

                CreateContextMenu();
            }
        }
        private void CreateContextMenu()
        {
            var menu = new MenuItem[]
            {
                new MenuItem
                {
                    Label = "Hello",
                    Click = async () => await Electron.Dialog.ShowMessageBoxAsync("Electron.NET rocks!")
                },
                new MenuItem { Type = MenuType.separator },
                new MenuItem { Label = "Electron.NET", Type = MenuType.checkbox, Checked = true }
            };

            Electron.App.BrowserWindowFocus += () =>
            {
                var mainWindow = Electron.WindowManager.BrowserWindows.FirstOrDefault();
                Electron.Menu.SetContextMenu(mainWindow, menu);
            };

            Electron.IpcMain.On("show-context-menu", (args) =>
            {
                var mainWindow = Electron.WindowManager.BrowserWindows.FirstOrDefault();
                Electron.Menu.ContextMenuPopup(mainWindow);
            });
        }
    }
}
