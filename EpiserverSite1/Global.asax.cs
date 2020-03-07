﻿using System;
using System.Web;
using System.Web.Mvc;

namespace EpiserverSite1
{
    public class EPiServerApplication : HttpApplication// EPiServer.Global
    {
        private readonly EPiServer.Global _g;
        public EPiServerApplication()
        {
            DryIocEpi.DryIocServiceConfigurationProvider.Inspector =
                (service, concrete, lifetime) =>
                {
                    if (service is EPiServer.Core.IContentCacheVersion)
                    {

                    }
                };

            _g = new EPiServer.Global();
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            //Tip: Want to call the EPiServer API on startup? Add an initialization module instead (Add -> New Item.. -> EPiServer -> Initialization Module)
        }
    }
}