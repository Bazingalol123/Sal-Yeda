using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Classes
{
    public class QuestionToShow
    {
        public int id { get; set; }
        public string content { get; set; }
        public string image { get; set; }
        public int GameId { get; set; }
        public List<AnswerToShow> AnswerList { get; set; } = new List<AnswerToShow>();

    }
}
