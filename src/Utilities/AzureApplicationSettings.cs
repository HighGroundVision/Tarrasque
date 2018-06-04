using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Tarrasque.Utilities
{
    public static class AzureApplicationSettings
    {
        public static string GetSettings(string name)
        {
            return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }
    }
}
