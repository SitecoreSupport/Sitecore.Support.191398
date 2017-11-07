using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Diagnostics;
using Sitecore.ContentSearch.Maintenance;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Events;
using Sitecore.Install.Events;
using Sitecore.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace Sitecore.Support.ContentSearch.Events
{
    public class PackagingEventHandler
    {
        public void OnPackageInstallStartingHandler(object sender, EventArgs e)
        {
            //Sitecore.Support.191398
            //'PauseIndexing' is used instaed of 'StopIndexing' as opposed to the original one.
            CrawlingLog.Log.Warn("Pausing indexing while package is being installed.", null);            
            IndexCustodian.PauseIndexing();            
        }
        public void OnPackageInstallItemsEndHandler(object sender, EventArgs e)
        {
            if (e != null)
            {
                SitecoreEventArgs args = e as SitecoreEventArgs;
                if (((args != null) && (args.Parameters != null)) && (args.Parameters.Length == 1))
                {
                    InstallationEventArgs args2 = args.Parameters[0] as InstallationEventArgs;
                    if ((args2 != null) && (args2.ItemsToInstall != null))
                    {
                        List<ItemUri> source = args2.ItemsToInstall.ToList<ItemUri>();
                        CrawlingLog.Log.Info($"Updating '{source.Count}' items from installed items.", null);
                        IEnumerable<IGrouping<string, Item>> enumerable = from item in source.Select<ItemUri, Item>(new Func<ItemUri, Item>(Database.GetItem))
                                                                          where item != null
                                                                          group item by ContentSearchManager.GetContextIndexName(new SitecoreIndexableItem(item));
                        List<Job> list2 = new List<Job>();
                        foreach (IGrouping<string, Item> grouping in enumerable)
                        {
                            if ((grouping != null) && !string.IsNullOrEmpty(grouping.Key))
                            {
                                CrawlingLog.Log.Info($"[Index={grouping.Key}] Updating '{grouping.Count<Item>()}' items from installed items.", null);
                                Job job = IndexCustodian.ForcedIncrementalUpdate(ContentSearchManager.GetIndex(grouping.Key), (IEnumerable<IIndexableUniqueId>)(from item in grouping select new SitecoreItemUniqueId(item.Uri)));
                                list2.Add(job);
                            }
                        }
                        //Sitecore.Support.191398
                        //This loop leads to a deadlock, due to the stopped 'Index_Update_IndexName' job.
                        //foreach (Job job2 in list2)
                        //{
                        //    while (!job2.IsDone)
                        //    {
                        //        Thread.Sleep(100);
                        //    }
                        //}
                        //CrawlingLog.Log.Info(string.Format("Items from installed items have been indexed.", new object[0]), null);
                        //To avoid a deadlock, the above check is performed in a separate thread.
                        Thread parallelThread = new Thread(new ParameterizedThreadStart(CheckingFinishUpdate));
                        parallelThread.Start(list2);
                    }
                }
            }
        }
        public void CheckingFinishUpdate(object list)
        {
            List<Job> list2 = (List<Job>)list;
            foreach (Job job2 in list2)
            {
                while (!job2.IsDone)
                {
                    Thread.Sleep(100);
                }
            }
            CrawlingLog.Log.Info(string.Format("Items from installed items have been indexed.", new object[0]), null);
        }
    }
}
