using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using template.Server.Helpers;
using template.Server.Data;
using template.Server.Models;

namespace template.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class UnityController : ControllerBase
    {
        //Database Configuration
        private readonly DbRepository _db;

        public UnityController(DbRepository db)
        {
            _db = db;
        }
        //Get game data IF gameCode exists & game is published
        [HttpGet("CodeCheck/{authGameCode}")]
        public async Task<ActionResult> GameCode(int authGameCode)
        {
            //Checking if the input is bigger than 0
            if (authGameCode < 0)
            {
                return BadRequest("Invalid game code.");
            }

            object param = new
            {
                CodeToCheck = authGameCode
            };

            string gameQuery = "SELECT ID, GameName, isPublished, questionTime FROM Games WHERE GameCode = @CodeToCheck";
            var gameData = await _db.GetRecordsAsync<GameSettings>(gameQuery, param);
            GameSettings game = gameData.FirstOrDefault();

            if (game != null)
            {
                if (game.isPublished == true)
                {
                    string questionQuery = "SELECT ID, content, GameId FROM questionList WHERE GameId = @GameId";
                    var questionData = await _db.GetRecordsAsync<QuestionsData>(questionQuery, new { GameId = game.ID });
                    List<QuestionsData> questions = questionData.ToList();


                    foreach (var question in questions)
                    {
                        string answerQuery = "SELECT ID, textContent, IsCorrect, QuestionId FROM AnswerList WHERE QuestionId = @QuestionId";
                        var answerData = await _db.GetRecordsAsync<AnswerData>(answerQuery, new { QuestionId = question.ID });
                        List<AnswerData> answers = answerData.ToList();

                        question.AnswerList = answers;
                    }
                    //Map to DTO to show only Game Name, Game Time, and question list (Answer list within)
                    var gameDto = new DtoToShow
                    {
                        GameName = game.GameName,
                        questionTime = game.questionTime,
                        QuestionsList = questions
                    };

                  


                    return Ok(gameDto);
                }
                else
                {
                    return Unauthorized("Game exists, but not published.");
                }

            }
            else
            {
                return NotFound("Game was not found.");
            }

        }
    }
}










