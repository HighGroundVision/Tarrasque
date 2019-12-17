using System;
using System.Collections.Generic;
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
        public DateRange Timestamp { get; set; }
        public long Latest { get; set; }
        public List<long> History { get; set; }

        public Checkpoint()
        {
            this.Timestamp = new DateRange();
            this.History = new List<long>();
        }
    }
}
