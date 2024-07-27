using template.Client.Pages;
using template.Shared.Models.Classes;

namespace template.Client.Services
{
    public class UsefulMethods
    {



        public void ClearFieldsCreatedQuestion(QuestionToShow createdQuestion, bool postCreateQuestion)
        {

            if (createdQuestion != null) {
                createdQuestion.content = string.Empty;
                createdQuestion.image = string.Empty;
                createdQuestion.AnswerList.Clear();
            }
        }
            


    }
}
