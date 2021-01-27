using System.Collections.Generic;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extract;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Create
{
    // renamed to NamedValues in APIM
    public class PropertyTemplateCreator : TemplateCreator
    {
        public Template CreatePropertyTemplate(CreatorConfig creatorConfig)
        {
            // create empty template
            Template propertyTemplate = CreateEmptyTemplate();

            // add parameters
            propertyTemplate.parameters = new Dictionary<string, TemplateParameterProperties>
            {
                { ParameterNames.ApimServiceName, new TemplateParameterProperties(){ type = "string" } }
            };

            if (creatorConfig.paramPolicyNamedValue)
            {
                propertyTemplate.parameters.Add(ParameterNames.NamedValues, new TemplateParameterProperties { type = "object" });
            }

            List<TemplateResource> resources = new List<TemplateResource>();
            foreach (PropertyConfig namedValue in creatorConfig.namedValues)
            {
                var value = namedValue.value == null ? null
                    : creatorConfig.paramPolicyNamedValue
                        ? $"[parameters('{ParameterNames.NamedValues}').{ExtractorUtils.GenValidParamName(namedValue.displayName, ParameterPrefix.Property)}]"
                        : namedValue.value;

                var keyVault = namedValue.keyVault == null ? null
                    : creatorConfig.paramPolicyNamedValue
                        ? new PropertyKeyVaultResourceProperties
                        {
                            secretIdentifier = $"[parameters('{ParameterNames.NamedValues}').{ExtractorUtils.GenValidParamName(namedValue.displayName, ParameterPrefix.Property)}]"
                        }
                        : namedValue.keyVault;

                // create property resource with properties
                PropertyTemplateResource propertyTemplateResource = new PropertyTemplateResource()
                {
                    name = $"[concat(parameters('{ParameterNames.ApimServiceName}'), '/{namedValue.displayName}')]",
                    type = ResourceTypeConstants.NamedValues,
                    apiVersion = GlobalConstants.NamedValuesAPIVersion,
                    properties = new PropertyResourceProperties()
                    {
                        displayName = namedValue.displayName,
                        value = value,
                        secret = namedValue.secret,
                        keyVault = keyVault,
                        tags = namedValue.tags
                    },
                    dependsOn = new string[] { }
                };
                resources.Add(propertyTemplateResource);
            }

            propertyTemplate.resources = resources.ToArray();
            return propertyTemplate;
        }
    }
}
