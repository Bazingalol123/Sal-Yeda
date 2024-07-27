namespace template.Server.Models
{
    public class DtoToShow
    {

        public string GameName { get; set; }
        public int questionTime { get; set; }
        public List<QuestionsData> QuestionsList { get; set; }

    }
}
