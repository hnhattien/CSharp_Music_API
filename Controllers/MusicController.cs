using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using MusicAppAPI.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Slugify;
using System.Security.Cryptography;

namespace MusicAppAPI.Controllers
{
    [Route("api/song")]
    [ApiController]
    public class MusicController : ControllerBase
    {
        private readonly IConfiguration _iconfiguration;

        public MusicController(IConfiguration configuration)
        {
            _iconfiguration = configuration;
        }

        [HttpGet("randomfetch/{num}")]
        public JsonResult GetMusicRandom(int? num)
        {
            string queryStr = @$"
                                 SELECT TOP {num} m.id, 
                                        m.created_at as upload_time, 
                                        m.title,
                                        m.artist_id, 
                                        m.artist_name, 
                                        m.thumbnail as music_thumbnail,
                                        m.audio,
                                        m.slug as music_slug,
                                        m.viewcount,
                                        l.lyrics FROM Music m 
                                        INNER JOIN Lyrictable l 
                                        ON m.id = l.songid 
                                        ORDER BY NEWID()";
            DataTable randomMusicTable = new DataTable();
            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();
            SqlCommand cmd = new SqlCommand(queryStr, conn);
            SqlDataReader reader = cmd.ExecuteReader();
            randomMusicTable.Load(reader);

            reader.Close();
            conn.Close();

            return new JsonResult(randomMusicTable);
        }

        [HttpPost("updateview")]
        public JsonResult UpdateView([FromBody] int? musicId)
        {
            if (musicId == null)
            {
                try
                {
                    string queryStr = @$"
                               UPDATE Music SET viewcount = viewcount+1 WHERE id={musicId}";
                    DataTable musicTable = new DataTable();
                    SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
                    conn.Open();
                    SqlCommand cmd = new SqlCommand(queryStr, conn);
                    SqlDataReader reader = cmd.ExecuteReader();
                    musicTable.Load(reader);

                    reader.Close();
                    conn.Close();
                    return new JsonResult(new { message = "Update view success" });
                }
                catch (Exception err)
                {
                    return new JsonResult(new { error = new { message = Convert.ToString(err) } });
                }    
            }
            else
            {
                return new JsonResult(new { error = new { message= "Please give a music id"} });
            }
           
        }

        [HttpGet("{slug}")]
        public JsonResult GetMusicBySlug(string? slug)
        {
            try
            {
                string sqlSelectMusic = @$"
            SELECT 
            m.id, 
            m.created_at as upload_time, 
            m.title, 
            m.artist_id, 
            m.artist_name, 
            m.thumbnail as music_thumbnail, 
            a.thumbnail as artist_thumbnail, 
            m.audio, 
            m.slug as music_slug, 
            a.slug as artist_slug, 
            m.viewcount 
            FROM Music m 
            INNER JOIN 
            Artist a ON a.id = m.artist_id 
            WHERE m.slug = '{slug}'
            UNION SELECT 
            m.id, 
            m.created_at as upload_time, 
            m.title, 
            m.artist_id, 
            m.artist_name, 
            m.thumbnail as music_thumbnail, 
            NULL as artist_thumbnail , 
            m.audio, 
            m.slug as music_slug, 
            NULL as artist_slug, 
            m.viewcount FROM music m 
            WHERE m.artist_id IS NULL AND m.slug = '{slug}'";

                string sqlSelectLyrics = @$"
            SELECT 
            l.lyrics, 
            u.displayedName as editLyricBy, 
            m.slug FROM lyrictable l 
            INNER JOIN Music m 
            ON l.songid = m.id 
            INNER JOIN UserTable u 
            ON l.userid = u.id 
            WHERE m.slug = '{slug}'";
                DataTable musicsTable = new DataTable();
                SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
                conn.Open();

                SqlCommand selectMusicsCmd = new SqlCommand(sqlSelectMusic, conn);
                SqlDataReader musicsReader = selectMusicsCmd.ExecuteReader();
                musicsTable.Load(musicsReader);
                
                if (musicsTable.Rows.Count > 0)
                {
                    DataTable lyricsTable = new DataTable();
                    SqlCommand selectLyricsCmd = new SqlCommand(sqlSelectLyrics, conn);
                    SqlDataReader lyricsReader = selectLyricsCmd.ExecuteReader();

                    lyricsTable.Load(lyricsReader);
                    musicsTable.Columns.Add("lyrics");
                    musicsTable.Columns.Add("editLyricBy");
                    DataRow music = musicsTable.Rows[0];


                    if (lyricsTable.Rows.Count > 0)
                    {
                        
                        music["lyrics"] = lyricsTable.Rows[0]["lyrics"];
                        music["editLyricBy"] = lyricsTable.Rows[0]["editLyricBy"];
                    }
                    else
                    {
                        music["lyrics"] = "";
                        music["editLyricBy"] = null;
                    }
                    lyricsReader.Close();
                    musicsReader.Close();
                    conn.Close();
                    return new JsonResult(Program.GetFirstRowFromDataTableAsJson(music.Table));
                }
                else
                {
                    conn.Close();
                    return new JsonResult(new { });
                }
            }
            catch(Exception err)
            {
                return new JsonResult(
                    new { error = new { message = Convert.ToString(err)}}
                );
            }
            
        }

