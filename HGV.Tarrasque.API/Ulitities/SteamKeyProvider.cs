using HGV.Daedalus;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.API.Ulitities
{
    public class SteamKeyProvider : ISteamKeyProvider
    {
        public string GetKey()
        {
            return Environment.GetEnvironmentVariable("SteamKey");
        }
    }
}
