using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeißSchwarzSharedClasses.Models
{
    public class DataVersion
    {
        [Key]
        public int ID { get; set; }
        public int Version { get; set; }
    }
}
