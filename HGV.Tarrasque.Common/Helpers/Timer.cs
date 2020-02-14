using System;
using System.Collections.Generic;
using System.Text;
using Humanizer;
using Humanizer.Localisation;
using Microsoft.Extensions.Logging;

namespace HGV.Tarrasque.Common.Helpers
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "Using Pattern")]
    public class Timer : IDisposable
    {
        private readonly ILogger log;
        private readonly DateTime start;
        private readonly string name;

        public Timer(string name, ILogger logger)
        {
            this.name = name;
            this.log = logger;
            this.start = DateTime.Now;
        }

        public void Dispose()
        {
            var delta = (DateTime.Now - this.start).Humanize(maxUnit: TimeUnit.Minute, minUnit: TimeUnit.Second);
            this.log.LogWarning($"Timer[{this.name}] took {delta}");
        }
    }
}
