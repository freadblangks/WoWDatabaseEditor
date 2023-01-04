using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Prism.Events;
using Prism.Ioc;
using RenderingTester;
using TheEngine;
using Unity;
using WDE.AzerothCore;
using WDE.Common.DBC;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DbcStore;
using WDE.DbcStore.FastReader;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MapSpawns;
using WDE.Module;
using WDE.MPQ;
using WDE.Parameters;
using WDE.SqlInterpreter;
using WDE.Trinity;
using WDE.TrinityMySqlDatabase;

namespace AvaloniaRenderingTester
{
    public partial class MainWindow : Avalonia.Controls.Window
    {
        public IGame Game { get; }
        public MainWindow()
        {
            var container = new UnityContainer();
            container.AddExtension(new Diagnostic());
            var scopedContainer = new ScopedContainer(new UnityContainerExtension(container), new UnityContainerRegistry(container), container);
            var registry = new UnityContainerRegistry(container);
            var provider = new UnityContainerProvider(container);

            registry.RegisterInstance<IScopedContainer>(scopedContainer);
            registry.RegisterInstance<IContainerProvider>(provider);
            registry.RegisterInstance<IContainerRegistry>(registry);

            registry.Register<IGameView, DummyGameView>();
            registry.Register<IStatusBar, DummyStatusBar>();
            registry.Register<IGameProperties, DummyGameProperties>();
            registry.Register<IMessageBoxService, DummyMessageBox>();
            registry.Register<IDatabaseClientFileOpener, DatabaseClientFileOpener>();
            registry.Register<ITableEditorPickerService, DummyTableEditorPickerService>();
            registry.Register<IQueryEvaluator, DummyQueryEvaluator>();
            var context = new SingleThreadSynchronizationContext(Thread.CurrentThread.ManagedThreadId);
            var mainThread = new MainThread(context);
            registry.RegisterInstance<IMainThread>(mainThread);
            registry.RegisterInstance<IEventAggregator>(new EventAggregator());

            registry.RegisterInstance<IClipboardService>(new AvaloniaClipboard());
            
            SetupModules(new DbcStoreModule(),
                new MpqModule(),
                new WoWDatabaseEditorCore.MainModule(),
                new TrinityMySqlDatabaseModule(),
                new ParametersModule(),
                new TrinityModule(),
                new AzerothModule(),
                new MapSpawnsModule());

            void SetupModules(params ModuleBase[] modules)
            {
                foreach (var module in modules)
                {
                    module.InitializeCore("unspecified");
                    module.RegisterTypes(registry);
                }
            }
            
            Game = provider.Resolve<Game>();
            
            DataContext = this;
            AvaloniaXamlLoader.Load(this);
        }
    }

    public class AvaloniaClipboard : IClipboardService
    {
        private IClipboard clipboard;

        public AvaloniaClipboard()
        {
            clipboard = AvaloniaLocator.Current!.GetService<IClipboard>() ?? throw new Exception("no clipboard");
        }
        
        public Task<string> GetText()
        {
            return clipboard.GetTextAsync();
        }

        public void SetText(string text)
        {
            clipboard.SetTextAsync(text).ListenErrors();
        }
    }
}

