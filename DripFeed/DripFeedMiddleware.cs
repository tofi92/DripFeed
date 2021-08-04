using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DripFeed
{
    public class DripFeedMiddleware
    {
        private readonly RequestDelegate next;
        private readonly DripFeedOptions options;

        public DripFeedMiddleware(RequestDelegate next, DripFeedOptions options)
        {
            this.next = next;
            this.options = options;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var controllerActionDescriptor = context.GetEndpoint().Metadata.GetMetadata<ControllerActionDescriptor>();

            var controllerName = controllerActionDescriptor.ControllerName.ToLower() + "controller";
            var actionName = controllerActionDescriptor.ActionName.ToLower();

            if (options.throttledActionsMap.ContainsKey(controllerName))
            {
                ThrottableAction throttableAction = null;


                //Check for wildcard first, so that a specific endpoint can use different options
                if (options.throttledActionsMap[controllerName].Contains("*"))
                {
                    throttableAction = options.throttledActions[DripFeedOptions.GetKeyStringForThrottledActions(controllerName, "*")];
                }


                if (options.throttledActionsMap[controllerName].Contains(actionName))
                {
                    throttableAction = options.throttledActions[DripFeedOptions.GetKeyStringForThrottledActions(controllerName, actionName)];
                }

                //No actions registered -> skip throttle logic
                if (throttableAction != null)
                {

                    string identifier = "";
                    if (options.customIdentifierAction != null)
                    {
                        identifier = options.customIdentifierAction(context);
                    }

                    //fallback, if no custom identifier is configured
                    if (string.IsNullOrEmpty(identifier))
                    {
                        identifier = context.Connection.RemoteIpAddress.ToString();
                    }


                    if (options.useMemoryCache)
                    {
                        await RunWithMemoryCache(context, identifier, throttableAction);
                    }
                    else if (options.useDistributedCache)
                    {
                        await RunWithDistributedCache(context, identifier, throttableAction);
                    }
                }


            }

            await next(context);
        }

        private async Task RunWithMemoryCache(HttpContext context, string identifier, ThrottableAction throttableAction)
        {

            var cache = context.RequestServices.GetRequiredService<IMemoryCache>();

            
            int currentAmount = cache.GetOrCreate(GetAmountKey(identifier), entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = throttableAction.ThrottleTimeSpan * 2; //Can expire if we do not renew
                return 0;
            });

            DateTime lastHit = cache.GetOrCreate(GetLastHitKey(identifier), entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = throttableAction.ThrottleTimeSpan * 2; //Can expire if we do not renew
                return DateTime.Now;
            });


            //if the last hit is not as long ago as the throttle timespan and the amount of hits is greater than the max amount we short circuit the request and exit with the configured statuscode
            if (DateTime.Now - lastHit < throttableAction.ThrottleTimeSpan && currentAmount >= throttableAction.AmountPerTimeSpan)
            {
                await Throttle(context, options, throttableAction, lastHit);
            }
            else
            {
                if (currentAmount > throttableAction.AmountPerTimeSpan)
                {
                    //we throttled the last request, so we reset the amount
                    cache.Set(GetAmountKey(identifier), 1);
                }
                else
                {
                    //we didn't throttle yet, so increment the amount
                    cache.Set(GetAmountKey(identifier), currentAmount + 1);
                }


                cache.Set(GetLastHitKey(identifier), DateTime.Now);
            }
        }



        private async Task RunWithDistributedCache(HttpContext context, string identifier, ThrottableAction throttableAction)
        {
            

            var cache = context.RequestServices.GetRequiredService<IDistributedCache>();

            var amountString = await cache.GetStringAsync(GetAmountKey(identifier));
            int amount = 0;
            if (!string.IsNullOrEmpty(amountString))
            {
                amount = int.Parse(amountString);
            }

            var lastHitString = await cache.GetStringAsync(GetLastHitKey(identifier));
            DateTime lastHit = DateTime.Now;
            if (!string.IsNullOrEmpty(lastHitString))
            {
                lastHit = DateTime.Parse(lastHitString);
            }


            if (DateTime.Now - lastHit < throttableAction.ThrottleTimeSpan && amount >= throttableAction.AmountPerTimeSpan)
            {
                await Throttle(context, options, throttableAction, lastHit);
            }
            else
            {
                if (amount > throttableAction.AmountPerTimeSpan)
                {
                    await cache.SetStringAsync(GetAmountKey(identifier), "1");
                }
                else
                {
                    await cache.SetStringAsync(GetAmountKey(identifier), (amount + 1).ToString());
                }
                await cache.SetStringAsync(GetLastHitKey(identifier), DateTime.Now.ToString("U"));
            }
        }

        /// <summary>
        /// Throttles the requests by short circuiting the request pipeline and serving an empty document with a 429 (or custom) status code and a Retry-After header
        /// </summary>
        /// <param name="context"></param>
        /// <param name="identifier"></param>
        /// <param name="throttableAction"></param>
        /// <returns></returns>
        private async Task Throttle(HttpContext context, DripFeedOptions options, ThrottableAction throttableAction, DateTime lastHit)
        {
            var retryAfterSeconds = (throttableAction.ThrottleTimeSpan - (DateTime.Now - lastHit)).TotalSeconds;
            context.Response.Headers.Add("Retry-After", ((int)retryAfterSeconds).ToString());
            context.Response.StatusCode = options.StatusCode;
            await context.Response.WriteAsync("");
        }

        private static string GetAmountKey(string identifier)
        {
            return $"dripfeed:{identifier}:amount";
        }

        private static string GetLastHitKey(string identifier)
        {
            return $"dripfeed:{identifier}:lasthit";
        }
    }
}
