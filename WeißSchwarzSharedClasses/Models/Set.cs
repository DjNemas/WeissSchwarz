using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WeißSchwarzSharedClasses.Models
{
    public class Set
    {
        [Key]
        public int ID { get; set; }
        public string SetID { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SetType Type { get; set; }
        public string Name { get; set; }
        public List<Card> Cards { get; set; }

        [NotMapped]
        [JsonIgnore]
        public int NumberOfCards { get => Cards.Count(); }

    }

    public enum SetType
    {
        BoosterPack,
        TrialDeck,
        ExtraPack,
        PRCard,
        Others,
        None
    }
}
