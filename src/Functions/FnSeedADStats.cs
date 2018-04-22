using HGV.Tarrasque.Data;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Functions
{
    public static class FnSeedADStats
    {
        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("FnSeedADStats")]
        public static async Task Run(
            // Seed
            [QueueTrigger("hgv-ad-stats-seed")]string seed,
            // Configuration File
            [Blob("hgv-master/valid-abilities.json", System.IO.FileAccess.Read)]TextReader abilitiesBlob,
            // Binder (dynamic output binding)
            Binder binder,
            // Logger
            TraceWriter log)
        {
            log.Info($"FnSeedADStats: started at {DateTime.UtcNow}");

            var serailizer = JsonSerializer.CreateDefault();
            var validAbilities = (List<int>)serailizer.Deserialize(abilitiesBlob, typeof(List<int>));

            await SeedAbilities(binder, serailizer, validAbilities);
            await SeedCombos(binder, serailizer, validAbilities);
        }

        private static async Task SeedCombos(Binder binder, JsonSerializer serailizer, List<int> validAbilities)
        {
            var pairs = validAbilities.SelectMany((lhs, i) => validAbilities.Skip(i + 1).Select(rhs => Tuple.Create<int, int>(lhs, rhs)));
            foreach (var pair in pairs)
            {

                var attr = new BlobAttribute($"hgv-stats/18/combos/{pair.Item1}-{pair.Item2}");
                using (var writer = await binder.BindAsync<TextWriter>(attr))
                {
                    var stats = new AbilityDraftStat();
                    stats.Abilities.Add(pair.Item1);
                    stats.Abilities.Add(pair.Item2);
                    serailizer.Serialize(writer, stats);
                }
            }
        }

        private static async Task SeedAbilities(Binder binder, JsonSerializer serailizer, List<int> validAbilities)
        {
            foreach (var id in validAbilities)
            {
                var attr = new BlobAttribute($"hgv-stats/18/abilities/{id}");
                using (var writer = await binder.BindAsync<TextWriter>(attr))
                {
                    var stats = new AbilityDraftStat();
                    stats.Abilities.Add(id);
                    serailizer.Serialize(writer, stats);
                }
            }
        }
    }
}
