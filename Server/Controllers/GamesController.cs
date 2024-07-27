using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using template.Server.Data;
using template.Server.Helpers;
using template.Shared.Models.Classes;

namespace template.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ServiceFilter(typeof(AuthCheck))]
    public class GamesController : ControllerBase
    {
        private readonly DbRepository _db;

        public GamesController(DbRepository db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserGames(int authUserId)
        {
            if (authUserId > 0)
            {
                object param = new
                {
                    UserId = authUserId
                };

                string gameQuery = "SELECT ID, GameName, GameCode, isPublished, CanPublish, questionTime FROM Games WHERE UserId = @UserId ORDER BY GameCode DESC";
                var gamesRecords = await _db.GetRecordsAsync<GamesTable>(gameQuery, param);

                if (gamesRecords == null || !gamesRecords.Any())
                {
                    return NotFound("No games found for this user.");
                }

                List<GamesTable> GamesList = gamesRecords.ToList();

                if (GamesList.Count > 0)
                {
                    return Ok(GamesList);
                }
                else
                {
                    return NotFound("No games for this user.");
                }
            }
            else
            {
                return Unauthorized("User is not authenticated");
            }
        }

        [HttpPost("addGame")]
        public async Task<IActionResult> AddGames(int authUserId, NewGameDTO gameToAdd)
        {
            if (authUserId > 0)
            {
                object newGameParam = new
                {
                    GameName = gameToAdd.GameName,
                    GameCode = 0,
                    isPublished = false,
                    questionTime = 30,
                    UserId = authUserId,
                    CanPublish = false
                };
                string insertGameQuery = "INSERT INTO Games (GameName, GameCode, isPublished, questionTime, UserId, CanPublish) " +
                        "VALUES (@GameName, @GameCode, @isPublished, @questionTime, @UserId, @CanPublish)";
                int newGameId = await _db.InsertReturnIdAsync(insertGameQuery, newGameParam);
                if (newGameId != 0)
                {
                    //אם המשחק נוצר בהצלחה, נחשב את הקוד עבורו
                    int gameCode = newGameId + 1000;
                    object updateParam = new
                    {
                        ID = newGameId,
                        GameCode = gameCode
                    };
                    string updateCodeQuery = "UPDATE Games SET GameCode = @GameCode WHERE ID=@ID";
                    int isUpdate = await _db.SaveDataAsync(updateCodeQuery, updateParam);
                    if (isUpdate > 0)
                    {
                        //אם המשחק עודכן בהצלחה - נחזיר את הפרטים שלו לעורך
                        object param2 = new
                        {
                            ID = newGameId
                        };
                        string gameQuery = "SELECT ID, GameName, GameCode, isPublished, CanPublish FROM Games WHERE ID = @ID";
                        var gameRecord = await _db.GetRecordsAsync<GamesTable>(gameQuery, param2);
                        GamesTable newGame = gameRecord.FirstOrDefault();
                        return Ok(newGame);
                    }
                    return BadRequest("Game code not created");
                }
                return BadRequest("Game not created");
            }
            else
            {
                return Unauthorized("user is not authenticated");
            }
        }
        [HttpDelete("delete/{gameId}")]
        public async Task<IActionResult> DeleteGame(int authUserId, int gameId)
        {
            if (authUserId > 0)
            {
                object param = new
                {
                    GameId = gameId,
                    UserId = authUserId
                };

                string deleteQuery = "DELETE FROM Games WHERE ID = @GameId AND UserId = @UserId";
                int rowsAffected = await _db.SaveDataAsync(deleteQuery, param);

                if (rowsAffected > 0)
                {
                    return Ok("Game deleted successfully.");
                }
                else
                {
                    return NotFound("Game not found or user unauthorized to delete this game.");
                }
            }
            else
            {
                return Unauthorized("User is not authenticated");
            }
        }


        //[HttpPost("change/{id}")]
        //public async Task<IActionResult> EditBool(int id, CanPublishDTO canPublishDTO)
        //{
        //    // Check if the provided ID is valid.
        //    if (id <= 0)
        //    {
        //        // Return a BadRequest response if the ID is not valid.
        //        return BadRequest("Invalid ID");
        //    }

        //    // Retrieve the game from the database to check CanPublish value
        //    var game = await _db.GetRecordsAsync<GamesTable>("SELECT CanPublish FROM Games WHERE ID = @ID", new { ID = id });

        //    if (game.Count() == 0 || !game.First().CanPublish)
        //    {
        //        // Return a BadRequest response if CanPublish is false or the game is not found.
        //        return BadRequest("CanPublish must be true to update isPublished");
        //    }

        //    string query = "UPDATE Games SET isPublished = @IsPublished WHERE ID = @ID";
        //    int isUpdateSuccessful = await _db.SaveDataAsync(query, new { ID = id, IsPublished = !canPublishDTO.isPublished });
        //    if (isUpdateSuccessful > 0)
        //    {
        //        return Ok("Update Successful");
        //    }
        //    else
        //    {
        //        return NotFound("Game not found or user unauthorized to edit this game.");
        //    }
        //}


        [HttpPost("publishGame")]
        public async Task<IActionResult> publishGame(int authUserId, PublishGame game)
        {
            object param = new
            {
                UserId = authUserId,
                gameID = game.ID
            };
            //שליפת שם המשחק לפי משתמש כדי לוודא שהמשחק המבוקש שייך למשתמש שמחובר
            string checkQuery = "SELECT GameName FROM Games WHERE UserId = @UserId and ID=@gameID";
            var checkRecords = await _db.GetRecordsAsync<string>(checkQuery, param);
            string gameName = checkRecords.FirstOrDefault();
            //שליפת שם המשחק כדי לוודא שהמשחק המבוקש שייך למשתמש המחובר
            if (gameName != null)
            {
                if (game.isPublished == true)
                {
                    //נבדוק באמצעות פונקציית עזר שניתן לפרסם אותו
                    bool canPublish = await CanPublishFunc(game.ID);
                    //במידה ולא ניתן לפרסם	
                    if (canPublish == false)
                    {
                        //נחזיר הודעת שגיאה	
                        return BadRequest("This game cannot be published");
                    }
                }
                string updateQuery = "UPDATE Games SET isPublished=@isPublished WHERE ID=@ID";
                int isUpdate = await _db.SaveDataAsync(updateQuery, game);
                if (isUpdate == 1)
                {
                    return Ok("Game is published");
                }
                return BadRequest("Update Failed");
            }
            return BadRequest("It's Not Your Game");
            //במידה ויש רצון לפרסם את המשחק

            //המשך הקוד כאן - מחוץ לתנאי הראשון. אם רוצים להסיר פרסום, לא צריך לבדוק את הרשאת הפרסום

        }
        [HttpPost("checkPublish/{id}")]
        public async Task<IActionResult> CheckIfPublished(int authUserId, int id, GamesTable game)
        {
            if (authUserId <= 0)
            {
                return Unauthorized("User is not authorized");
            }

            object param = new
            {
                UserId = authUserId,
                gameID = game.ID
            };
            //שליפת שם המשחק לפי משתמש כדי לוודא שהמשחק המבוקש שייך למשתמש שמחובר
            string checkQuery = "SELECT GameName FROM Games WHERE UserId = @UserId and ID=@gameID";
            var checkRecords = await _db.GetRecordsAsync<string>(checkQuery, param);
            string gameName = checkRecords.FirstOrDefault();
            //שליפת שם המשחק כדי לוודא שהמשחק המבוקש שייך למשתמש המחובר
            if (gameName != null)
            {
                if (game.isPublished == false)
                {
                    //נבדוק באמצעות פונקציית עזר שניתן לפרסם אותו
                    bool canPublish = await CanPublishFunc(game.ID);
                    if (canPublish == false)
                    {
                        return BadRequest("You can't publish the game yet.");
                    }

                    object paramCanPublish = new
                    {
                        ID = game.ID,
                        CanPublish = canPublish
                    };
                    string updateQuery = "UPDATE Games SET CanPublish=@CanPublish WHERE ID=@ID";
                    int isUpdate = await _db.SaveDataAsync(updateQuery, new { CanPublish = canPublish, ID = id });
                    if (isUpdate == 1)
                    {
                        return Ok("Game can be published now.");
                    }
                    //במידה ולא ניתן לפרסם	
                    if (canPublish == false)
                    {
                        //נחזיר הודעת שגיאה	
                        return BadRequest("This game cannot be published");
                    }
                }

            }
            return BadRequest("Game is already published.");
        }


        [HttpPut("settings/{id}")]
        public async Task<IActionResult> EditGameSettings(int authUserId, int id, EditGameDTO gameToEdit)
        {
            if (authUserId > 0)
            {
                object param = new
                {

                    ID = id,
                    GameName = gameToEdit.GameName,
                    questionTime = gameToEdit.questionTime
                };

                string updateGameQuery = "UPDATE Games SET GameName = @GameName, questionTime = @questionTime WHERE ID = @ID";
                int isUpdateSuccesful = await _db.SaveDataAsync(updateGameQuery, param);
                if (isUpdateSuccesful > 0)
                {
                    return Ok("Game was updated.");

                }
                else
                {
                    return BadRequest("Could not update game.");
                }
            }
            else
            {
                Unauthorized("User is unauothrized.");
            }
            return BadRequest();
        }

        //שיטה שבודקת אם ניתן לפרסם את המשחק
        //אם נמצא שלא ניתן לפרסם - נוודא שהמשחק גם לא מפורסם
        private async Task<bool> CanPublishFunc(int gameId)
        {
            //במקרה שלנו - התנאי לפרסום משחק הוא לפחות שלוש שאלות
            //יש לשנות את השיטה בהתאם לתנאי הפרסום עליהם החלטתם
            int minQuestions = 10;

            //משתנה לשמירה של הסטטוס - האם ניתן לפרסום
            bool canPublish = false;

            object param = new
            {
                ID = gameId
            };

            //שאילתה שבודקת כמה שאלות יש במשחק
            string queryQuestionCount = "SELECT Count(ID) from questionList WHERE GameId=@ID";
            var recordQuestionCount = await _db.GetRecordsAsync<int>(queryQuestionCount, param);
            int numberOfQuestions = recordQuestionCount.FirstOrDefault();

            //נשמור משתנה ריק שיכיל את שאילתת העדכון בהתאם למספר השאלות
            string updateQuery;

            //אם יש מספיק שאלות במשחק
            if (numberOfQuestions >= minQuestions)
            {
                //נשנה את הסטטוס של האם ניתן לפרסום	
                canPublish = true;
                //נעדכן את השאילתה – אם המשחק מורשה לפרסום, לא נשנה את מצב הפרסום בפועל
                updateQuery = "UPDATE Games SET CanPublish=true WHERE ID=@ID";
            }
            //אם אין מספיק שאלות
            else
            {
                //נעדכן את השאילתה כך שגם האם ניתן לפרסם וגם האם מפורסם שליליים
                updateQuery = "UPDATE Games SET isPublished=false, CanPublish=false WHERE ID=@ID";
            }

            //נעדכן את בסיס הנתונים
            int isUpdate = await _db.SaveDataAsync(updateQuery, param);
            //נחזיר משתנה בוליאני שאומר אם ניתן לפרסם את המשחק או לא
            return canPublish;

            //סוף שיטת הקונטרולר

        }
    }
}