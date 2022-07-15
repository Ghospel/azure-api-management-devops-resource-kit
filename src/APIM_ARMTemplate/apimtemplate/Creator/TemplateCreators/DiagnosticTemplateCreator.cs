using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Create
{
    public class DiagnosticTemplateCreator
    {
        public DiagnosticTemplateResource CreateAPIDiagnosticTemplateResource(APIConfig api, string[] dependsOn)
        {
            // create diagnostic resource with properties
            DiagnosticTemplateResource diagnosticTemplateResource = new DiagnosticTemplateResource()
            {
                name = $"[concat(parameters('{ParameterNames.ApimServiceName}'), '/{api.name}/{api.diagnostic.name}')]",
                type = ResourceTypeConstants.APIDiagnostic,
                apiVersion = GlobalConstants.APIVersion,
                properties = new DiagnosticTemplateProperties()
                {
                    alwaysLog = api.diagnostic.alwaysLog,
                    sampling = api.diagnostic.sampling,
                    frontend = api.diagnostic.frontend,
                    backend = api.diagnostic.backend,
                    enableHttpCorrelationHeaders = api.diagnostic.enableHttpCorrelationHeaders
                },
                dependsOn = dependsOn
            };
            // reference the provided logger if loggerId is provided
            if (api.diagnostic.paramLoggerId)
            {
                diagnosticTemplateResource.properties.loggerId = $"[resourceId('Microsoft.ApiManagement/service/loggers', parameters('{ParameterNames.ApimServiceName}'), parameters('{ParameterNames.ApplicationInsightsName}'))]";
            }
            else if (api.diagnostic.loggerId != null)
            {
                diagnosticTemplateResource.properties.loggerId = $"[resourceId('Microsoft.ApiManagement/service/loggers', parameters('{ParameterNames.ApimServiceName}'), '{api.diagnostic.loggerId}')]";
            }

            // apply parameterization of diagnostics sampling percentage if nothing is supplied
            if (diagnosticTemplateResource.properties.sampling == null)
            {
                diagnosticTemplateResource.properties.sampling =
                    new DiagnosticTemplateSamplingParameterized()
                    {
                        samplingType = "fixed",
                        percentage = $"[parameters('{ParameterNames.SamplingPercentage}')]"
                    };
            }

            // apply parameterization of diagnostics payload logging size if nothing is supplied
            var parameterizedBody = new DiagnosticTemplateRequestResponseBodyParameterized()
            {
                bytes = $"[parameters('{ParameterNames.MaxLoggingPayloadSize}')]"
            };

            if (diagnosticTemplateResource.properties.frontend == null)
            {
                diagnosticTemplateResource.properties.frontend = new DiagnosticTemplateFrontendBackend();
                diagnosticTemplateResource.properties.frontend.request = new DiagnosticTemplateRequestResponse();
                diagnosticTemplateResource.properties.frontend.request.body = parameterizedBody;
                diagnosticTemplateResource.properties.frontend.response = new DiagnosticTemplateRequestResponse();
                diagnosticTemplateResource.properties.frontend.response.body = parameterizedBody;
            }

            if (diagnosticTemplateResource.properties.backend == null)
            {
                diagnosticTemplateResource.properties.backend = new DiagnosticTemplateFrontendBackend();
                diagnosticTemplateResource.properties.backend.request = new DiagnosticTemplateRequestResponse();
                diagnosticTemplateResource.properties.backend.request.body = parameterizedBody;
                diagnosticTemplateResource.properties.backend.response = new DiagnosticTemplateRequestResponse();
                diagnosticTemplateResource.properties.backend.response.body = parameterizedBody;
            }
            return diagnosticTemplateResource;
        }
    }
}
