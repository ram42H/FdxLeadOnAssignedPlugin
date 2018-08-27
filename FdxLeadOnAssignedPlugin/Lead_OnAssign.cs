using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace FdxLeadOnAssignedPlugin
{
    public class Lead_OnAssign : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            //Extract the tracing service for use in debugging sandboxed plug-ins....
            ITracingService tracingService =
                (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            //Obtain execution contest from the service provider.....
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            int step = 0;

            if (context.InputParameters.Contains("Target"))
            {
                step = 2;
                Entity leadPreImageEntity = ((context.PreEntityImages != null) && context.PreEntityImages.Contains("leadpre")) ? context.PreEntityImages["leadpre"] : null;

                Entity leadEntity = new Entity
                {
                    LogicalName = "lead",
                    Id = leadPreImageEntity.Id
                };

                step = 3;
                if (leadPreImageEntity.LogicalName != "lead")
                    return;

                try
                {
                    step = 5;
                    IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
                    IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

                    //Get current user information....
                    step = 6;
                    WhoAmIResponse response = (WhoAmIResponse)service.Execute(new WhoAmIRequest());

                    step = 7;
                    leadEntity["fdx_lastassignedowner"] = new EntityReference("systemuser", ((EntityReference)leadPreImageEntity.Attributes["ownerid"]).Id);

                    step = 8;
                    leadEntity["fdx_lastassigneddate"] = DateTime.UtcNow;

                    step = 9;
                    service.Update(leadEntity);

                    //Update last assign date on account if exist....
                    step = 10;
                    Entity lead = new Entity();
                    lead = service.Retrieve("lead", leadPreImageEntity.Id, new ColumnSet(true));

                    step = 11;
                    if (lead.Attributes.Contains("parentaccountid"))
                    {
                        step = 12;
                        Entity account = new Entity
                        {
                            Id = ((EntityReference)lead.Attributes["parentaccountid"]).Id,
                            LogicalName = "account"
                        };

                        step = 13;
                        account["fdx_lastassigneddate"] = DateTime.UtcNow;

                        step = 14;
                        service.Update(account);
                    }

                }
                catch (FaultException<OrganizationServiceFault> ex)
                {
                    throw new InvalidPluginExecutionException(string.Format("An error occurred in the Lead_OnAssign plug-in at Step {0}.", step), ex);
                }
                catch (Exception ex)
                {
                    tracingService.Trace("Lead_OnAssign: step {0}, {1}", step, ex.ToString());
                    throw;
                }
            }
        }
    }
}
