using Microsoft.Extensions.DependencyInjection;
using System;

namespace DripFeed
{
    public static class ServiceCollectionExtension
    {

        /// <summary>
        /// Add the required options for DripFeed to your DI container. Don't forget to use app.UseDripFeed(); in your Configure method!
        /// </summary>
        /// <param name="serviceCollection">Your DI container</param>
        /// <param name="options">You can modifiy DripFeed with these options. Please notice that you MUST use either IMemoryCache or IDistributedCache to use DripFeed!</param>
        public static void AddDripFeed(this IServiceCollection serviceCollection, Action<DripFeedOptions> options)
        {
            var opt = new DripFeedOptions();
            options.Invoke(opt);

            if (!opt.useMemoryCache && opt.useDistributedCache)
            {
                throw new DripFeedException("You must use either IMemoryCache or IDistributedCache!");
            }

            serviceCollection.AddSingleton(opt);
        }
    }
}
