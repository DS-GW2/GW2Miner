using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using GW2Miner.Engine;
using GW2Miner.Domain;

namespace GW2Miner
{
    static class Program
    {
        static TradeWorker trader = new TradeWorker();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args) 
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new Form1());

          /*  try
            {
                using (StreamReader sr = new StreamReader(@"C:\David\gw2\tp\GW2Miner-master\Sample Data\gw2-allitems.json"))
                {
                    ItemParser itemParser = new ItemParser();
                    List<Item> itemList = itemParser.Parse(sr.BaseStream);

                    foreach (Item item in itemList)
                    {
                        Console.WriteLine("{0}", item.Name);
                    }
                }
            }
            catch(Exception e)
            {
                // handle error
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }*/

            /*TradeWorker trader = new TradeWorker();
            Task<List<Item>> itemList = trader.get_items(12236, 20316);
            //Task<List<Item>> itemList = trader.get_my_sells();
            //Task<List<Item>> itemList = trader.get_my_buys();

            foreach (Item item in itemList.Result)
            {
                Console.WriteLine("{0}", item.Name);
            }*/

            /*TradeWorker trader = new TradeWorker();
            Task<List<ItemBuySellListingItem>> itemSellListing = trader.get_sell_listings(12236);

            foreach (ItemBuySellListingItem listing in itemSellListing.Result)
            {
                Console.WriteLine("{0} {1}", listing.PricePerUnit, listing.NumberAvailable);
            }*/

            try
            {
                Task<List<Item>> sellItemList = trader.get_my_sells();

                Console.WriteLine("Sell Count: {0}", sellItemList.Result.Count);

                foreach (Item item in sellItemList.Result)
                {
                    Task<List<ItemBuySellListingItem>> itemSellListing = trader.get_sell_listings(item.Id);
                    ItemBuySellListingItem listing = itemSellListing.Result[0];
                    if (listing.PricePerUnit < item.UnitPrice)
                    {
                        // someone is undercutting me

                        // we can assume the sellListing is sorted from lowest to highest unit sale price
                        int sum = 0;
                        int profit = 0;
                        bool ridiculous = true;
                        foreach (ItemBuySellListingItem sellListing in itemSellListing.Result)
                        {
                            if (item.UnitPrice * 0.85 > sellListing.PricePerUnit)
                            {
                                profit = profit + (int)Math.Floor(item.UnitPrice * 0.85 - sellListing.PricePerUnit) * sellListing.NumberAvailable;
                                sum = sum + sellListing.PricePerUnit * sellListing.NumberAvailable;
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("undercut {0}: My Price = {1}({2}) Their Price = {3}({4}) Profit = {5} Cost to rectify = {6}", item.Name,
                                             item.UnitPrice, item.Quantity, sellListing.PricePerUnit, sellListing.NumberAvailable, profit, sum);
                                Console.ResetColor();
                            }
                            else
                            {
                                if (sellListing.PricePerUnit == item.UnitPrice)
                                {
                                    if (ridiculous)
                                    {
                                        Console.WriteLine("Ridiculous undercut {0}: My Price = {1} Profit = {2} Cost to rectify = {3}", item.Name,
                                                item.UnitPrice, profit, sum);
                                        Console.WriteLine("Do you want to buy them up? (y/n)");
                                        ConsoleKeyInfo key = Console.ReadKey(true);
                                        if (key.KeyChar == 'y')
                                        {
                                            trader.BuyAllRidiculousSellOrders(itemSellListing.Result, item);
                                        }
                                    }
                                    break;
                                }
                                else
                                {
                                    ridiculous = false;
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("undercut {0}: My Price = {1}({2}) Their Price = {3}({4})", item.Name,
                                                 item.UnitPrice, item.Quantity, sellListing.PricePerUnit, sellListing.NumberAvailable);
                                    Console.ResetColor();
                                }
                            }
                        }
                    }
                }

                sellItemList = trader.get_my_sells();
                Task<List<Item>> itemList = trader.get_my_buys();

                Console.WriteLine("Buy Count: {0}", itemList.Result.Count);
                List<Item> runesCollection = null;
                List<Item> sigilsCollection = null;
                foreach (Item item in itemList.Result)
                {
                    int worth;
                    Item richItem = trader.make_rich_item(item).Result;

                    if (richItem.TypeId == TypeEnum.Armor)
                    {
                        if (runesCollection == null) runesCollection = trader.search_items("", true, TypeEnum.Upgrade_Component, (int)UpgradeComponentSubTypeEnum.Rune).Result;
                        worth = trader.Worth(item, sellItemList.Result, runesCollection);
                    }
                    else if (richItem.TypeId == TypeEnum.Weapon)
                    {
                        if (sigilsCollection == null) sigilsCollection = trader.search_items("", true, TypeEnum.Upgrade_Component, (int)UpgradeComponentSubTypeEnum.Sigil).Result;
                        worth = trader.Worth(item, sellItemList.Result, sigilsCollection);
                    }
                    else
                    {
                        worth = trader.Worth(item, sellItemList.Result);
                    }

                    if (worth <= item.UnitPrice)
                    {
                        Console.WriteLine("{0}: {1} is not worth({2}) the price({3}) you are bidding for anymore.", item.ListingId, item.Name, worth, item.UnitPrice);
                        Console.WriteLine("Do you want to cancel this bid?");
                        ConsoleKeyInfo key = Console.ReadKey(true);
                        if (key.KeyChar == 'y')
                        {
                            trader.cancelBuyOrder(item.Id, item.ListingId);
                            continue;
                        }
                    }

                    Task<List<ItemBuySellListingItem>> itemBuyListing = trader.get_buy_listings(item.Id);
                    ItemBuySellListingItem listing = itemBuyListing.Result[0];
                    //Console.WriteLine("{0} {1} {2}", item.Name, listing.PricePerUnit, item.UnitPrice);
                    if (listing.PricePerUnit > item.UnitPrice)
                    {
                        bool IAmSelling, underCut;
                        Item myItemOnSale;
                        int sellPrice = trader.GetMySellPrice(sellItemList.Result, item, out IAmSelling, out underCut, out myItemOnSale);
                        //int breakEvenPrice = (int)Math.Floor(sellPrice * 0.85);
                        int breakEvenPrice = worth;

                        Console.WriteLine("{0}: {1}: you bid {2}(quantity: {5}) but others have bid {3}(quantity: {4}) - BreakEven {6} at Sell Price {7}",
                                    item.ListingId, item.Name, item.UnitPrice, listing.PricePerUnit, listing.NumberAvailable,
                                            item.Quantity, breakEvenPrice, sellPrice);

                        if (IAmSelling)
                        {
                            if (!underCut)
                                Console.WriteLine("I am still selling this item!");
                            else
                            {
                                Console.WriteLine("I am still selling this item but I am being undercut!");
                            }

                            if (myItemOnSale != null)
                            {
                                Console.WriteLine("I am selling {0} at {1}(quantity: {2})", myItemOnSale.Name, myItemOnSale.UnitPrice, myItemOnSale.Quantity);
                            }
                        }

                        Console.WriteLine("Do you want to outbid this listing? (y/n)");
                        ConsoleKeyInfo key = Console.ReadKey(true);
                        if (key.KeyChar == 'y')
                        {
                            if (breakEvenPrice <= item.UnitPrice)
                            {
                                // Not possible to up my bid with that kind of sell price
                                Console.WriteLine("Not possible to up my bid!  You need a sell price of {0} to earn a profit.", (int)Math.Ceiling((item.UnitPrice + 1) / 0.85));
                            }
                            else
                            {
                                trader.RenewBuyOrder(item, Math.Min(listing.PricePerUnit + 1, breakEvenPrice - 1));
                            }
                        }
                    }
                    else if (itemBuyListing.Result.Count > 1 && (item.UnitPrice - itemBuyListing.Result[1].PricePerUnit) > 1)
                    {
                        listing = itemBuyListing.Result[1];
                        Console.WriteLine("You are paying too much for {1}: you bid {2}(quantity: {5}) but others have bid {3}(quantity: {4}) - BreakEven {6} at Sell Price {7}",
                                    item.ListingId, item.Name, item.UnitPrice, listing.PricePerUnit, listing.NumberAvailable,
                                            item.Quantity, (int)Math.Floor((item.MinSaleUnitPrice - 1) * 0.85), item.MinSaleUnitPrice - 1);
                        Console.WriteLine("Do you want to lower your bid? (y/n)");
                        ConsoleKeyInfo key = Console.ReadKey(true);
                        if (key.KeyChar == 'y')
                        {
                            trader.RenewBuyOrder(item, listing.PricePerUnit + 1);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(ExceptionHelper.FlattenException(e));
            }

            Console.WriteLine("Hit ENTER to exit...");
            Console.ReadLine();
        }
    }
}
