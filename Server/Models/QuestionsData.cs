namespace template.Server.Models
{
    public class QuestionsData
    {
        public int ID { get; set; }
        public string content { get; set; }
        public string image { get; set; }
        public int GameId { get; set; }
        public List<AnswerData> AnswerList { get; set; } = new List<AnswerData>();

    }
}
