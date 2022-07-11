using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WeißSchwarzSharedClasses.Models
{
    public class Trait
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
