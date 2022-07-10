using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WeißSchwarzSharedClasses.Models
{
    public class Trigger
    {
        [Key]
        public int Id { get; set; }
        public TriggerType TriggerType { get; set; }
    }

    public enum TriggerType
    {
        [EnumMember(Value = "Soul")]
        Soul,
        [EnumMember(Value = "Salvage")]
        Salvage,
        [EnumMember(Value = "Draw")]
        Draw,
        [EnumMember(Value = "Gate")]
        Gate,
        [EnumMember(Value = "Shot")]
        Shot,
        [EnumMember(Value = "Bounce")]
        Bounce,
        [EnumMember(Value = "Stock")]
        Stock,
        [EnumMember(Value = "Treasure")]
        Treasure,
        [EnumMember(Value = "Choice")]
        Choice,
        [EnumMember(Value = "StandBy")]
        StandBy
    }
}
