using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HGV.Tarrasque.Collection.Models
{
    public class DateRange
    {
        public DateTimeOffset Min { get; set; }
        public DateTimeOffset Max { get; set; }

        public TimeSpan Delta { get { return this.Max - this.Min; } }

        public void SetRange(long min, long max)
        {
            this.Min = DateTimeOffset.FromUnixTimeSeconds(min);
            this.Max = DateTimeOffset.FromUnixTimeSeconds(max);
        }

        public void ToLocal()
        {
            this.Min = this.Min.ToLocalTime();
            this.Max = this.Max.ToLocalTime();
        }

        public void ToUniversal()
        {
            this.Min = this.Min.ToUniversalTime();
            this.Max = this.Max.ToUniversalTime();
        }
    }

    public class Checkpoint
    {
        public DateRange Timestamp { get; private set; }
        public IEnumerable<long> History { get; private set; }

        public long Latest { get; set; }
        public int TotalMatches { get; set; }
        public int TotalADMatches { get; set; }

        public Checkpoint()
        {
            this.TotalADMatches = 0;
            this.TotalMatches = 0;
            this.Timestamp = new DateRange();
            this.History = new List<long>();
        }

        public void AddHistory(long item)
        {
            var temp = new List<long> { item };
            this.History = this.History.Concat(temp);

            var offset = this.History.Count() - 100;
            offset = offset < 0 ? 0 : offset;
            this.History = this.History.Skip(offset);
        }
    }
}
