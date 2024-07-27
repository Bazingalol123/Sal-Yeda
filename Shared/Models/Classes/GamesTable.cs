using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Classes
{
    public class GamesTable
    {
        public int ID { get; set; }

        public string GameName { get; set; }
        public bool isPublished { get; set; }
        public string GameCode { get; set; }
        public bool CanPublish { get; set; }
        public int questionTime { get; set; }
    }
}
