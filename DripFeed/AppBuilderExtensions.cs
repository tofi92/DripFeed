using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DripFeed
{
    public static class AppBuilderExtensions
    {

        /// <summary>
        /// Use DripFeed to throttle requests. Please make sure you configured DripFeed in you ConfigureServices method (with <see cref="ServiceCollectionExtension.AddDripFeed(Microsoft.Extensions.DependencyInjection.IServiceCollection, Action{DripFeedOptions})"/>
        /// </summary>
        /// <param name="appBuilder"></param>
        public static void UseDripFeed(this IApplicationBuilder appBuilder)
        {
            appBuilder.UseMiddleware<DripFeedMiddleware>();
        }
    }
}
