using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WeißSchwarzSharedClasses.Models
{
    public class Set
    {
        [Key]
        public int ID { get; set; }
        public string SetID { get; set; }
        public SetType Type { get; set; }
        public string Name { get; set; }
        public List<Card> Cards
        {
            get
            {
                return _cards;
            }
            set 
            {
                _cards = value;
                NumberOfCards = value.Count();
            } 
        }

        [NotMapped]
        private List<Card> _cards { get; set; }

        [NotMapped]
        public int NumberOfCards { get; set; }

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
