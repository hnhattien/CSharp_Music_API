using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BCrypt.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace MusicAppAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IConfiguration _iconfiguration;

        public UserController(IConfiguration configuration)
        {
            _iconfiguration = configuration;
        }
        public static bool checkUserExisted(string username, string userId, SqlConnection conn)
        {
            if(conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            string sqlCheckUser = $@"SELECT * FROM UserTable WHERE username='{username}' AND id='{userId}'";
            DataTable checkUserTable = new DataTable();
            SqlCommand checkUserCmd = new SqlCommand(sqlCheckUser, conn);
            SqlDataReader checkUserReader = checkUserCmd.ExecuteReader();
            checkUserTable.Load(checkUserReader);
            if(checkUserTable.Rows.Count <= 0)
            {
                return false;
            }
            else
            {
                return true;
            }
            
        }

        [HttpPost("uploadavatar")]
        public JsonResult UploadAvatar([FromBody] dynamic reqBody, [FromHeader] string auth)
        {
            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();
            
                JToken user = JToken.Parse(JsonConvert.DeserializeObject(auth).ToString() ?? "{}");
                string username = (string)user.SelectToken("username");
                string userId = (string)user.SelectToken("id");
                if ((user != null && userId != null && username != null) && UserController.checkUserExisted(username, userId, conn))
                {

                    string base64 = reqBody.base64Image;
                    string filename = reqBody.filename;

                    string rawDataBase64 = Convert.ToString(base64).Split(";base64,").Last();

                    System.IO.File.WriteAllBytes($"wwwroot/upload/images/{filename}", Convert.FromBase64String(rawDataBase64));

                    string sqlUpdateAvatar = @$"UPDATE UserTable SET avatar=N'{filename}' WHERE id='{userId}'";
                    SqlCommand sqlUpdateAvatarCmd = new SqlCommand(sqlUpdateAvatar, conn);
                    sqlUpdateAvatarCmd.ExecuteNonQuery();
                    return new JsonResult(new { message = "Update avatar successfull!" });
                }
                else
                {
                    return new JsonResult(
                        new { isRequireLogin = true, error = new { message = "You must to login to perform this action." } }
                        );
                }
            
        }

        [HttpPost("changenickname")]
        public JsonResult ChangeNickname([FromBody] dynamic reqBody, [FromHeader] string auth)
        {
            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();
            try
            {
                JToken user = JToken.Parse(JsonConvert.DeserializeObject(auth).ToString() ?? "{}");
                string username = (string)user.SelectToken("username");
                string userId = (string)user.SelectToken("id");
                if ((user != null && userId != null && username != null) && UserController.checkUserExisted(username, userId, conn))
                {

                    string newNickname = reqBody.newNickname;
                    

                    string sqlUpdateNickname = @$"UPDATE UserTable SET displayedName=N'{newNickname}'";
                    SqlCommand sqlUpdateNicknameCmd = new SqlCommand(sqlUpdateNickname, conn);
                    sqlUpdateNicknameCmd.ExecuteNonQuery();
                    return new JsonResult(new { message = "Update nickname successfull!" });
                }
                else
                {
                    return new JsonResult(
                        new { isRequireLogin = true, error = new { message = "You must to login to perform this action." } }
                        );
                }
            }
            catch (Exception err)
            {

                return new JsonResult(new { error = new { message = Convert.ToString(err) } });
            }
            finally
            {

                conn.Close();
            }
        }

        [HttpGet("likedmusics")]
        public JsonResult LikedMusics([FromHeader] string auth)
        {
            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();
            try
            {
                JToken user = JToken.Parse(JsonConvert.DeserializeObject(auth).ToString() ?? "{}");
                string username = (string)user.SelectToken("username");
                string userId = (string)user.SelectToken("id");
                
                if ((user != null && userId != null && username != null) && UserController.checkUserExisted(username, userId, conn))
                {
                    string sqlSelectLikedMusics = @$"
                    SELECT 
                    m.id,l.id as like_id, 
                    m.title, m.artist_name, 
                    m.thumbnail as music_thumbnail, 
                    m.audio, m.slug as music_slug, 
                    a.slug as artist_slug from music m 
                    INNER JOIN artist a ON m.artist_id = a.id 
                    INNER JOIN liketable l ON l.songid = m.id 
                    WHERE l.userid ='{userId}'
                    UNION 
                    SELECT m.id, l.id as like_id, m.title, m.artist_name, 
                    m.thumbnail as music_thumbnail, m.audio, m.slug as music_slug, 
                    NULL as artist_slug from music m 
                    INNER JOIN liketable l ON l.songid = m.id 
                    WHERE m.artist_id IS NULL AND l.userid ='{userId}' ORDER BY like_id DESC";
                    DataTable sqlLikedMusicsTable = new DataTable();
                    SqlCommand sqlSelectLikedMusicsCmd = new SqlCommand(sqlSelectLikedMusics, conn);
                    SqlDataReader sqlSelectLikedMusicsReader = sqlSelectLikedMusicsCmd.ExecuteReader();
                    sqlLikedMusicsTable.Load(sqlSelectLikedMusicsReader);

                    JToken musics = JToken.FromObject(sqlLikedMusicsTable);

                    JToken[] newResultMusic = musics.AsEnumerable().Select(music =>
                    {
                        music["liked"] = true;
                        return music;  
                    }).ToArray();
                    return new JsonResult(newResultMusic);
                }
                else
                {
                    return new JsonResult(
                        new { isRequireLogin = true, error = new { message = "You must to login to perform this action." } }
                        );
                }
            }
            catch (Exception err)
            {

                return new JsonResult(new { error = new { message = Convert.ToString(err) } });
            }
            finally
            {

                conn.Close();
            }
        }

        [HttpGet("{id}")]
        public JsonResult GetUserById(string id, [FromHeader] string auth)
        {
            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();
            try
            {
                JToken user = JToken.Parse(JsonConvert.DeserializeObject(auth).ToString() ?? "{}");
                string username = (string)user.SelectToken("username");
                string userId = (string)user.SelectToken("id");
                if ((user != null && userId != null && username != null) && UserController.checkUserExisted(username, userId, conn))
                {
                    if(Convert.ToString(id) == Convert.ToString(userId))
                    {
                        string sqlSelectUser = @$"SELECT username, email FROM UserTable WHERE id='{id}'";

                        DataTable sqlSelectUserTable = new DataTable();
                        SqlCommand sqlSelectUserCmd = new SqlCommand(sqlSelectUser, conn);
                        SqlDataReader sqlSelectUserReader = sqlSelectUserCmd.ExecuteReader();
                        sqlSelectUserTable.Load(sqlSelectUserReader);

                        JToken selectedUser = Program.GetFirstRowFromDataTableAsJson(sqlSelectUserTable);

                        return new JsonResult(selectedUser);
                    }
                    else
                    {
                        return new JsonResult(new { error = new { message = "You cannot see user info who is not you" } });
                    }
                }
                else
                {
                    return new JsonResult(new { error = new { message = "You have not authorized yet" } });
                }
            }
            catch (Exception err)
            {

                return new JsonResult(new { error = new { message = Convert.ToString(err) } });
            }
            finally
            {

                conn.Close();
            }

        }

        [HttpPost("changepassword")]
        public JsonResult ChangePassword([FromBody] dynamic reqBody, [FromHeader] string auth)
        {
            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();
            try
            {
                JToken user = JToken.Parse(JsonConvert.DeserializeObject(auth).ToString() ?? "{}");
                string username = (string)user.SelectToken("username");
                string userId = (string)user.SelectToken("id");

                if (reqBody.selector != null)
                {
                    string selector = reqBody.selector;
                    string sqlSelectEmail = @$"SELECT useremail FROM resetpassword WHERE selector = '{selector}'";
                    DataTable sqlSelectEmailTable = new DataTable();
                    SqlCommand sqlSelectEmailCmd = new SqlCommand(sqlSelectEmail, conn);
                    SqlDataReader sqlSelectEmailReader = sqlSelectEmailCmd.ExecuteReader();
                    sqlSelectEmailTable.Load(sqlSelectEmailReader);
                    sqlSelectEmailReader.Close();
                    if (sqlSelectEmailTable.Rows.Count <= 0)
                    {
                        return new JsonResult(new { error = new { message = "You not permit to perform this action." } });
                    }
                    else
                    {
                        string userPassword = reqBody.password;
                        
                        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(userPassword);
                        JToken selectorRecord = Program.GetFirstRowFromDataTableAsJson(sqlSelectEmailTable);
                        string useremail = (string)selectorRecord.SelectToken("useremail");
                        string sqlUpdatePassword = @$"UPDATE UserTable SET password ='{hashedPassword}' WHERE email ='{useremail}' AND password IS NOT NULL";

                        SqlCommand sqlChangePasswordCmd = new SqlCommand(sqlUpdatePassword, conn);
                        sqlChangePasswordCmd.ExecuteNonQuery();

                        return new JsonResult(new { message = "Update password successfull." });
                    }
                }
                else if ((user != null && userId != null && username != null) && UserController.checkUserExisted(username, userId, conn))
                {
                    string sqlSelectCheckPassword = @$"SELECT password FROM UserTable WHERE id ='{userId}'";
                    DataTable sqlSelectCheckPasswordTable = new DataTable();
                    SqlCommand sqlSelectCheckPasswordCmd = new SqlCommand(sqlSelectCheckPassword, conn);
                    SqlDataReader sqlSelectCheckPasswordReader = sqlSelectCheckPasswordCmd.ExecuteReader();
                    sqlSelectCheckPasswordTable.Load(sqlSelectCheckPasswordReader);
                    sqlSelectCheckPasswordReader.Close();
                    JToken userPasswordRecord = JToken.FromObject(sqlSelectCheckPasswordTable)[0];
                    string oldPaswword = (string)userPasswordRecord.SelectToken("password");
                    if (BCrypt.Net.BCrypt.Verify(reqBody.currentpassword, oldPaswword))
                    {
                        string newPassword = BCrypt.Net.BCrypt.HashPassword(reqBody.newpassword);
                        string sqlUpdatePassword = @$"UPDATE user SET password='{newPassword}' WHERE id='{userId}'";
                        SqlCommand sqlChangePasswordCmd = new SqlCommand(sqlUpdatePassword, conn);
                        sqlChangePasswordCmd.ExecuteNonQuery();
                        
                        return new JsonResult(new { message = "Update password successfull." });
                    }
                    else
                    {
                        return new JsonResult(new { error = new { message = "Current password is not correct!" } });
                    }
                    
                }
                else
                {
                    return new JsonResult(new { error = new { message = "You not permit to perform this action." } });
                }
            }
            catch (Exception err)
            {

                return new JsonResult(new { error = new { message = Convert.ToString(err) } });
            }
            finally
            {

                conn.Close();
            }
        }

        [HttpPost("resetpassword")]
        public async Task<JsonResult> ResetPassword([FromBody] dynamic reqBody)
        {
            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();
            try
            {
                string username = reqBody.username;
                if (username != null)
                {
                    string sqlSelectEmail = @$"SELECT email FROM UserTable WHERE (username ='{username}' OR email ='{username}') AND password IS NOT NULL";
                    DataTable sqlSelectEmailTable = new DataTable();
                    SqlCommand sqlSelectEmailCmd = new SqlCommand(sqlSelectEmail, conn);
                    SqlDataReader sqlSelectEmailReader = sqlSelectEmailCmd.ExecuteReader();
                    sqlSelectEmailTable.Load(sqlSelectEmailReader);
                    sqlSelectEmailReader.Close();
                    JToken selectEmailRecord = JToken.FromObject(sqlSelectEmailTable)[0];
                    if (sqlSelectEmailTable.Rows.Count == 0)
                    {
                        return new JsonResult(new { error = new { message = "No any user with username or email you provide." } });
                    }
                    else
                    {
                        string userEmail = (string)selectEmailRecord.SelectToken("email");
                        string token = Program.RandomBytesHexString(30);
                        string selector = Program.RandomBytesHexString(9);
                        Int64 expires = DateTime.Now.Millisecond + 600000;

                        string hashToken = BCrypt.Net.BCrypt.HashPassword(token);

                        string sqlInsertPassword = @$"INSERT resetpassword(selector, useremail, token, expires)VALUES('{selector}','{userEmail}','{hashToken}','{expires}')";

                        SqlCommand sqlInsertPasswordCmd = new SqlCommand(sqlInsertPassword, conn);
                        sqlInsertPasswordCmd.ExecuteNonQuery();

                        JToken reqEmail = JToken.FromObject(new { });
                        reqEmail["toName"] = "User";
                        reqEmail["toEmail"] = userEmail;
                        reqEmail["subject"] = "Yêu cầu lấy lại mật khẩu - Music App Đồ án Thương Mại Điện Tử";
                        reqEmail["hostname"] = "localhost";
                        reqEmail["selector"] = selector;
                        reqEmail["token"] = token;
                        await Program.sendEmail(reqEmail);

                        return new JsonResult(new { message = "Check email for reset password." });

                    }
                }
                else
                {
                    return new JsonResult(new { error = new { message = "No any user with username or email you provide." } });
                }
            }
            catch (Exception err)
            {

                return new JsonResult(new { error = new { message = Convert.ToString(err) } });
            }
            finally
            {

                conn.Close();
            }
        }

        [HttpPost("authresetpassword")]
        public JsonResult AuthResetPassword([FromBody] dynamic reqBody)
        {
            string sel = reqBody.sel;
            string userToken = reqBody.token;
            string hexRegex = "/^[0-9a-fA-F]+$/";
            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();
            try
            {


                if (Regex.IsMatch(sel, hexRegex) && Regex.IsMatch(userToken, hexRegex))
                {
                    string sqlSelectSelector = @$"SELECT * FROM resetpassword WHERE selector ='{sel}' AND expires > '{DateTime.Now.Millisecond}'";
                    DataTable sqlSelectSelectorTable = new DataTable();
                    SqlCommand sqlSelectSelectorCmd = new SqlCommand(sqlSelectSelector, conn);
                    SqlDataReader sqlSelectSelectorReader = sqlSelectSelectorCmd.ExecuteReader();

                    sqlSelectSelectorTable.Load(sqlSelectSelectorReader);

                    if (sqlSelectSelectorTable.Rows.Count == 0)
                    {
                        return new JsonResult(new { error = new { message = "Token was expires!" } });
                    }
                    else
                    {
                        JToken selectorRecord = JToken.FromObject(sqlSelectSelectorTable)[0];
                        string hashedUserToken = (string)selectorRecord.SelectToken("token");

                        if (BCrypt.Net.BCrypt.Verify(userToken, hashedUserToken))
                        {
                            return new JsonResult(new { isAuth = true, message = "Request Reset password success!" });
                        }
                        else
                        {
                            return new JsonResult(new { error = new { message = "Token is invalid." } });
                        }
                    }

                }
                else
                {
                    return new JsonResult(new { error = new { message = "Token is invalid." } });
                }
            }
            catch (Exception err)
            {

                return new JsonResult(new { error = new { message = Convert.ToString(err) } });
            }
            finally
            {

                conn.Close();
            }
        }
    }
}
