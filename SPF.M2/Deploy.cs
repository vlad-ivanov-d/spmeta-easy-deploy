using Microsoft.SharePoint.Client;
using SPMeta2.Common;
using SPMeta2.CSOM.Services;
using SPMeta2.Definitions;
using SPMeta2.Extensions;
using SPMeta2.Models;
using SPMeta2.Services;
using SPMeta2.Syntax.Default;
using System;
using System.Collections.ObjectModel;

namespace SPF.M2
{
    public static class Deploy
    {
        private static string AddZeros(this object Number, int Zeros)
        {
            return Number.AddBeforeSymbols(Zeros, '0');
        }

        private static string AddSpacesBefore(this object Number, int Zeros)
        {
            return Number.AddBeforeSymbols(Zeros, ' ');
        }

        private static string AddBeforeSymbols(this object Str, int Zeros, char Symbol)
        {
            var val = Str.ToString();
            for (var i = 0; i < Zeros; i += 1)
            {
                val = Symbol + val;
            }
            val = val.Substring(val.Length - Zeros);

            return val;
        }

        public static void DeploySPMetaModel(this ClientContext Ctx, WebModelNode model)
        {
            DeploySPMetaModel(Ctx, model, false);
        }

        public static void DeploySPMetaModel(this ClientContext Ctx, SiteModelNode model)
        {
            DeploySPMetaModel(Ctx, model, false);
        }

        public static void DeploySPMetaModel(this ClientContext Ctx, WebModelNode model, bool Incremental)
        {
            BeforeDeployModel(Incremental, x =>
            {
                Console.WriteLine("Provisioning preparing model");
                x.DeployModel(SPMeta2.CSOM.ModelHosts.WebModelHost.FromClientContext(Ctx), model.GetContainersModel());
                Console.WriteLine();
                Console.WriteLine("Provisioning main model");
                x.DeployModel(SPMeta2.CSOM.ModelHosts.WebModelHost.FromClientContext(Ctx), model);
            });
        }

        public static void DeploySPMetaModel(this ClientContext Ctx, SiteModelNode model, bool Incremental)
        {
            BeforeDeployModel(Incremental, x =>
            {
                Console.WriteLine("Provisioning preparing model");
                Console.WriteLine();
                x.DeployModel(SPMeta2.CSOM.ModelHosts.WebModelHost.FromClientContext(Ctx), model.GetContainersModel());
                Console.WriteLine("Provisioning main model");
                Console.WriteLine();
                x.DeployModel(SPMeta2.CSOM.ModelHosts.SiteModelHost.FromClientContext(Ctx), model);
            });

        }
        private static void BeforeDeployModel(bool Incremental, Action<CSOMProvisionService> Deploy)
        {
            var StartedDate = DateTime.Now;
            var provisionService = new CSOMProvisionService();
            if (Incremental)
            {
                var IncProvisionConfig = new IncrementalProvisionConfig();
                IncProvisionConfig.AutoDetectSharePointPersistenceStorage = true;
                provisionService.SetIncrementalProvisionMode(IncProvisionConfig);
            }
            provisionService.OnModelNodeProcessed += (sender, args) =>
            {
                ModelNodeProcessed(sender, args, Incremental);
            };

            Deploy(provisionService);
            provisionService.SetDefaultProvisionMode();
            var FinishedDate = DateTime.Now;
            var DateDiff = (FinishedDate - StartedDate);
            var TotalHrs = Math.Round(DateDiff.TotalHours);
            var TotalMinutes = Math.Round(DateDiff.TotalMinutes);
            var TotalSeconds = Math.Round(DateDiff.TotalSeconds);

            if (TotalHrs == 0)
            {
                if (TotalMinutes == 0)
                {
                    Console.WriteLine(String.Format("It took us {0} seconds", TotalSeconds.ToString()));
                }
                else
                {
                    Console.WriteLine(String.Format("It took us {0} minutes", TotalMinutes.ToString()));
                }
            }
            else
            {
                Console.WriteLine(String.Format("It took us {0} hours", TotalHrs.ToString()));
            }
            Console.WriteLine();
            Console.WriteLine();
        }

        private static void ModelNodeProcessed(object sender, ModelProcessingEventArgs args, bool Incremental)
        {
            var ModelId = args.Model.GetPropertyBagValue(DefaultModelNodePropertyBagValue.Sys.IncrementalProvision.PersistenceStorageModelId);

            bool shouldDeploy = args.CurrentNode.GetIncrementalRequireSelfProcessingValue();

            var NodeName = args.CurrentNode.Value.ToString();
            if (NodeName.Length > 20)
            {
                NodeName = NodeName.Substring(0, 20) + "...";
            }
            if (!Incremental)
            {
                shouldDeploy = true;
            }

            Console.WriteLine(
            string.Format("{5}[{6}] [{0}/{1}] - [{2}%] - [{3}] [{4}]",
            new object[] {
                    args.ProcessedModelNodeCount.AddZeros(4),
                    args.TotalModelNodeCount.AddZeros(4),
                    Math.Round(100d * (double)args.ProcessedModelNodeCount / (double)args.TotalModelNodeCount).AddSpacesBefore(3),
                    args.CurrentNode.Value.GetType().Name,
                    NodeName,
                    (shouldDeploy == true) ? "[+]" : "[-]",
                    ModelId
           }));
        }

        private static ModelNode GetContainersModel(this ModelNode model)
        {
            WebModelNode containersModel = SPMeta2Model.NewWebModel();

            foreach (ModelNode modelNode in model.ChildModels)
            {
                if (modelNode.Value.GetType() == typeof(WebDefinition))
                {
                    containersModel.AddWeb((WebDefinition)modelNode.Value, currentWeb => {
                        currentWeb.GetWebContainersModel(modelNode.ChildModels);
                    });
                }

                if (modelNode.Value.GetType() == typeof(ListDefinition))
                {
                    containersModel.AddList((ListDefinition)modelNode.Value);
                }
            }

            return containersModel;
        }

        private static WebModelNode GetWebContainersModel(this WebModelNode model, Collection<ModelNode> childModels)
        {
            foreach (ModelNode modelNode in childModels)
            {
                if (modelNode.Value.GetType() == typeof(WebDefinition))
                {
                    model.AddWeb((WebDefinition)modelNode.Value, currentWeb => {
                        currentWeb.GetWebContainersModel(modelNode.ChildModels);
                    });
                }

                if (modelNode.Value.GetType() == typeof(ListDefinition))
                {
                    model.AddList((ListDefinition)modelNode.Value);
                }
            }
            return model;
        }

    }
}
