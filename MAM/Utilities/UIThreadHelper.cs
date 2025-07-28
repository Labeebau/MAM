using Microsoft.UI.Dispatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAM.Utilities
{
    public static class UIThreadHelper
    {
        private static DispatcherQueue dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        public static void RunOnUIThread(Action action)
        {
            if (dispatcherQueue.HasThreadAccess)
                action();
            else
                dispatcherQueue.TryEnqueue(() => action());
        }

        public static async Task RunOnUIThreadAsync(Func<Task> asyncAction)
        {
            var tcs = new TaskCompletionSource();

            dispatcherQueue.TryEnqueue(async () =>
            {
                try
                {
                    await asyncAction();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            await tcs.Task;
        }

        public static async Task RunOnUIThreadAsync(Action action)
        {
            var tcs = new TaskCompletionSource();

            dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    action();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            await tcs.Task;
        }
        
        public static Task<T> RunOnUIThreadAsync<T>(Func<Task<T>> func)
        {
            var tcs = new TaskCompletionSource<T>();

            // Use your main window's DispatcherQueue directly
            var dispatcher = App.MainAppWindow?.DispatcherQueue;

            if (dispatcher == null)
            {
                tcs.SetException(new InvalidOperationException("DispatcherQueue is not available."));
                return tcs.Task;
            }

            dispatcher.TryEnqueue(async () =>
            {
                try
                {
                    var result = await func();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }
    }

}


