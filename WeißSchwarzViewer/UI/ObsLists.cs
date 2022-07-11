using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeißSchwarzSharedClasses.Models;

namespace WeißSchwarzViewer.UI
{
    internal class ObsLists
    {
        public static ObservableCollection<Set> Sets = new ObservableCollection<Set>();
        public static ObservableCollection<Card> Cards = new ObservableCollection<Card>();

    }
}
