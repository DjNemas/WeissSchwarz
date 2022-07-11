using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WeißSchwarzSharedClasses.Models
{
    public class Card
    {
        public string CardID { get; set; }
        public string Prefix { get; set; }
        [Key]
        public string LongID { get; set; }
        public string Name { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CardType Type { get; set; }
        public int? Level { get; set; }
        public int? Power { get; set; }
        public List<Trigger>? Triggers { get; set; }
        public string Rarity { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Color Color { get; set; }
        public int? Cost { get; set; }
        public int? Soul { get; set; }
        public List<Trait>? Traits { get; set; }
        public string? SkillText { get; set; }
        public string? FalvorText { get; set; }
        public string? IllustrationText { get; set; }
        public string ImageURL { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public Side Side { get; set; }
    }

    public enum Side
    {
        Weiß,
        Schwarz
    }

    public enum Color
    {
        Red,
        Green,
        Blue,
        Yellow,
        None
    }

    public enum CardType
    {
        Character,
        Event,
        Climax,
        None
    }

}
