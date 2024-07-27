namespace template.Server.Models
{
    public class AnswerData
    {
        public int id { get; set; }
        public int questionId { get; set; }
        public string textContent { get; set; }
        public string imageContentWithoutText { get; set; }
        public bool IsCorrect { get; set; }
        
        
    }
}
