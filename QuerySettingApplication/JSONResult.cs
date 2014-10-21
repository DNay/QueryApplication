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
