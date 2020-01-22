using HGV.Basilius;
using HGV.Daedalus.GetMatchDetails;
using HGV.Tarrasque.API.Entities;
using HGV.Tarrasque.API.Models;
using HGV.Tarrasque.API.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace HGV.Tarrasque.API.Functions
{
    public class FnProcessAccount
    {
        public FnProcessAccount()
        {
        }

        [FunctionName("FnProcessAccount")]
        public async Task Process(
            [QueueTrigger("hgv-ad-players")]AccountRef item,
            [Blob("hgv-ad-players/{AccountId}.json")]TextReader reader,
            [Blob("hgv-ad-players/{AccountId}.json")]TextWriter writer,
            ILogger log)
        {
            var data = await GetAccountData(reader);

            UpdateAccountData(item, data);

            await StoreAccountData(writer, data);
        }

        private static void UpdateAccountData(AccountRef item, AccountModel data)
        {
            var match = item.Match;
            var player = item.Player;

            var victory = (match.radiant_win && player.player_slot < 6);
            if (victory)
                data.Wins++;
            else
                data.Losses++;

            data.Total++;
        }

        private static async Task<AccountModel> GetAccountData(TextReader reader)
        {
            if(reader == null)
                return new AccountModel();

            var json = await reader.ReadToEndAsync();
            return JsonConvert.DeserializeObject<AccountModel>(json);
        }

        private static async Task StoreAccountData(TextWriter writer, AccountModel data)
        {
            var json = JsonConvert.SerializeObject(data);
            await writer.WriteAsync(json);
        }
    }
}

