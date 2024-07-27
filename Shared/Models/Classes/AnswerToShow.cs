using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace template.Shared.Models.Classes
{

    public class AnswerToShow
    {
        public int id { get; set; }

        public int questionId { get; set; }

            
        [MaxLength(40, ErrorMessage = "מסיח יכול להכיל מקסימום 40 תווים.")]
        public string textContent { get; set; }

        public string imageContentWithoutText { get; set; }

        [Required]
        public bool IsCorrect { get; set; }

        public bool IsImageAnswer { get; set; }


    }

   



}
