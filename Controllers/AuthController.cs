using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace MusicAppAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly IConfiguration _iconfiguration;

        public AuthController(IConfiguration configuration)
        {
            _iconfiguration = configuration;
        }
        
        [HttpPost("login")]
        public JsonResult Login([FromBody] dynamic reqBody)
        {
            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();
            try
            {
                string username = (string)reqBody.username, password = (string)reqBody.password;
                if (username != null && password != null)
                {
                    string sqlCheckPassword = @$"SELECT * FROM UserTable WHERE username = '{username}'";
                    DataTable sqlCheckPasswordTable = new DataTable();
                    SqlCommand sqlCheckPasswordCmd = new SqlCommand(sqlCheckPassword, conn);
                    SqlDataReader sqlCheckPasswordReader = sqlCheckPasswordCmd.ExecuteReader();
                    sqlCheckPasswordTable.Load(sqlCheckPasswordReader);

                    if(sqlCheckPasswordTable.Rows.Count == 0)
                    {
                        return new JsonResult(new { error = new { message = "Incorrect username." } });
                    }
                    else
                    {
                        JToken sqlCheckPasswordRecord = JToken.FromObject(sqlCheckPasswordTable)[0];
                        string hashedPassword = (string)sqlCheckPasswordRecord.SelectToken("password");

                        if(BCrypt.Net.BCrypt.Verify(password, hashedPassword))
                        {
                            string userId = (string)sqlCheckPasswordRecord.SelectToken("id");
                            string sqlSelectUser = @$"SELECT id,avatar,displayedName as nickname, username, email, role FROM UserTable WHERE id='{userId}'";
                            DataTable sqlSelectUserTable = new DataTable();
                            SqlCommand sqlSelectUserCmd = new SqlCommand(sqlSelectUser, conn);
                            SqlDataReader sqlSelectUserReader = sqlSelectUserCmd.ExecuteReader();
                            sqlSelectUserTable.Load(sqlSelectUserReader);
                            return new JsonResult(new { user = JToken.FromObject(sqlSelectUserTable)[0], message = "Login success!" });
                        }
                        else
                        {
                            return new JsonResult(new { error = new { message = "Incorrect password." } });
                        }

                        
                    }
                }
                else
                {
                    return new JsonResult(new { error = new { message = "Incorrect password." } });
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

        [HttpGet("loginstatus")]
        public JsonResult LoginStatusChecker([FromHeader] string auth)
        {

            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();
            try
            {
                
                JToken user = JToken.Parse(JsonConvert.DeserializeObject(auth).ToString() ?? "{}");

                string userId = (string)user.SelectToken("id");
                if (user != null && userId != null)
                {
                    
                    string sqlSelectUser = @$"SELECT id,avatar,displayedName as nickname, username, email, role FROM UserTable WHERE id='{userId}'";
                    DataTable sqlSelectUserTable = new DataTable();
                    SqlCommand sqlSelectUserCmd = new SqlCommand(sqlSelectUser, conn);
                    SqlDataReader sqlSelectUserReader = sqlSelectUserCmd.ExecuteReader();
                    sqlSelectUserTable.Load(sqlSelectUserReader);
                    JToken sqlSelectUserRecord = JToken.FromObject(sqlSelectUserTable)[0];
                    sqlSelectUserRecord["isLogin"] = true;
                    return new JsonResult(sqlSelectUserRecord);
                }
                else
                {
                    return new JsonResult(new { isLogin = false });
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

        


        [HttpPost("signup")]
        public async Task<JsonResult> SignUp([FromBody] dynamic reqBody)
        {
            
            string username = reqBody.username, email = reqBody.email,password = (string)reqBody.password;
            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();
            try
            {
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

                if (reqBody.isAdminSignup != null)
                {
                    return new JsonResult(new { });
                }
                else
                {
                    string sqlInsertUser = @$"INSERT INTO UserTable(username, email, password) VALUES('{username}','{email}','{hashedPassword}')";
                    JToken checkExistAccountResponse = JToken.FromObject(Program.CheckAccountExisted(username, email, "user", conn));
                    if ((Object)checkExistAccountResponse.SelectToken("error") != null)
                    {
                        return new JsonResult(checkExistAccountResponse);
                    }
                    else
                    {
                        SqlCommand sqlInsertUserCmd = new SqlCommand(sqlInsertUser, conn);
                        sqlInsertUserCmd.ExecuteNonQuery();

                        return new JsonResult(new { username = username, message = "Sign up successful!" });
                    }
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
