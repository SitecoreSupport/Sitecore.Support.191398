using Sitecore.ContentSearch.Diagnostics;
using Sitecore.ContentSearch.Maintenance;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
