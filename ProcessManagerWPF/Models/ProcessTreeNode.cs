using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace ProcessManagerWPF.Models
{
    public class ProcessTreeNode
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public ObservableCollection<ProcessTreeNode> Children { get; set; }
            = new ObservableCollection<ProcessTreeNode>();
    }
}