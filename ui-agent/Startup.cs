using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ui_agent.Services;

namespace ui_agent
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddTokenGetters()
                .AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Items}/{action=Welcome}/{id?}");
                endpoints.MapControllerRoute(
                    name: "mapping",
                    pattern: "{controller=Data}/{action=Mapping}/{id?}");
                endpoints.MapControllerRoute(
                    name: "items",
                    pattern: "{controller=Items}/{action=EbayListings}/{id?}");
            });

            if (HybridSupport.IsElectronActive)
            {
                ElectronBootstrap();
            }
        }

        public async void ElectronBootstrap()
        {
            var browserWindow = await Electron.WindowManager.CreateWindowAsync(new BrowserWindowOptions
            {
                
            });

           // Electron.App.CommandLine.AppendSwitch("ignore-certificate-errors");

           // Electron.HostHook.Call

            await browserWindow.WebContents.Session.ClearCacheAsync();

            browserWindow.OnReadyToShow += () => browserWindow.Show();
            browserWindow.SetTitle("TreeCat");

            SetupMenus();
        }


        private void SetupMenus()
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
                            Label = "App Menu Demo",
                            Click = async () => {
                                var options = new MessageBoxOptions("This demo is for the Menu section, showing how to create a clickable menu item in the application menu.");
                                options.Type = MessageBoxType.info;
                                options.Title = "Application Menu Demo";
                                await Electron.Dialog.ShowMessageBoxAsync(options);
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
