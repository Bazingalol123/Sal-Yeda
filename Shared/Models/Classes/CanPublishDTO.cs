using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Classes
{
    public class CanPublishDTO
    {
        public int ID { get; set; }
        public bool CanPublish { get; set; } 
        public bool isPublished { get; set; }
    }
}
