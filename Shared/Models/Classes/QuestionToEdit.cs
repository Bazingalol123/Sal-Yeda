using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Classes
{
    public class QuestionToEdit
    {
        public int id { get; set; }
        [Required(ErrorMessage = "חובה לכתוב שאלה כדי לשמור")]
        [MinLength(2, ErrorMessage = "יש להזין לפחות שני תווים בשאלה")]
        [MaxLength(60, ErrorMessage = "השאלה חייבת להכיל מקסימום 60 תווים.")]
        public string content { get; set; }
        public string imageContentWithoutText { get; set; }
        public int GameId { get; set; }
        public List<AnswerToShow> AnswerList { get; set; } = new List<AnswerToShow>();
        

    }
}
