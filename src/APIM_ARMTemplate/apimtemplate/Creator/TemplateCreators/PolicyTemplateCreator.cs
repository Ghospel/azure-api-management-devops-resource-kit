using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common;
using Microsoft.Azure.Management.ApiManagement.ArmTemplates.Extract;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Create
{
    public class PolicyTemplateCreator : TemplateCreator
    {
        private FileReader fileReader;

        public PolicyTemplateCreator(FileReader fileReader)
        {
            this.fileReader = fileReader;
        }

        public Template CreateGlobalServicePolicyTemplate(CreatorConfig creatorConfig)
        {
            // create empty template
            Template policyTemplate = CreateEmptyTemplate();

            // add parameters
            policyTemplate.parameters = new Dictionary<string, TemplateParameterProperties>
            {
                { ParameterNames.ApimServiceName, new TemplateParameterProperties(){ type = "string" } }
            };

            List<TemplateResource> resources = new List<TemplateResource>();

            // create global service policy resource with properties
            string globalServicePolicy = creatorConfig.policy;
            Uri uriResult;
            bool isUrl = Uri.TryCreate(globalServicePolicy, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            // create policy resource with properties
            PolicyTemplateResource policyTemplateResource = new PolicyTemplateResource()
            {
                name = $"[concat(parameters('{ParameterNames.ApimServiceName}'), '/policy')]",
                type = ResourceTypeConstants.GlobalServicePolicy,
                apiVersion = GlobalConstants.APIVersion,
                properties = new PolicyTemplateProperties()
                {
                    // if policy is a url inline the url, if it is a local file inline the file contents
                    format = isUrl ? "rawxml-link" : "rawxml",
                    value = isUrl ? globalServicePolicy : this.fileReader.RetrieveLocalFileContents(globalServicePolicy)
                },
                dependsOn = new string[] { }
            };

            ProcessPolicy(policyTemplateResource, creatorConfig);

            resources.Add(policyTemplateResource);

            policyTemplate.resources = resources.ToArray();

            return policyTemplate;
        }

        public PolicyTemplateResource CreateAPIPolicyTemplateResource(CreatorConfig creatorConfig, APIConfig api, string[] dependsOn)
        {
            Uri uriResult;
            bool isUrl = Uri.TryCreate(api.policy, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            // create policy resource with properties
            PolicyTemplateResource policyTemplateResource = new PolicyTemplateResource()
            {
                name = $"[concat(parameters('{ParameterNames.ApimServiceName}'), '/{api.name}/policy')]",
                type = ResourceTypeConstants.APIPolicy,
                apiVersion = GlobalConstants.APIVersion,
                properties = new PolicyTemplateProperties()
                {
                    // if policy is a url inline the url, if it is a local file inline the file contents
                    format = isUrl ? "rawxml-link" : "rawxml",
                    value = isUrl ? api.policy : this.fileReader.RetrieveLocalFileContents(api.policy)
                },
                dependsOn = dependsOn
            };
            ProcessPolicy(policyTemplateResource, creatorConfig);
            return policyTemplateResource;
        }

        public PolicyTemplateResource CreateProductPolicyTemplateResource(CreatorConfig creatorConfig, ProductConfig product, string[] dependsOn)
        {
            if (string.IsNullOrEmpty(product.name))
            {
                product.name = product.displayName;
            }

            Uri uriResult;
            bool isUrl = Uri.TryCreate(product.policy, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            // create policy resource with properties
            PolicyTemplateResource policyTemplateResource = new PolicyTemplateResource()
            {
                name = $"[concat(parameters('{ParameterNames.ApimServiceName}'), '/{product.name}/policy')]",
                type = ResourceTypeConstants.ProductPolicy,
                apiVersion = GlobalConstants.APIVersion,
                properties = new PolicyTemplateProperties()
                {
                    // if policy is a url inline the url, if it is a local file inline the file contents
                    format = isUrl ? "rawxml-link" : "rawxml",
                    value = isUrl ? product.policy : this.fileReader.RetrieveLocalFileContents(product.policy)
                },
                dependsOn = dependsOn
            };
            ProcessPolicy(policyTemplateResource, creatorConfig);
            return policyTemplateResource;
        }

        public PolicyTemplateResource CreateOperationPolicyTemplateResource(CreatorConfig creatorConfig, KeyValuePair<string, OperationsConfig> policyPair, string apiName, string[] dependsOn)
        {
            Uri uriResult;
            bool isUrl = Uri.TryCreate(policyPair.Value.policy, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
            // create policy resource with properties
            PolicyTemplateResource policyTemplateResource = new PolicyTemplateResource()
            {
                name = $"[concat(parameters('{ParameterNames.ApimServiceName}'), '/{apiName}/{policyPair.Key}/policy')]",
                type = ResourceTypeConstants.APIOperationPolicy,
                apiVersion = GlobalConstants.APIVersion,
                properties = new PolicyTemplateProperties()
                {
                    // if policy is a url inline the url, if it is a local file inline the file contents
                    format = isUrl ? "rawxml-link" : "rawxml",
                    value = isUrl ? policyPair.Value.policy : this.fileReader.RetrieveLocalFileContents(policyPair.Value.policy)
                },
                dependsOn = dependsOn
            };
            ProcessPolicy(policyTemplateResource, creatorConfig);
            return policyTemplateResource;
        }

        public List<PolicyTemplateResource> CreateOperationPolicyTemplateResources(CreatorConfig creatorConfig, APIConfig api, string[] dependsOn)
        {
            // create a policy resource for each policy listed in the config file and its associated provided xml file
            List<PolicyTemplateResource> policyTemplateResources = new List<PolicyTemplateResource>();
            foreach (KeyValuePair<string, OperationsConfig> pair in api.operations)
            {
                policyTemplateResources.Add(this.CreateOperationPolicyTemplateResource(creatorConfig, pair, api.name, dependsOn));
            }
            return policyTemplateResources;
        }

        private void ProcessPolicy(PolicyTemplateResource policy, CreatorConfig creatorConfig)
        {
            if (creatorConfig.paramPolicyNamedValue)
            {
                var matches = Regex.Matches(policy.properties.value, "{{([a-zA-Z0-9-_]*)}}");

                if (matches.Count > 0)
                {
                    var newValue = $"[concat('{policy.properties.value}')]";

                    foreach (Match match in matches)
                    {
                        var param = match.Groups[1].Value;

                        newValue = newValue.Replace(match.Value, $"', parameters('{ParameterNames.NamedValuesInPolicy}').{ExtractorUtils.GenValidParamName(param, ParameterPrefix.Property)}, '");
                    }

                    policy.properties.value = newValue;
                }
            }
        }
    }
}
