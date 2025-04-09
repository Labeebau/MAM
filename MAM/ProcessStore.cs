using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAM
{
    public static class ProcessStore
    {
        public static ObservableCollection<Process> AllProcesses { get; set; } = new ObservableCollection<Process>();
    }
}
