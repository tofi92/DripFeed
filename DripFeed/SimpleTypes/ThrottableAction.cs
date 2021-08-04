using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DripFeed
{
    internal class ThrottableAction
    {
        public TimeSpan ThrottleTimeSpan { get; set; }
        public int AmountPerTimeSpan { get; set; }

        public string ActionName { get; set; }
    }
}
