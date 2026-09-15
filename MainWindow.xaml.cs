using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using System;
using System.Threading;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace App4
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        private WeakReference? refs;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var thread = new Thread(() =>
            {
                var controller = DispatcherQueueController.CreateOnCurrentThread();
                controller.DispatcherQueue.EnsureSystemDispatcherQueue();
                controller.DispatcherQueue.TryEnqueue(() =>
                {
                    var window = new Window();

                    var content = new BlankPage();

                    window.Content = content;

                    refs = new WeakReference(content);

                    window.Activate();

                    window.Closed += (_, _) =>
                    {
                        // Uncomment this to fix leak
                        /*controller.DispatcherQueue.ShutdownStarting += (sender, args) =>
                        {
                            var deferral = args.GetDeferral();
                            var finalizer = new Thread(() =>
                            {
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                                GC.Collect();
                                deferral.Complete();
                            });
                            finalizer.Start();
                        };*/

                        controller.DispatcherQueue.EnqueueEventLoopExit();
                    };

                });

                var manager = WindowsXamlManager.InitializeForCurrentThread();

                SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(controller.DispatcherQueue));
                controller.DispatcherQueue.RunEventLoop();

                manager.Dispose();

                controller.ShutdownQueue();
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            var bg = new Thread(() =>
            {
                while (true)
                {
                    if (DispatcherQueue == null)
                        return;
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        State.Text = $"Alive: {refs?.IsAlive}";
                    });
                    Thread.Sleep(1000);
                }
            })
            {
                IsBackground = true
            };
            bg.Start();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            new Thread(() =>
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }).Start();
        }

    }
}
