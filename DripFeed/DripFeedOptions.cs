using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DripFeed
{
    /// <summary>
    /// The DripFeed config
    /// </summary>
    public class DripFeedOptions
    {
        internal bool useMemoryCache = false;
        internal bool useDistributedCache = false;
        internal int StatusCode = 429;

        //holds all action names per controller
        internal Dictionary<string, IEnumerable<string>> throttledActionsMap = new Dictionary<string, IEnumerable<string>>();

        //holds the actual throttable action data with the key format controllername/actionname for faster searching in the middleware
        internal Dictionary<string, ThrottableAction> throttledActions = new Dictionary<string, ThrottableAction>();

        internal Func<HttpContext, string> customIdentifierAction = null;


        /// <summary>
        /// Use IMemoryCache to control the throttling
        /// </summary>
        public void UseMemoryCache()
        {
            this.useMemoryCache = true;
            this.useDistributedCache = false;
        }


        /// <summary>
        /// Use IDistributedCache to control the throttling (this is recommended in production use-cases)
        /// </summary>
        /// <param name="distributedCache"></param>
        public void UseDistributedCache(IDistributedCache distributedCache)
        {
            this.useMemoryCache = false;
            this.useDistributedCache = true;
        }


        /// <summary>
        /// You can specify a custom identifier to use for throttling. Defaults to the remote IP address. If your app is behind a reverse proxy you need to use a custom identifier!
        /// </summary>
        /// <param name="customIdentifierAction">Use the HttpContext to create your custom identifier here</param>
        public void UseCustomIdentifier(Func<HttpContext, string> customIdentifierAction)
        {
            this.customIdentifierAction = customIdentifierAction;
        }
        

        /// <summary>
        /// Adds a controller to your endpoints to throttle. Note that you need to configure enpoints here too (with <see cref="ControllerThrottleBuilder{TController}.AddAction(Expression{Func{TController, Func{object}}})"/>
        /// </summary>
        /// <typeparam name="TController"></typeparam>
        /// <param name="buildAction"></param>
        public void AddController<TController>(Action<ControllerThrottleBuilder<TController>> buildAction) where TController: ControllerBase
        {
            ControllerThrottleBuilder<TController> builder = new ControllerThrottleBuilder<TController>();
            buildAction(builder);

            string controllerName = typeof(TController).Name.ToLower();

            if (throttledActionsMap.ContainsKey(controllerName))
            {
                throttledActionsMap[controllerName] = builder.ThrottableActions.Select(s => s.Key);
            }
            else
            {
                throttledActionsMap.Add(controllerName, builder.ThrottableActions.Select(s => s.Key));
            }

            

            foreach (var action in throttledActionsMap[controllerName])
            {
                string key = GetKeyStringForThrottledActions(controllerName, action);
                if (!throttledActions.ContainsKey(key))
                {
                    throttledActions.Add(key, null);
                }

                throttledActions[key] = builder.ThrottableActions[action];
            }
        }

        internal static string GetKeyStringForThrottledActions(string controller, string action)
        {
            return $"{controller}/{action}";
        }


        /// <summary>
        /// Use a custom status code to serve to your users when they are throttled. Defaults to 429 (Too Many Requests)
        /// </summary>
        /// <param name="statusCode"></param>
        public void UseStatusCode(int statusCode)
        {
            this.StatusCode = statusCode;
        }


        /// <summary>
        /// Throttle all actions of this controller with the same options. You can use this in conjunction with <see cref="AddController{TController}(Action{ControllerThrottleBuilder{TController}})"/> to use other options for some actions of the same controller but use these options for all other actions
        /// </summary>
        /// <typeparam name="TController"></typeparam>
        /// <param name="timeSpan">The time span for you throttling</param>
        /// <param name="amountOfRequests">The amout of requests the user can make in the configured time span</param>
        public void AddAllActionsFromController<TController>(TimeSpan timeSpan, int amountOfRequests) where TController: ControllerBase
        {
            List<string> allActions = new List<string>();
            allActions.Add("*");

            var controllerName = typeof(TController).Name.ToLower();
            if (throttledActionsMap.ContainsKey(controllerName))
            {
                throttledActionsMap[controllerName] = allActions;
            }
            else
            {
                throttledActionsMap.Add(controllerName, allActions);
            }

            var toDelete = throttledActions.Where(a => a.Key.StartsWith(controllerName)).Select(d => d.Key);

            foreach(var key in toDelete)
            {
                throttledActions.Remove(key);
            }

            throttledActions.Add(GetKeyStringForThrottledActions(controllerName, "*"), new ThrottableAction() { ThrottleTimeSpan = timeSpan, AmountPerTimeSpan = amountOfRequests });
        }

    }


}
