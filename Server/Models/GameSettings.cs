namespace template.Server.Models
{
    public class GameSettings
    {
        public int ID { get; set; }
        public string GameName { get; set; }
        public bool isPublished { get; set; }
        public int questionTime { get; set; }
        public int GameCode { get; set; }
        public List <QuestionsData> QuestionsList { get; set; }
    }
}
