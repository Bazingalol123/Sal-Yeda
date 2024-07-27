using Microsoft.AspNetCore.Mvc;
using template.Server.Data;
using template.Server.Helpers;
using template.Server.Models;
using template.Shared.Models.Classes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TriangleFileStorage;

namespace template.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ServiceFilter(typeof(AuthCheck))]
    public class QuestionsController : ControllerBase
    {
        private readonly DbRepository _db;

        public QuestionsController(DbRepository db)
        {
            _db = db;
        }

        [HttpGet("{gameId}")]
        public async Task<IActionResult> GetQuestions(int authuserId, int gameId)
        {
            if (authuserId > 0)
            {
                string questionQuery = "SELECT * FROM questionList WHERE GameId = @GameId";
                var questions = (await _db.GetRecordsAsync<QuestionsData>(questionQuery, new { GameId = gameId })).ToList();

                if (questions == null || !questions.Any())
                {
                    return NotFound("No questions found for this game.");
                }

                foreach (var question in questions)
                {
                    string answerQuery = "SELECT * FROM AnswerList WHERE QuestionId = @QuestionId";
                    var answers = (await _db.GetRecordsAsync<AnswerData>(answerQuery, new { QuestionId = question.ID })).ToList();
                    question.AnswerList = answers ?? new List<AnswerData>();
                }

                return Ok(questions);
            }
            return Unauthorized("User is not authenticated.");
        }

        [HttpGet("answers/{questionId}")]
        public async Task<IActionResult> GetAnswers(int questionId)
        {
            if (questionId > 0)
            {
                // Check if the question exists
                string questionQuery = "SELECT COUNT(*) FROM questionList WHERE ID = @questionId";
                var questionExists = await _db.ExecuteScalarAsync<int>(questionQuery, new { questionId });

                if (questionExists == 0)
                {
                    return NotFound("Sorry, the question does not exist.");
                }

                // If the question exists, proceed to fetch the answers
                string answerQuery = "SELECT * FROM AnswerList WHERE questionId = @questionId";
                var answerData = (await _db.GetRecordsAsync<AnswerData>(answerQuery, new { questionId })).ToList();

                if (answerData == null || !answerData.Any())
                {
                    return NotFound("Sorry, this question has no answers.");
                }

                // Map AnswerData to AnswerToShow
                var answers = answerData.Select(a => new AnswerToShow
                {
                    id = a.id,
                    questionId = a.questionId,
                    textContent = a.textContent,
                    imageContentWithoutText = a.imageContentWithoutText,
                    IsCorrect = a.IsCorrect,
                    IsImageAnswer = !string.IsNullOrEmpty(a.imageContentWithoutText)
                }).ToList();

                return Ok(answers);
            }

            return BadRequest("Invalid question ID.");
        }

        [HttpGet("edit-question/{questionId}")]
        public async Task<IActionResult> GetQuestion(int authUserId, int questionId)
        {
            if (authUserId > 0)
            {
                string questionQuery = "SELECT * FROM questionList WHERE ID = @ID";
                var questionData = (await _db.GetRecordsAsync<QuestionsData>(questionQuery, new { ID = questionId })).FirstOrDefault();

                if (questionData == null)
                {
                    return NotFound("Question not found.");
                }

                string answerQuery = "SELECT * FROM AnswerList WHERE QuestionId = @QuestionId";
                var answers = await _db.GetRecordsAsync<AnswerData>(answerQuery, new { QuestionId = questionData.ID });
                questionData.AnswerList = answers.ToList();

                var questionToShow = new QuestionToShow
                {
                    id = questionData.ID,
                    content = questionData.content,
                    image = questionData.image,
                    GameId = questionData.GameId,
                    AnswerList = questionData.AnswerList.Select(a => new AnswerToShow
                    {
                        id = a.id,
                        questionId = a.questionId,
                        textContent = a.textContent,
                        imageContentWithoutText = a.imageContentWithoutText,
                        IsCorrect = a.IsCorrect
                    }).ToList()
                };

                return Ok(questionToShow);
            }
            return Unauthorized("User is not authorized.");
        }

        [HttpPost("newQuestion")]
        public async Task<IActionResult> CreateQuestion(int authUserId, [FromBody] QuestionToEdit newQuestion)
        {
            if (authUserId <= 0)
            {
                return Unauthorized("User is not authorized.");
            }

            // Verify the GameId exists
            string verifyGameQuery = "SELECT ID FROM Games WHERE ID = @GameId";
            int gameExists = await _db.ExecuteScalarAsync<int>(verifyGameQuery, new { newQuestion.GameId });

            if (gameExists == 0)
            {
                return NotFound("GameId does not exist.");
            }

            // Check if question limit for the game is reached
            string questionCountQuery = "SELECT COUNT(*) FROM questionList WHERE GameId = @GameId";
            int questionCount = await _db.ExecuteScalarAsync<int>(questionCountQuery, new { GameId = newQuestion.GameId });

            if (questionCount >= 30)
            {
                return BadRequest("Cannot create more than 30 questions for this game.");
            }

            if (newQuestion == null)
            {
                return BadRequest("Invalid question data.");
            }

            // Validate each answer
            foreach (var answer in newQuestion.AnswerList)
            {
                if (string.IsNullOrEmpty(answer.textContent) && string.IsNullOrEmpty(answer.imageContentWithoutText))
                {
                    return BadRequest("Each answer must have either text content or an image.");
                }

                if (!string.IsNullOrEmpty(answer.textContent) && answer.textContent.Length < 2)
                {
                    return BadRequest("Text content must be at least 2 characters long.");
                }
            }

            // Insert the question into the database
            string questionQuery = "INSERT INTO questionList (content, image, GameId) VALUES (@Content, @Image, @GameId)";
            newQuestion.id = await _db.InsertReturnIdAsync(questionQuery, new
            {
                Content = newQuestion.content,
                Image = newQuestion.imageContentWithoutText,
                GameId = newQuestion.GameId
            });

            // Insert the answers into the database
            string answerQuery = "INSERT INTO AnswerList (questionId, textContent, imageContentWithoutText, IsCorrect) VALUES (@QuestionId, @TextContent, @Image, @IsCorrect)";
            foreach (var answer in newQuestion.AnswerList)
            {
                await _db.SaveDataAsync(answerQuery, new
                {
                    QuestionId = newQuestion.id,
                    TextContent = answer.textContent,
                    Image = answer.imageContentWithoutText,
                    IsCorrect = answer.IsCorrect
                });
            }

            return Ok(newQuestion);
        }


        [HttpPost("addAnswer/{questionId}")]
        public async Task<IActionResult> CreateAnswer(int questionId, [FromBody] AnswerToShow newAnswer)
        {
            if (newAnswer == null)
            {
                return BadRequest("Invalid answer data.");
            }

            // Ensure the provided questionId matches the questionId in the newAnswer object (if applicable)
            if (newAnswer.questionId != questionId)
            {
                return BadRequest("Question ID mismatch.");
            }

            // Check if the question exists before inserting the answer
            string questionQuery = "SELECT COUNT(*) FROM questionList WHERE ID = @questionId";
            var questionExists = await _db.ExecuteScalarAsync<int>(questionQuery, new { questionId });

            if (questionExists == 0)
            {
                return NotFound("Question not found.");
            }

            string answerCountQuery = "SELECT COUNT(*) FROM AnswerList WHERE questionId = @questionId";
            var answerCount = await _db.ExecuteScalarAsync<int>(answerCountQuery, new { questionId });

            if (answerCount >= 6)
            {
                return BadRequest("Cannot add more than 6 answers to a question.");
            }

            if (string.IsNullOrEmpty(newAnswer.textContent) && string.IsNullOrEmpty(newAnswer.imageContentWithoutText))
            {
                return BadRequest("Answer content cannot be null.");
            }

            string answerQuery = "INSERT INTO AnswerList (textContent, imageContentWithoutText, IsCorrect, questionId) VALUES (@textContent, @imageContentWithoutText, @isCorrect, @questionId)";
            await _db.SaveDataAsync(answerQuery, new
            {
                textContent = newAnswer.textContent,
                imageContentWithoutText = newAnswer.imageContentWithoutText,
                IsCorrect = newAnswer.IsCorrect,
                questionId = questionId
            });

            return Ok("Answer has been created successfully.");
        }

        [HttpPut("edit-questions/{questionId}")]
        public async Task<IActionResult> EditQuestion(int authUserId, int questionId, [FromBody] QuestionToShow updatedQuestion)
        {
            if (authUserId > 0)
            {
                if (updatedQuestion == null || updatedQuestion.id != questionId)
                {
                    return BadRequest("Question ID mismatch.");
                }

                if (string.IsNullOrEmpty(updatedQuestion.content) || updatedQuestion.AnswerList == null || !updatedQuestion.AnswerList.Any())
                {
                    return BadRequest("Question content and answers cannot be null.");
                }

                string questionQuery = "UPDATE questionList SET content = @Content, image = @Image WHERE ID = @ID";
                await _db.SaveDataAsync(questionQuery, new
                {
                    Content = updatedQuestion.content,
                    Image = updatedQuestion.image,
                    ID = questionId
                });

                foreach (var answer in updatedQuestion.AnswerList)
                {
                    if (string.IsNullOrEmpty(answer.textContent) && string.IsNullOrEmpty(answer.imageContentWithoutText))
                    {
                        return BadRequest("Each answer must have either text content or an image.");
                    }

                    if (!string.IsNullOrEmpty(answer.textContent) && answer.textContent.Length < 2)
                    {
                        return BadRequest("Text content must be at least 2 characters long.");
                    }
                
                if (answer.id == 0)
                    {
                        string insertAnswerQuery = "INSERT INTO AnswerList (textContent, imageContentWithoutText, IsCorrect, questionId) VALUES (@TextContent, @Image, @IsCorrect, @QuestionId)";
                        await _db.SaveDataAsync(insertAnswerQuery, new
                        {
                            TextContent = answer.textContent,
                            Image = answer.imageContentWithoutText,
                            IsCorrect = answer.IsCorrect,
                            QuestionId = questionId
                        });
                    }
                    else // If the answer has a valid ID, update it
                    {
                        string updateAnswerQuery = "UPDATE AnswerList SET textContent = @TextContent, imageContentWithoutText = @Image, IsCorrect = @IsCorrect WHERE ID = @ID";
                        await _db.SaveDataAsync(updateAnswerQuery, new
                        {
                            TextContent = answer.textContent,
                            Image = answer.imageContentWithoutText,
                            IsCorrect = answer.IsCorrect,
                            ID = answer.id
                        });
                    }
                }
                return Ok(updatedQuestion);
            }
            return Unauthorized("User is not authorized.");


        }
         
        

        [HttpDelete("{questionId}")]
        public async Task<IActionResult> DeleteQuestion(int questionId)
        {
            string deleteAnswerQuery = "DELETE FROM AnswerList WHERE QuestionId = @QuestionId";
            await _db.SaveDataAsync(deleteAnswerQuery, new { QuestionId = questionId });

            string deleteQuestionQuery = "DELETE FROM questionList WHERE ID = @ID";
            await _db.SaveDataAsync(deleteQuestionQuery, new { ID = questionId });

            return NoContent();
        }

        [HttpDelete("answers/{answerId}")]
        public async Task<IActionResult> DeleteAnswer(int authUserId, int answerId)
        {
            if (authUserId > 0)
            {
                string findQuestionIdQuery = "SELECT questionId FROM AnswerList WHERE ID = @answerId";
                var questionId = await _db.ExecuteScalarAsync<int>(findQuestionIdQuery, new { answerId });

                if (questionId == 0)
                {
                    return NotFound("Answer not found.");
                }

                // If there are more than 2 answers, proceed with deletion
                string deleteAnswerQuery = "DELETE FROM AnswerList WHERE ID = @answerId";
                await _db.SaveDataAsync(deleteAnswerQuery, new { answerId });

                return Ok("Answer has been deleted");
            }
            return Unauthorized("User is not authorized.");
        }

        [HttpPost("updateImagesQuestion/{id}")]
        public async Task<IActionResult> UpdateImages(int id, [FromBody] string ImgName)
        {
            object param = new
            {
                ID = id,
                ImgName = ImgName
            };

            // Query to update the image name in the database
            string query = "UPDATE questionList SET image=@ImgName WHERE ID=@ID";
            int isUpdate = await _db.SaveDataAsync(query, param); // Execute the query

            if (isUpdate > 0) // Check if the update was successful
                return Ok(); // If the update was successful, return a successful response
            else
                return BadRequest("Update failed"); // If the update failed, return an error
        }

        [HttpPost("updateImageAnswer/{id}")]
        public async Task<IActionResult> UpdateImagesAnswer(int id, [FromBody] string ImgName)
        {
            object param = new
            {
                ID = id,
                ImgName = ImgName
            };

            // Query to update the image name in the database
            string query = "UPDATE AnswerList SET imageContentWithoutText=@ImgName WHERE ID=@ID";
            int isUpdate = await _db.SaveDataAsync(query, param); // Execute the query

            if (isUpdate > 0) // Check if the update was successful
                return Ok(); // If the update was successful, return a successful response
            else
                return BadRequest("Update failed"); // If the update failed, return an error
        }

        [HttpPost("deleteImageReference/{answerId}")]
        public async Task<IActionResult> DeleteImageReference(int authUserId, int answerId, [FromBody] dynamic data)
        {
            if (authUserId > 0)
            {

            
            string imagePath = data.ImagePath;
            if (string.IsNullOrEmpty(imagePath))
            {
                return BadRequest("Invalid image path.");
            }

            string query = "UPDATE AnswerList SET imageContentWithoutText = NULL WHERE ID = @Id";
            int rowsAffected = await _db.SaveDataAsync(query, new { Id = answerId });

            if (rowsAffected > 0)
            {
                return Ok("Image reference deleted successfully.");
            }
            else
            {
                return BadRequest("Failed to delete image reference.");
            }
            }
            return Unauthorized("User is not authorized.");
        }

    }
}
