using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Utilities
{
    public class NotModifiedResult : StatusCodeResult
    {
        public NotModifiedResult()
            : base(304)
        {}
    }
}
