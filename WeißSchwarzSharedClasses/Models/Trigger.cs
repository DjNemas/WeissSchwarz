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
    public class Trigger
    {
        [Key]
        public int Id { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TriggerType TriggerType { get; set; }
        [JsonIgnore]
        [NotMapped]
        public string TriggerTypeUI { get => Enum.GetName(TriggerType); }
    }

    public enum TriggerType
    {
        Soul,
        Salvage,
        Draw,
        Gate,
        Shot,
        Bounce,
        Stock,
        Treasure,
        Choice,
        StandBy
    }
}
