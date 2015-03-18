using Newtonsoft.Json;

namespace QuerySettingApplication
{
    [JsonObject]
    struct JSONResult
    {
        [JsonProperty]
        public Results Results
        {
            get;
            set;
        }
    }

    struct JSONResultInfo
    {
        [JsonProperty]
        public ResultsInfo Results
        {
            get;
            set;
        }
    }

    [JsonObject]
    struct Results
    {
        [JsonProperty]
        public Binding[] Bindings
        {
            get;
            set;
        }
    }

    [JsonObject]
    struct ResultsInfo
    {
        [JsonProperty]
        public BindingInfo[] Bindings
        {
            get;
            set;
        }
    }

    [JsonObject]
    struct Entity
    {
        [JsonProperty]
        public string Type
        {
            get;
            set;
        }

        [JsonProperty]
        public string Value
        {
            get;
            set;
        }
    }

    [JsonObject]
    struct BindingInfo
    {
        [JsonProperty]
        public Entity P
        {
            get;
            set;
        }

        [JsonProperty]
        public Entity S
        {
            get;
            set;
        }
    }

    [JsonObject]
    struct Binding
    {
        [JsonProperty]
        public Entity Entity
        {
            get;
            set;
        }
    }
}
