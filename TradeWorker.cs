using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using GW2Miner.Engine;
using GW2Miner.Domain;

namespace GW2Miner
{
    class TradeWorker
    {
        private ConnectionManager _cm;

        public TradeWorker()
        {
            _cm = new ConnectionManager();
        }

        public async Task<List<Item>> get_items(params int[] item_ids)
        {
            String itemsString = String.Join(",", item_ids);
            String query = String.Format("ids={0}", itemsString);
            Stream itemStreams = await _cm.RequestItems("search", query, false);
            //_cm._requested.WaitOne();

            ItemParser itemParser = new ItemParser();
            return itemParser.Parse(itemStreams);
        }
    }
}
