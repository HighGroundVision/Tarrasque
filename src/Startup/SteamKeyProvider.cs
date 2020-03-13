using HGV.Daedalus;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque
{
    public class SteamKeyProvider : ISteamKeyProvider
    {
        public string GetKey()
        {
            return Environment.GetEnvironmentVariable("SteamKey");
        }
    }
}
