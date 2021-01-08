﻿using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Microsoft.Azure.Management.ApiManagement.ArmTemplates.Common
{
    public class MasterTemplateResource : TemplateResource
    {
        public MasterTemplateProperties properties { get; set; }
    }

    public class MasterTemplateProperties
    {
        public string mode { get; set; }
        public MasterTemplateLink templateLink { get; set; }
        public Dictionary<string, TemplateParameterProperties> parameters { get; set; }
    }

    public class MasterTemplateLink
    {
        public string uri { get; set; }
        public string contentVersion { get; set; }
    }

    public class NestedTemplateResource : TemplateResource
    {
        public NestedTemplateProperties properties { get; set; }
    }

    public class NestedTemplateProperties
    {
        public string mode { get; set; }

        public NestedExpressionEvaluationOptions expressionEvaluationOptions { get; set; }

        public JObject template { get; set; }

        public Dictionary<string, TemplateParameterProperties> parameters { get; set; }
    }

    public class NestedExpressionEvaluationOptions
    {
        public string scope { get; set; }
    }
}
