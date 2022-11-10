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
    [Route("/api")]
    [ApiController]
    public class IndexController : Controller
    {
        private readonly IConfiguration _iconfiguration;

        public IndexController(IConfiguration configuration)
        {
            _iconfiguration = configuration;
        }

        [HttpGet]
        public string Get()
        {
            return "Music App Server";
        }

        [HttpGet("index")]
        public JsonResult GetHomePage([FromHeader] string auth)
        {
            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();
            try
            {
                JToken user = JToken.Parse(JsonConvert.DeserializeObject(auth).ToString() ?? "{}");
                string username = (string)user.SelectToken("username");
                string userId = (string)user.SelectToken("id");
                if ((user != null && userId != null && username != null) && UserController.checkUserExisted(username,userId, conn))
                {
                    string sqlSelectCoreMusic = @$"
                    SELECT 
                    m.id, m.created_at as upload_time, m.title, 
                    m.artist_id, m.artist_name, 
                    m.thumbnail as music_thumbnail, 
                    a.thumbnail as artist_thumbnail, m.audio, m.slug as music_slug, 
                    a.slug as artist_slug, 
                    m.viewcount, 
                    l.lyrics FROM music m 
                    INNER JOIN artist a oN m.artist_id = a.id
                    INNER JOIN lyrictable l ON l.songid = m.id 
                    UNION SELECT m.id, m.created_at as upload_time, 
                    m.title, m.artist_id, m.artist_name, 
                    m.thumbnail as music_thumbnail, NULL As artist_thumbnail, 
                    m.audio, m.slug as music_slug, NULL as artist_slug, 
                    m.viewcount,
                    l.lyrics FROM music m INNER JOIN lyrictable l ON m.id = l.songid 
                    WHERE m.artist_id IS NULL";
                    string sqlSelectLikedUser = @$"SELECT * FROM liketable WHERE userid='{userId}'";
                    DataTable likedUserTable = new DataTable();
                    SqlCommand likedUserCmd = new SqlCommand(sqlSelectLikedUser, conn);
                    SqlDataReader likedUserReader = likedUserCmd.ExecuteReader();
                    likedUserTable.Load(likedUserReader);
                    likedUserReader.Close();

                    DataTable coreMusicsTable = new DataTable();
                    SqlCommand coreMusicsCmd = new SqlCommand(sqlSelectCoreMusic, conn);
                    SqlDataReader coreMusicsReader = coreMusicsCmd.ExecuteReader();
                    coreMusicsTable.Load(coreMusicsReader);
                    coreMusicsReader.Close();
                    JToken musics = JToken.FromObject(coreMusicsTable);
                    
                    JToken[] newResultCoreMusic  = musics.AsEnumerable().Select(music =>
                    {

                        if (likedUserTable.AsEnumerable().Where(likedMusic =>  Convert.ToString(likedMusic["songid"]) == Convert.ToString(music["id"])).Any())
                        {
                            
                            music["liked"] = true;
                            return music;
                        }
                        else
                        {
                            return music;
                        }
                    }).ToArray();
                    
                    return new JsonResult(new { musics = newResultCoreMusic });
                }
                else
                {
                    string sqlSelectCoreMusic = @$"
                    SELECT m.id, m.created_at as upload_time, m.title, m.artist_id, m.artist_name, 
                    m.thumbnail as music_thumbnail, a.thumbnail as artist_thumbnail ,
                    m.audio, m.slug as music_slug, a.slug as artist_slug, m.viewcount, 
                    l.lyrics FROM music m 
                    INNER JOIN artist a oN m.artist_id = a.id 
                    INNER JOIN lyrictable l ON l.songid = m.id 
                    UNION SELECT m.id, m.created_at as upload_time, m.title, 
                    m.artist_id, m.artist_name, m.thumbnail as music_thumbnail, NULL as artist_thumbnail, 
                    m.audio, m.slug as music_slug, NULL as artist_slug, 
                    m.viewcount, l.lyrics FROM music m INNER JOIN 
                    lyrictable l ON m.id = l.songid WHERE m.artist_id IS NULL";

                    DataTable coreMusicsTable = new DataTable();
                    SqlCommand coreMusicsCmd = new SqlCommand(sqlSelectCoreMusic, conn);
                    SqlDataReader coreMusicsReader = coreMusicsCmd.ExecuteReader();
                    coreMusicsTable.Load(coreMusicsReader);
                    coreMusicsReader.Close();

                    return new JsonResult(new { musics = coreMusicsTable });
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