        [HttpPost("heartaction")]
        public JsonResult HeartAction([FromBody] dynamic reqBody, [FromHeader] string auth)
        {
            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();
            try
            {
                string songId = reqBody.songid;
                JToken user = JToken.Parse(JsonConvert.DeserializeObject(auth).ToString() ?? "{}");
                string username = (string)user.SelectToken("username");
                string userId = (string)user.SelectToken("id");
                if ((user != null && userId != null && username != null) && UserController.checkUserExisted(username, userId, conn))
                {

                    string sqlSelectCheck = @$"SELECT * FROM liketable 
                                            WHERE 
                                            userid={userId} 
                                            AND
                                            songid={reqBody.songid}";
                    string sqlSelectAffectedMusic = @$"
                SELECT 
                m.id, 
                m.created_at as upload_time, 
                m.title, 
                m.artist_id, 
                m.artist_name, 
                m.thumbnail as music_thumbnail, 
                a.thumbnail as artist_thumbnail, 
                m.audio, 
                m.slug as music_slug, 
                a.slug as artist_slug, 
                m.viewcount, 
                l.lyrics 
                FROM Music m 
                INNER JOIN Artist a 
                ON 
                a.id = m.artist_id 
                INNER JOIN 
                lyrictable l 
                ON m.id = l.songid 
                WHERE m.id ={songId}
                UNION 
                SELECT 
                m.id, 
                m.created_at as upload_time, 
                m.title, 
                m.artist_id, 
                m.artist_name, 
                m.thumbnail as music_thumbnail, 
                NULL as artist_thumbnail , 
                m.audio, 
                m.slug as music_slug, 
                NULL as artist_slug, 
                m.viewcount, 
                l.lyrics 
                FROM Music m 
                INNER JOIN Lyrictable l ON l.songid = m.id WHERE m.artist_id IS NULL AND m.id ={songId}
                UNION
				SELECT 
                m.id, 
                m.created_at as upload_time, 
                m.title, 
                m.artist_id, 
                m.artist_name, 
                m.thumbnail as music_thumbnail, 
                NULL as artist_thumbnail , 
                m.audio, 
                m.slug as music_slug, 
                NULL as artist_slug, 
                m.viewcount, 
                NULL as lyrics 
                FROM Music m WHERE m.id={songId}";

                    DataTable sqlSelectCheckTable = new DataTable();


                    SqlCommand selectCheckCmd = new SqlCommand(sqlSelectCheck, conn);
                    SqlDataReader musicsReader = selectCheckCmd.ExecuteReader();
                    sqlSelectCheckTable.Load(musicsReader);
                    musicsReader.Close();
                    if (sqlSelectCheckTable.Rows.Count == 0)
                    {
                        try
                        {
                            string sqlInsertLike = @$"INSERT INTO liketable(userid, songid) VALUES({userId}, {songId})";
                            SqlCommand insertLikeCmd = new SqlCommand(sqlInsertLike, conn);
                            insertLikeCmd.ExecuteNonQuery();
                        }
                        catch (Exception err)
                        {
                            return new JsonResult(new { error = new { message = "Invalid User Info" } });
                        }

                        DataTable affectedMusicTable = new DataTable();
                        SqlCommand selectAffectedMusic = new SqlCommand(sqlSelectAffectedMusic, conn);
                        SqlDataReader affectedMusicReader = selectAffectedMusic.ExecuteReader();
                        affectedMusicTable.Load(affectedMusicReader);
                        var affectedMusic = Program.GetFirstRowFromDataTableAsJson(affectedMusicTable);
                        affectedMusic["liked"] = true;
                        affectedMusicReader.Close();
                        return new JsonResult(new { message = "Liked", isLike = true, music = affectedMusic });
                    }
                    else
                    {
                        string sqlRemoveLike = @$"DELETE FROM liketable WHERE userid ={userId} AND songid ={songId}";
                        SqlCommand removeLikeCmd = new SqlCommand(sqlRemoveLike, conn);
                        removeLikeCmd.ExecuteNonQuery();

                        DataTable affectedMusicTable = new DataTable();
                        SqlCommand selectAffectedMusic = new SqlCommand(sqlSelectAffectedMusic, conn);
                        SqlDataReader affectedMusicReader = selectAffectedMusic.ExecuteReader();
                        affectedMusicTable.Load(affectedMusicReader);
                        var affectedMusic = Program.GetFirstRowFromDataTableAsJson(affectedMusicTable);
                        affectedMusicReader.Close();
                        return new JsonResult(new { message = "Unliked", isLike = false, music = affectedMusic });
                    }
                    
                }
                else
                {
                    return new JsonResult(
                        new { isRequireLogin = true, error = new { message = "You must to login to perform this action." } }
                        );

                }
            }
            catch(Exception err)
            {
                return new JsonResult(new { error = new { message = Convert.ToString(err) } });
            }
            finally
            {
                conn.Close();
            }
             
        }

