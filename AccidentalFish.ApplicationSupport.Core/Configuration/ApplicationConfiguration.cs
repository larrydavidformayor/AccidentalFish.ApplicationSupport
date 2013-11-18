﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AccidentalFish.ApplicationSupport.Core.Configuration
{
    public class ApplicationConfiguration
    {
        protected ApplicationConfiguration()
        {
            SqlServerConnectionStrings = new Dictionary<string, string>();
            StorageAccountConnectionStrings = new Dictionary<string, string>();
            ApplicationComponents = new List<ApplicationComponent>();
        }

        public Dictionary<string, string> SqlServerConnectionStrings { get; set; }

        public Dictionary<string, string> StorageAccountConnectionStrings { get; set; }

        public List<ApplicationComponent> ApplicationComponents { get; set; }

        public static ApplicationConfiguration FromFile(string filename, ApplicationConfigurationSettings settings)
        {
            ApplicationConfiguration configuration = new ApplicationConfiguration();
            XDocument document;
            using (StreamReader reader = new StreamReader(filename))
            {
                if (settings != null)
                {
                    string processedXml = settings.Merge(reader);
                    document = XDocument.Parse(processedXml);
                }
                else
                {
                    document = XDocument.Load(reader);
                }
            }

            document.Root.XPathSelectElements("infrastructure/sql-server").ToList().ForEach(element =>
            {
                configuration.SqlServerConnectionStrings.Add(element.Element("fqn").Value, element.Element("connection-string").Value);
            });
            document.Root.XPathSelectElements("infrastructure/storage-account").ToList().ForEach(element =>
            {
                configuration.StorageAccountConnectionStrings.Add(element.Element("fqn").Value, element.Element("connection-string").Value);
            });

            document.Root.Elements("component").ToList().ForEach(element =>
            {
                ApplicationComponent component = new ApplicationComponent
                {
                    Fqn = element.Attribute("fqn").Value
                };
                XElement sqlServerElement = element.Element("sql-server");
                XElement storageElement = element.Element("storage-account");
                XElement dbContextTypeElement = element.Element("db-context-type");
                XElement defaultBlobContainerNameElement = element.Element("default-blob-container-name");
                XElement defaultQueueNameElement = element.Element("default-queue-name");
                XElement defaultTableNameElement = element.Element("default-table-name");
                XElement settingsElement = element.Element("settings");
                XAttribute defaultBlobContainerAccessAttribute = defaultBlobContainerNameElement == null ? null : defaultBlobContainerNameElement.Attribute("public-permission");

                if (sqlServerElement != null)
                {
                    try
                    {
                        component.SqlServerConnectionString = configuration.SqlServerConnectionStrings[sqlServerElement.Value]; component.SqlServerConnectionString = configuration.SqlServerConnectionStrings[sqlServerElement.Value];
                    }
                    catch (KeyNotFoundException)
                    {
                        throw new InvalidDataException(String.Format("Sql server with fqn of {0} is missing from configuration file.", sqlServerElement.Value));
                    }
                    
                }
                if (storageElement != null)
                {
                    try
                    {
                        component.StorageAccountConnectionString = configuration.StorageAccountConnectionStrings[storageElement.Value];
                    }
                    catch (Exception)
                    {
                        throw new InvalidDataException(String.Format("Storage account with fqn of {0} is missing from configuration file.", storageElement.Value));
                    }
                    
                }
                component.DbContextType = dbContextTypeElement == null ? null : dbContextTypeElement.Value;
                component.DefaultBlobContainerName = defaultBlobContainerNameElement == null ? null : defaultBlobContainerNameElement.Value;
                component.DefaultQueueName = defaultQueueNameElement == null ? null : defaultQueueNameElement.Value;
                component.DefaultTableName = defaultTableNameElement == null ? null : defaultTableNameElement.Value;
                component.DefaultBlobContainerAccessType = BlobContainerPublicAccessType.Off;
                if (defaultBlobContainerAccessAttribute != null)
                {
                    string accessAttribtueValue = defaultBlobContainerAccessAttribute.Value.ToLower();
                    if (accessAttribtueValue == "blob")
                    {
                        component.DefaultBlobContainerAccessType = BlobContainerPublicAccessType.Blob;
                    }
                    else if (accessAttribtueValue == "container")
                    {
                        component.DefaultBlobContainerAccessType = BlobContainerPublicAccessType.Container;
                    }
                }

                if (settingsElement != null)
                {
                    settingsElement.Elements().ToList().ForEach(x =>
                    {
                        string resourceType = null;
                        XAttribute resourceTypeAttr = x.Attribute("resource-type");
                        if (resourceTypeAttr != null)
                        {
                            resourceType = resourceTypeAttr.Value;
                        }
                        component.Settings.Add(new ApplicationComponentSetting
                        {
                            Key = x.Name.LocalName,
                            ResourceType = resourceType,
                            Value = x.Value
                        });                        
                    });
                }

                configuration.ApplicationComponents.Add(component);
            });

            return configuration;
        }
    }
}