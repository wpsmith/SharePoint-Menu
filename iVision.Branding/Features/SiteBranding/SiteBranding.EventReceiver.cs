using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

namespace iVision.Branding.Features.SiteBranding
{
    /// <summary>
    /// This class handles events raised during feature activation, deactivation, installation, uninstallation, and upgrade.
    /// </summary>
    /// <remarks>
    /// The GUID attached to this class may be used during packaging and should not be modified.
    /// </remarks>

    [Guid("f0fbeb4f-6f69-49fe-88ef-ba0a7f5ac1bd")]
    public class SiteBrandingEventReceiver : SPFeatureReceiver
    {
        // Uncomment the method below to handle the event raised after a feature has been activated.

        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            using (SPWeb web = GetWeb(properties))
            {
                // Calculate relative path to site from Web Application root.
                string webAppRelativePath = web.ServerRelativeUrl;
                if (!webAppRelativePath.EndsWith("/"))
                {
                    webAppRelativePath += "/";
                }

                web.MasterUrl = webAppRelativePath + "_catalogs/masterpage/team.master";
                web.CustomMasterUrl = webAppRelativePath + "_catalogs/masterpage/main.master";

                web.Update();


                var ElementDefinitions = properties.Definition.GetElementDefinitions(CultureInfo.CurrentCulture);

                foreach (SPElementDefinition ElementDefinition in ElementDefinitions)
                {
                    if (ElementDefinition.ElementType == "Module")
                    {
                        Helper.UpdateFilesInModule(ElementDefinition, web);

                    }
                }
            }
        }


        // Uncomment the method below to handle the event raised before a feature is deactivated.

        public override void FeatureDeactivating(SPFeatureReceiverProperties properties)
        {
            SPSite siteCollection = properties.Feature.Parent as SPSite;
            if (siteCollection != null)
            {
                SPWeb topLevelSite = siteCollection.RootWeb;

                // Calculate relative path to site from Web Application root.
                string webAppRelativePath = topLevelSite.ServerRelativeUrl;
                if (!webAppRelativePath.EndsWith("/"))
                {
                    webAppRelativePath += "/";
                }

                // Enumerate through each site and apply branding.
                foreach (SPWeb site in siteCollection.AllWebs)
                {
                    site.MasterUrl = webAppRelativePath + "_catalogs/masterpage/seattle.master";
                    site.CustomMasterUrl = webAppRelativePath + "_catalogs/masterpage/seattle.master";
                    site.SiteLogoUrl = string.Empty;
                    site.Update();
                }
            }
        }


        // Uncomment the method below to handle the event raised after a feature has been installed.

        //public override void FeatureInstalled(SPFeatureReceiverProperties properties)
        //{
        //}


        // Uncomment the method below to handle the event raised before a feature is uninstalled.

        //public override void FeatureUninstalling(SPFeatureReceiverProperties properties)
        //{
        //}

        // Uncomment the method below to handle the event raised when a feature is upgrading.

        //public override void FeatureUpgrading(SPFeatureReceiverProperties properties, string upgradeActionName, System.Collections.Generic.IDictionary<string, string> parameters)
        //{
        //}

        // MY methods
        public SPWeb GetWeb(SPFeatureReceiverProperties properties)
        {
            SPWeb site;
            if (properties.Feature.Parent is SPWeb)
            {
                site = (SPWeb)properties.Feature.Parent;
            }
            else if (properties.Feature.Parent is SPSite)
            {
                site = ((SPSite)properties.Feature.Parent).RootWeb;
            }
            else
            {
                throw new Exception("Error 192424234223442 (MY CRAZY MSFT Number that means nothing at all.): Unable to retrieve SPWeb - this feature is not Site or Web-scoped.");
            }
            return site;
        }

        public SPSite GetSite(SPFeatureReceiverProperties properties)
        {
            SPSite site;
            if (properties.Feature.Parent is SPSite)
            {
                site = (SPSite)properties.Feature.Parent;
            }
            else if (properties.Feature.Parent is SPWeb)
            {
                site = ((SPWeb)properties.Feature.Parent).Site;
            }
            else
            {
                throw new Exception("Error 192424234223442 (MY CRAZY MSFT Number that means nothing at all.): Unable to retrieve SPSite - this feature is not Site or Web-scoped.");
            }
            return site;
        }

    }

    internal static class Helper
    {
        internal static void UpdateFilesInModule(SPElementDefinition elementDefinition, SPWeb web)
        {
            XElement xml = elementDefinition.XmlDefinition.ToXElement();
            XNamespace xmlns = "http://schemas.microsoft.com/sharepoint/";
            string featureDir = elementDefinition.FeatureDefinition.RootDirectory;
            Module module = (from m in xml.DescendantsAndSelf()
                             select new Module
                             {
                                 ProvisioningUrl = m.Attribute("Url").Value,
                                 //PhysicalPath = Path.Combine(featureDir, m.Attribute("Path").Value),
                                 Files = (from f in m.Elements(xmlns.GetName("File"))
                                          select new Module.File
                                          {
                                              Name = f.Attribute("Url").Value,
                                              PhysicalPath = Path.Combine(featureDir, f.Attribute("Path").Value),
                                              Properties = (from p in f.Elements(xmlns.GetName("Property"))
                                                            select p).ToDictionary(
                                                              n => n.Attribute("Name").Value,
                                                              v => v.Attribute("Value").Value)
                                          }).ToArray()
                             }).First();

            if (module == null)
            {
                return;
            }

            foreach (Module.File file in module.Files)
            {
                string physicalPath = file.PhysicalPath;
                string virtualPath = string.Concat(web.Url, "/", module.ProvisioningUrl, "/", file.Name);

                if (File.Exists(physicalPath))
                {
                    using (StreamReader sreader = new StreamReader(physicalPath))
                    {
                        if (!CheckOutStatus(web.GetFile(virtualPath)))
                        {
                            web.GetFile(virtualPath).CheckOut();
                        }
                        SPFile spFile = web.Files.Add(virtualPath, sreader.BaseStream, new Hashtable(file.Properties), true);
                        spFile.CheckIn("Updated", SPCheckinType.MajorCheckIn);
                        if (CheckContentApproval(spFile.Item))
                        {
                            spFile.Approve("Updated");
                        }

                        spFile.Update();
                    }
                }
            }

        }

        private static bool CheckOutStatus(SPFile file)
        {
            if (file.CheckOutStatus != SPFile.SPCheckOutStatus.None)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool CheckContentApproval(SPListItem listitem)
        {
            bool isContentApprovalEnabled = listitem.ParentList.EnableModeration;

            return isContentApprovalEnabled;
        }

        public static XElement ToXElement(this XmlNode node)
        {
            XDocument xDoc = new XDocument();

            using (XmlWriter xmlWriter = xDoc.CreateWriter())

                node.WriteTo(xmlWriter);

            return xDoc.Root;

        }
    }

    public class Module
    {
        public string ProvisioningUrl { get; set; }
        //public string PhysicalPath { get; set; }
        public Module.File[] Files { get; set; }

        public class File
        {
            public string Name { get; set; }
            public string PhysicalPath { get; set; }
            public Dictionary<string, string> Properties { get; set; }
        }
    }
}
