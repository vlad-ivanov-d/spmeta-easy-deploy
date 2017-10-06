# SPF.SPMeta2.Deploy
## Why it's useful?
1. Deploy SPMeta2 models becomes as simple as posible
2. No any SharePoint issues with Lookup fields, so you can use one model
3. Console ouput for provisioning process

## How to use?
1. Add Nuget package "SPF.SPMeta2.Deploy" to your project
2. Add using to your class:
   > using SPF.M2;
3. Make your SPMeta2 models
4. Set incremental provision id for models (optional, if you want to use incremental provision further)
   > webModel.SetIncrementalProvisionModelId("MyFirstWebModel");
5. Deploy your models:
   > using (ClientContext clientContext = new ClientContext("http://contoso.com/sites/spmeta2"))  
   > {  
   >    clientContext.DeploySPMetaModel(siteModel);  
   >    clientContext.DeploySPMetaModel(webModel);  
   > }

## What are the limitations?
1. Only SharePoint CSOM support
2. Only SPMeta2 SiteModel and WebModel provisioning
