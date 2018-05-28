using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HGV.Tarrasque.Utilities
{
    public class ETagTest
    {
        public static bool Compare(HttpRequest req, EntityTagHeaderValue etag)
        {
            var existing = req.GetTypedHeaders().IfNoneMatch;
            if (existing == null)
                return false;

            return existing.Any(_ => _.Compare(etag, false));
     
        }
    }
}
