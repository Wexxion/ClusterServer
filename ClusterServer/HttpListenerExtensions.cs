using System;
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

        public static void StartProcessingRequestsAsync(this HttpListener listener, Func<HttpListenerContext, Task> callbackAsync)
        {
            listener.Start();

            while (true)
            {
                try
                {
                    var context = listener.GetContext();

                    Task.Run(
                        async () =>
                        {
                            var ctx = context;
                            try
                            {
                                await callbackAsync(ctx);
                            }
                            catch (Exception e)
                            {
                                Log.Error(e);
                            }
                            finally
                            {
                                ctx.Response.Close();
                            }
                        }
                    );
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        public static void StartProcessingRequestsSync(this HttpListener listener, Action<HttpListenerContext> callbackSync)
        {
            listener.Start();
            var taskQueue = new LinkedList<HttpListenerContext>();
            StartTaskReciever(listener, taskQueue);
            StartTaskHandler(callbackSync, taskQueue);
        }

        private static void StartTaskHandler(Action<HttpListenerContext> callbackSync, LinkedList<HttpListenerContext> taskQueue)
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

        private static void StartTaskReciever(HttpListener listener, LinkedList<HttpListenerContext> taskQueue)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        var context = await listener.GetContextAsync();
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
                }
            });
        }
    }
}