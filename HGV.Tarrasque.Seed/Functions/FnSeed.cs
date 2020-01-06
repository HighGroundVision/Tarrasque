using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using HGV.Tarrasque.Seed.Services;

namespace HGV.Tarrasque.Seed.Functions
{
    public class FnSeed
    {
        private readonly ISeedService _service;

        public FnSeed(ISeedService service)
        {
            _service = service;
        }

        
    }
}
