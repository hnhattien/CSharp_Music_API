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
    public class SearchController : Controller
    {
        private readonly IConfiguration _iconfiguration;

        public SearchController(IConfiguration configuration)
        {
            _iconfiguration = configuration;
        }

        [HttpGet("{target}")]
        public JsonResult Get(string target, [FromHeader] string auth)
        {
            string sqlSelectMusic = $@"
            SELECT 
            m.id, 
            m.created_at as upload_time, m.title, m.artist_id, m.artist_name, 
            m.thumbnail as music_thumbnail, a.thumbnail as artist_thumbnail, 
            m.audio, m.slug as music_slug, 
            a.slug as artist_slug, m.viewcount, l.lyrics FROM music m 
            INNER JOIN artist a on m.artist_id = a.id INNER JOIN lyrictable l 
            ON m.id = l.songid WHERE m.title LIKE '{target}%' OR m.artist_name LIKE '{target}%' 
            OR m.slug LIKE '{target}%' 
            UNION SELECT m.id, m.created_at as upload_time, m.title, 
            m.artist_id, m.artist_name,m.thumbnail as music_thumbnail, NULL As artist_thumbnail, 
            m.audio, m.slug as music_slug, NULL as artist_slug, 
            m.viewcount, l.lyrics 
            FROM music m INNER JOIN 
            lyrictable l ON l.songid = m.id WHERE 
            m.artist_id IS NULL AND(m.title LIKE '{target}%' 
            OR m.slug LIKE '{target}%' 
            OR m.artist_name LIKE '{target}%')";
            string sqlSelectArtist = @$"
            SELECT id, title, slug, thumbnail FROM artist 
            WHERE title LIKE '{target}%' OR slug LIKE '{target}%'";
            string sqlSelectAlbum = @$"
            SELECT id, title, slug, thumbnail FROM album 
            WHERE title LIKE '{target}%' OR slug LIKE '{target}%'";
            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();
            try
            {
                JToken user = JToken.Parse(JsonConvert.DeserializeObject(auth).ToString() ?? "{}");
                DataTable likedUserTable = new DataTable();
                string username = (string)user.SelectToken("username");
                string userId = (string)user.SelectToken("id");
                if ((user != null && userId != null && username != null) && UserController.checkUserExisted(username, userId, conn))
                {
                    string sqlSelectLikedUser = $@"
                    SELECT * FROM liketable WHERE userid={userId}";
                    SqlCommand selectLikedUserCmd = new SqlCommand(sqlSelectLikedUser, conn);
                    SqlDataReader selectLikedUserReader = selectLikedUserCmd.ExecuteReader();
                    likedUserTable.Load(selectLikedUserReader);
                    selectLikedUserReader.Close();
                }

                DataTable musicsTable = new DataTable();
                SqlCommand musicsCmd = new SqlCommand(sqlSelectMusic, conn);
                SqlDataReader musicsReader = musicsCmd.ExecuteReader();
                musicsTable.Load(musicsReader);
                musicsReader.Close();
                JToken musics = JToken.FromObject(musicsTable);

                JToken[] newResultMusic = musics.AsEnumerable().Select(music =>
                {

                    if (likedUserTable.AsEnumerable().Where(likedMusic => Convert.ToString(likedMusic["songid"]) == Convert.ToString(music["id"])).Any())
                    {

                        music["liked"] = true;
                        return music;
                    }
                    else
                    {
                        return music;
                    }
                }).ToArray();



                DataTable artistTable = new DataTable();
                SqlCommand artistCmd = new SqlCommand(sqlSelectArtist, conn);
                SqlDataReader artistReader = artistCmd.ExecuteReader();
                artistTable.Load(artistReader);


                DataTable albumTable = new DataTable();
                SqlCommand albumCmd = new SqlCommand(sqlSelectAlbum, conn);
                SqlDataReader albumReader = albumCmd.ExecuteReader();
                albumTable.Load(albumReader);

                artistReader.Close();
                albumReader.Close();

                return new JsonResult(new { musics = newResultMusic, artists = artistTable, albums = albumTable });

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
