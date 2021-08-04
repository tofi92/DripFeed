using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DripFeed
{
    public class ActionThrottleBuilder
    {
        private readonly ThrottableAction throttableAction;


        internal ActionThrottleBuilder(ThrottableAction throttableAction)
        {
            this.throttableAction = throttableAction;

        }

        /// <summary>
        /// Throttle this action
        /// </summary>
        /// <param name="timeSpan">The time span for you throttling</param>
        /// <param name="amountOfRequests">The amout of requests the user can make in the configured time span</param>
        /// <returns></returns>
        public ActionThrottleBuilder WithLimit(TimeSpan timeSpan, int amountOfRequests)
        {
            throttableAction.ThrottleTimeSpan = timeSpan;
            throttableAction.AmountPerTimeSpan = amountOfRequests;
            return this;
        }

        internal ThrottableAction Build()
        {
            return this.throttableAction;
        }
    }
}