        [HttpPost("upload")]
        public JsonResult UploadSong([FromBody] dynamic reqBody, [FromHeader] string auth)
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
                    string songname = reqBody.songname;
                    string artistname = reqBody.artistname;
                    string category = reqBody.catid;
                    string thumbnailfilename = reqBody.thumbnailfilename;
                    string songfilename = reqBody.songfilename;
                    string songThumbnailFileBase64 = reqBody.songThumbnailFileBase64;
                    string songThumbnailFileBase64Rawdata = Convert.ToString(songThumbnailFileBase64).Split(";base64,").Last();
                    string songFileBase64 = reqBody.songFileBase64;
                    string songFileBase64Rawdata = Convert.ToString(songFileBase64).Split(";base64,").Last();
                    System.IO.File.WriteAllBytes(@$"wwwroot/upload/musics/thumbnails/{thumbnailfilename}", Convert.FromBase64String(songThumbnailFileBase64Rawdata));
                    System.IO.File.WriteAllBytes($@"wwwroot/upload/musics/audio/{songfilename}", Convert.FromBase64String(songFileBase64Rawdata));
                    bool isDuplicate = true;
                    string slugData = "";
                    while (isDuplicate)
                    {
                        SlugHelper slugify = new SlugHelper();
                        RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider();

                        slugData = $"{slugify.GenerateSlug(songname)}.{Program.GenerateRandomHexNumber(16)}";
                        string sqlSelectSlug = @$"SELECT * FROM Music WHERE slug = '{slugData}'"; //Check duplicate
                        

                            DataTable selectCheckSlugTable = new DataTable();
                            

                            SqlCommand selectCheckCmd = new SqlCommand(sqlSelectSlug, conn);
                            SqlDataReader slugReader = selectCheckCmd.ExecuteReader();
                            selectCheckSlugTable.Load(slugReader);

                            if (selectCheckSlugTable.Rows.Count == 0)
                            {
                                isDuplicate = false;
                            }

                        slugReader.Close();
                        


                    }
                    string sqlInsertSong = @$"
                INSERT INTO 
                Music(title, slug, artist_name, cat_id, thumbnail, audio) VALUES(N'{songname}','{slugData}',N'{artistname}','{category}',N'{thumbnailfilename}',N'{songfilename}')";

                    SqlCommand sqlInsertSongCmd = new SqlCommand(sqlInsertSong, conn);
                    sqlInsertSongCmd.ExecuteNonQuery();
                    
                    return new JsonResult(new { message = "Upload song success" });
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
                
