using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Classes
{
    public class NewGameDTO
    {

        [Required(ErrorMessage = "חובה לבחור שם למשחק")]
        [MinLength(2, ErrorMessage = "יש להזין לפחות שני תווים")]
        [MaxLength(30, ErrorMessage = "שם המשחק חייב להכיל מקסימום 30 תווים.")]
       
        public string GameName { get; set; }

        [Required(ErrorMessage = "שדה חובה")]
        public int questionTime { get; set; }


    }


}


