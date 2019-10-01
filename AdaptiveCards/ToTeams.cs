using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace O365Connectors
{

    public class Fact
    {
        public string name { get; set; }
        public string value { get; set; }
    }

    public class Image
    {
        public string image { get; set; }
    }

    public class Section
    {
        public string activityTitle { get; set; }
        public string activitySubtitle { get; set; }
        public string activityText { get; set; }
        public string activityImage { get; set; }
        public string title { get; set; }
        public List<Fact> facts { get; set; }
        public List<Image> images { get; set; }
    }

    public class PotentialAction
    {
        [JsonProperty("@context")]
        public string @context { get; set; }
        [JsonProperty("@type")]
        public string @type { get; set; }
        [JsonProperty("@id")]
        public string id { get; set; }
        public string name { get; set; }
        public List<string> target { get; set; }
    }

    public class Content
    {
        [JsonProperty("@type")]
        public string type { get; set; } = "MessageCard";
        [JsonProperty("@context")]
        public string context { get; set; } = "http://schema.org/extensions";
        public string summary { get; set; }
        public string title { get; set; }
        public List<Section> sections { get; set; }
        public List<PotentialAction> potentialAction { get; set; }
    }

    public class ToTeams
    {
        public string contentType { get; set; } = "application/vnd.microsoft.teams.card.o365connector";
        public Content content { get; set; }
    }
}

