using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace ClusterServer
{
    public static class HttpListenerExtensions
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HttpListenerExtensions));

        public static async Task StartProcessingRequestsAsync(this HttpListener listener,
            Func<HttpListenerContext, Task> callbackAsync)
        {
            listener.Start();
            var taskCancellationTokens = new ConcurrentDictionary<string, CancellationTokenSource>();

            while (true)
                try
                {
                    var context = await listener.GetContextAsync();
                    var query = context.Request.QueryString["query"];
                    if (context.Request.Headers["abort"] == "True")
                        taskCancellationTokens[query].Cancel();
                    else
                    {
                        var cancellationTokenSource = new CancellationTokenSource();
                        if (!taskCancellationTokens.ContainsKey(query))
                            taskCancellationTokens.TryAdd(query, cancellationTokenSource);
                        else
                            taskCancellationTokens[query] = cancellationTokenSource;
                        Task.Run(async () =>
                            {
                                try
                                {
                                    await callbackAsync(context);
                                }
                                catch (Exception e)
                                {
                                    Log.Error(e);
                                }
                                finally
                                {
                                    context.Response.Close();
                                }
                            }, cancellationTokenSource.Token
                        );
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
        }

        public static void StartProcessingRequestsSync(this HttpListener listener,
            Action<HttpListenerContext> callbackSync)
        {
            listener.Start();
            var taskQueue = new LinkedList<HttpListenerContext>();
            StartTaskRecieverSync(listener, taskQueue);
            StartTaskHandlerSync(callbackSync, taskQueue);
        }

        private static void StartTaskHandlerSync(Action<HttpListenerContext> callbackSync,
            LinkedList<HttpListenerContext> taskQueue)
        {
            while (true)
            {
                HttpListenerContext context;
                lock (taskQueue)
                {
                    if (taskQueue.Count == 0)
                        Monitor.Wait(taskQueue);
                    context = taskQueue.Dequeue();
                }

                try
                {
                    callbackSync(context);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
                finally
                {
                    context.Response.Close();
                }
            }
        }

        private static void StartTaskRecieverSync(HttpListener listener, LinkedList<HttpListenerContext> taskQueue)
        {
            Task.Run(() =>
            {
                while (true)
                    try
                    {
                        var context = listener.GetContext();
                        lock (taskQueue)
                        {
                            if (context.Request.Headers["abort"] == "True")
                            {
                                taskQueue.RemoveAll(x =>
                                    x.Request.QueryString["query"] == context.Request.QueryString["query"]);
                                //context.SendResponse($"{context.Request.QueryString["query"]} is aborted!");
                            }
                            else
                            {
                                taskQueue.AddLast(context);
                                if (taskQueue.Count == 1)
                                    Monitor.Pulse(taskQueue);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
            });
        }
    }
}