                return new JsonResult(new { error = new { message= Convert.ToString(err) } });
            }
            finally {
                conn.Close();
            }
        }

        [HttpGet("category/{slug}")]
        public JsonResult GetMusicsByCategory(string? slug)
        {
            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();
            try
            {
                string sqlSelectMusicsByCategory = @$"
                SELECT 
                m.id, 
                m.created_at as upload_time, 
                m.title, 
                m.artist_id, 
                m.artist_name, 
                m.thumbnail as music_thumbnail, 
                a.thumbnail as artist_thumbnail, 
                m.audio, 
                m.slug as music_slug, 
                a.slug as artist_slug, 
                m.viewcount, 
                l.lyrics, 
                c.title as category_name 
                FROM music m 
                INNER JOIN category c 
                ON c.id = m.cat_id 
                INNER JOIN artist a 
                ON m.artist_id = a.id 
                INNER JOIN lyrictable l 
                ON l.songid = m.id 
                WHERE c.slug ='{slug}'
                UNION SELECT 
                m.id, 
                m.created_at as upload_time, 
                m.title, 
                m.artist_id, 
                m.artist_name, 
                m.thumbnail as music_thumbnail, 
                NULL as artist_thumbnail, 
                m.audio, 
                m.slug as music_slug, 
                NULL as artist_slug, 
                m.viewcount, 
                l.lyrics, 
                c.title as category_name 
                FROM music m 
                INNER JOIN category c 
                ON c.id = m.cat_id 
                INNER JOIN artist a 
                ON m.artist_id IS NULL 
                INNER JOIN lyrictable l 
                ON m.id = l.songid WHERE c.slug ='{slug}'";
                DataTable musicsByCategoryTable = new DataTable();
                SqlCommand selectMusicsByCategory = new SqlCommand(sqlSelectMusicsByCategory, conn);
                SqlDataReader musicsByCategoryReader = selectMusicsByCategory.ExecuteReader();
                musicsByCategoryTable.Load(musicsByCategoryReader);
                musicsByCategoryReader.Close();
                return new JsonResult(musicsByCategoryTable);
            }
            catch(Exception err)
            {
                
                return new JsonResult(new { error = new { message = Convert.ToString(err) } });
            }
            finally
            {
                
                conn.Close();
            }
        }

        [HttpGet("album/{slug}")]
        public JsonResult GetMusicsByAlbum(string? slug)
        {
            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();
            try
            {
                string sqlSelectMusicsByAlbum = @$"
                SELECT 
                m.id, 
                m.created_at as upload_time,
                m.title,
                m.artist_id,
                m.artist_name,
                m.thumbnail as music_thumbnail, 
                a.thumbnail as artist_thumbnail, 
                m.audio, 
                m.slug as music_slug, 
                a.slug as artist_slug, 
                m.viewcount, 
                l.lyrics, 
                al.title as album_name 
                FROM music m 
                INNER JOIN album al 
                ON al.cat_id = m.cat_id 
                INNER JOIN artist a 
                ON m.artist_id = a.id
                INNER JOIN lyrictable l 
                ON l.songid = m.id 
                WHERE al.slug = '{slug}'
                UNION 
                SELECT 
                m.id, 
                m.created_at as upload_time, 
                m.title, 
                m.artist_id, 
                m.artist_name, 
                m.thumbnail as music_thumbnail, 
                NULL as artist_thumbnail , 
                m.audio, 
                m.slug as music_slug, 
                NULL as artist_slug, 
                m.viewcount, 
                l.lyrics, 
                al.title as album_name 
                FROM music m 
                INNER JOIN album al 
                ON al.cat_id = m.cat_id 
                INNER JOIN artist a 
                ON m.artist_id IS NULL 
                INNER JOIN lyrictable l 
                ON l.songid = m.id WHERE al.slug='{slug}'
                UNION SELECT 
                m.id, 
                m.created_at as upload_time,
                m.title, 
                m.artist_id, 
                m.artist_name, 
                m.thumbnail as music_thumbnail, 
                a.thumbnail as artist_thumbnail, 
                m.audio, 
                m.slug as music_slug, 
                a.slug as artist_slug, 
                m.viewcount, 
                l.lyrics, 
                al.title as album_name 
                FROM music m 
                INNER JOIN album al 
                ON al.artist_id = m.artist_id 
                INNER JOIN artist a 
                ON m.artist_id = a.id 
                INNER JOIN lyrictable l 
                ON l.songid = m.id 
                WHERE al.slug = '{slug}'
                UNION 
                SELECT 
                m.id, 
                m.created_at as upload_time, 
                m.title, 
                m.artist_id, 
                m.artist_name, 
                m.thumbnail as music_thumbnail, 
                NULL as artist_thumbnail , 
                m.audio, 
                m.slug as music_slug, 
                NULL as artist_slug, 
                m.viewcount, 
                l.lyrics, 
                al.title as album_name 
                FROM music m 
                INNER JOIN album al 
                ON al.artist_id = m.artist_id 
                INNER JOIN artist a 
                ON m.artist_id IS NULL 
                INNER JOIN lyrictable l 
                ON m.id=l.songid WHERE al.slug='{slug}'";
                DataTable musicsByAlbumTable = new DataTable();
                SqlCommand selectMusicsByAlbum = new SqlCommand(sqlSelectMusicsByAlbum, conn);
                SqlDataReader musicsByAlbumReader = selectMusicsByAlbum.ExecuteReader();
                musicsByAlbumTable.Load(musicsByAlbumReader);
                musicsByAlbumReader.Close();
                return new JsonResult(musicsByAlbumTable);
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

        [HttpPost("updatelyrics")]
        public JsonResult UpdateLyrics([FromBody] dynamic reqBody, [FromHeader] string auth)
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
                    string sqlUpdateLyrics = @$"UPDATE lyrictable SET lyrics =N'{reqBody.lyrics}', userid ={userId} WHERE songid ={reqBody.songId}";

                    SqlCommand updateLyricsCmd = new SqlCommand(sqlUpdateLyrics, conn);
                    updateLyricsCmd.ExecuteNonQuery();

                    return new JsonResult(new { message = "Update lyrics success" });
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
    }
}
