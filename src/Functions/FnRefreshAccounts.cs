using HGV.Daedalus;
using HGV.Daedalus.GetMatchHistory;
using HGV.Tarrasque.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HGV.Tarrasque.Functions
{
    public static class FnRefreshAccounts
    {
        private static string APIKEY = "4932A809199A74AB6833EDFD9BADC176";
        private static List<int> SKILLS = new List<int>() { 5003, 5004, 5005, 5007, 5008, 5009, 5012, 5011, 5014, 5523, 5015, 5016, 5017, 5126, 5127, 5128, 5019, 5632, 5021, 5023, 5024, 5025, 5028, 5029, 5027, 5051, 5048, 5050, 5059, 5060, 5061, 5062, 5063, 5052, 5053, 7000, 5055, 5056, 5065, 5066, 5068, 5069, 5071, 5072, 5070, 5075, 5076, 5074, 5082, 5083, 5084, 5102, 5103, 5104, 5098, 5099, 5100, 5094, 5095, 5096, 5106, 5107, 5108, 6937, 5122, 5124, 5123, 5130, 5131, 5132, 5110, 5111, 5112, 6325, 5031, 5032, 5033, 5034, 5040, 5041, 5042, 5134, 5135, 5136, 5044, 5045, 5046, 5078, 5079, 5080, 5114, 5115, 5116, 5118, 5119, 5120, 5138, 5139, 5140, 5142, 5143, 5144, 5146, 5147, 5148, 5150, 5151, 5152, 5154, 5155, 5156, 5158, 5160, 5159, 5162, 5163, 5164, 5168, 5169, 5172, 5173, 5174, 5175, 5178, 5179, 5180, 5182, 5691, 5184, 5086, 5087, 5088, 5090, 5091, 5685, 5190, 5191, 5192, 5186, 5187, 5188, 5194, 5195, 5196, 5198, 5218, 5219, 5220, 5222, 5223, 5224, 5226, 5227, 5228, 5233, 5234, 5235, 5237, 5238, 5239, 5241, 5242, 5243, 5245, 5246, 5247, 5249, 5250, 5251, 5671, 5655, 5675, 5255, 5256, 5257, 5259, 5260, 5261, 5263, 5264, 5265, 5267, 5268, 5269, 5271, 5272, 5273, 5275, 5276, 5277, 5279, 5280, 5281, 5285, 5286, 5287, 5289, 5290, 5291, 5297, 5298, 5299, 5320, 5321, 5322, 5328, 5329, 5330, 5334, 5335, 5336, 5338, 5339, 5340, 5341, 5343, 5344, 5345, 5346, 5347, 5357, 5358, 5359, 5353, 5354, 5355, 5361, 5362, 5363, 5365, 5366, 5368, 5367, 5370, 5371, 5372, 5373, 5374, 5376, 5381, 5382, 5383, 5384, 5385, 5386, 5387, 5389, 5390, 5377, 5378, 5379, 5391, 5392, 5393, 5395, 5396, 5397, 5400, 5401, 5402, 5421, 5422, 5423, 5424, 5412, 5413, 5414, 5417, 5426, 5427, 5428, 5430, 5431, 5432, 5434, 5435, 5436, 5649, 5438, 5439, 5440, 5466, 5442, 5443, 5444, 5448, 5450, 5451, 5453, 5454, 5449, 5458, 5459, 5460, 5462, 5463, 5464, 5666, 5673, 5467, 5468, 5469, 5471, 5472, 5473, 5475, 5476, 5477, 5479, 5503, 5485, 5486, 5487, 5490, 5493, 5489, 5480, 5481, 5482, 7116, 5494, 5495, 5496, 5504, 5505, 5506, 5508, 5509, 5510, 5511, 5514, 5515, 5516, 5518, 5519, 5520, 5524, 5525, 5526, 5645, 5646, 5548, 5549, 5550, 5565, 5566, 5567, 5672, 5641, 5581, 5582, 5583, 5585, 5586, 5587, 5589, 5591, 5593, 5592, 5595, 5596, 5597, 5603, 5604, 5605, 5607, 5608, 5609, 5610, 5611, 5648, 5619, 5620, 5621, 5623, 5625, 5626, 5628, 5631, 5624, 5627, 5637, 5638, 5639, 5599, 5600, 5601, 5635, 5644, 5651, 5652, 5653, 5677, 5678, 5679, 5613, 5614, 5615, 5716, 5721, 5724, 5723, 5719, 5722, 6344, 6461, 6346, 6339, 6341, 6342, 5006, 5010, 5013, 5018, 5129, 5022, 5026, 5030, 5049, 5064, 5057, 5058, 5067, 5073, 5077, 5085, 5105, 5101, 5097, 5109, 5125, 5133, 5113, 5035, 5043, 5137, 5047, 5081, 5117, 5121, 5141, 5145, 5149, 5153, 5157, 5161, 5165, 5177, 5176, 5181, 5185, 5089, 5093, 5193, 5189, 5197, 5221, 5225, 5229, 5236, 5240, 5244, 5248, 5252, 5253, 5258, 5262, 5266, 5270, 5274, 5278, 5282, 5288, 5292, 5300, 5323, 5331, 5337, 5342, 5348, 5349, 5360, 5356, 5364, 5369, 5375, 5380, 5394, 5398, 5403, 5425, 5415, 5416, 5429, 5433, 5437, 5441, 5447, 5452, 5461, 5465, 5470, 5478, 5474, 5488, 5483, 5497, 5507, 5512, 5517, 5521, 5527, 5528, 5551, 5568, 5584, 5588, 5594, 5598, 5606, 5612, 5622, 5630, 5640, 5602, 5654, 5683, 5616, 5617, 5725, 6343, 6459, 6340, 8340 };

        [StorageAccount("AzureWebJobsStorage")]
        [FunctionName("RefreshAccounts")]
        public static void Run(
            [QueueTrigger("hgv-refresh-accounts")]AccountRefreshMessage msg,
            [Blob("hgv-matches/{game_mode}/{dota_id}.json", FileAccess.Read)]CloudBlockBlob blob,
            TraceWriter log
        )
        {
            

            var task = Task.Run(async () => { await GetMatchHistory(log, msg, blob); });
            var result = task.Wait(TimeSpan.FromMinutes(5));
            if(!result)
            {
                throw new TimeoutException($"RefreshAccounts(): account[{msg.dota_id}] failed to complete! Trying Again...");
            }
        }

        private static async Task GetMatchHistory(TraceWriter log, AccountRefreshMessage msg, CloudBlockBlob blob)
        {
            var collection = new List<RecentMatch>();

            var client = new DotaApiClient(APIKEY);
            var history = await client.GetMatchHistory(msg.dota_id);
            var count = 1;

            foreach (var item in history)
            {
                log.Info($"RefreshAccounts(): Processing match[{count++}]");

                while (await TryFetchMatchDetails(client, msg, item.match_id, collection)) {}
            }

            var jsonUpload = JsonConvert.SerializeObject(collection);
            await blob.UploadTextAsync(jsonUpload);
        }

        private static async Task<bool> TryFetchMatchDetails(DotaApiClient client, AccountRefreshMessage msg, long match_id, List<RecentMatch> collection)
        {
            try
            {

                await Task.Delay(TimeSpan.FromSeconds(1));

                var match = await client.GetMatchDetails(match_id);
                if (match.game_mode == msg.game_mode)
                {
                    var player = match.players.FirstOrDefault(_ => _.account_id == msg.dota_id);
                    var team = player.player_slot > 6 ? 2 : 1;
                    var abilities = player.ability_upgrades.Select(_ => _.ability).Distinct().Intersect(SKILLS).ToList();

                    var recent = new RecentMatch()
                    {
                        match_id = match.match_id,
                        match_number = match.match_seq_num,
                        slot = player.player_slot,
                        team = team,
                        won = match.radiant_win ? team == 1 ? true : false : team == 2 ? true : false,
                        start_time = match.start_time,
                        duration = match.duration,
                        game_mode = match.game_mode,
                        hero_id = player.hero_id,
                        kills = player.kills,
                        deaths = player.deaths,
                        assists = player.assists,
                        last_hits = player.last_hits,
                        level = player.level,
                        abilities = abilities
                    };
                    collection.Add(recent);
                }

                return false;
            }
            catch (Exception)
            {
                await Task.Delay(TimeSpan.FromSeconds(30));

                return true;
            }
        }
    }
}
