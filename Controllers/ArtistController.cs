using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
    public class ArtistController : ControllerBase
    {
        private readonly IConfiguration _iconfiguration;

        public ArtistController(IConfiguration configuration)
        {
            _iconfiguration = configuration;
        }

        [HttpGet("artistinfo")]
        public JsonResult GetArtistInfo()
        {
            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();

            string sqlSelectArtist = @$"SELECT * FROM Artist ORDER BY created_at DESC";
            DataTable ArtistTable = new DataTable();
            SqlCommand cmd = new SqlCommand(sqlSelectArtist, conn);
            SqlDataReader reader = cmd.ExecuteReader();
            ArtistTable.Load(reader);

            reader.Close();
            conn.Close();

            return new JsonResult(ArtistTable);
        }

        [HttpGet("{slug}")]
        public JsonResult GetArtistBySlug(string? slug)
        {
            

            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();
            try
            {
                string sqlSelectArtist = @$"
                SELECT 
                m.id, m.created_at as upload_time, 
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
                FROM music m 
                INNER JOIN 
                artist a oN 
                m.artist_id = a.id 
                INNER JOIN lyrictable l ON l.songid = m.id WHERE a.slug ='{slug}'";
                DataTable selectArtistTable = new DataTable();
                SqlCommand selectArtistCmd = new SqlCommand(sqlSelectArtist, conn);
                SqlDataReader musicsByArtistReader = selectArtistCmd.ExecuteReader();
                selectArtistTable.Load(musicsByArtistReader);
                musicsByArtistReader.Close();
                JArray musics = new JArray();
                JToken selectArtistTableJToken = JToken.FromObject(selectArtistTable);
                if(selectArtistTable.Rows.Count > 0)
                {

                    JToken selectArtistRecord = JToken.FromObject(selectArtistTable)[0];
                    return new JsonResult(new { artist_id = selectArtistRecord.SelectToken("artist_id"), 
                        title = selectArtistRecord.SelectToken("artist_name"),
                        artist_slug = selectArtistRecord.SelectToken("artist_slug"),
                        artist_thumbnail = selectArtistRecord.SelectToken("artist_thumbnail"),
                        musics= selectArtistTable
                    });
                    
                }
                else
                {
                    string sqlSelectArtistOnly = @$"SELECT * FROM artist WHERE slug='{slug}'";
                    DataTable selectArtistOnlyTable = new DataTable();
                    SqlCommand selectArtistOnlyCmd = new SqlCommand(sqlSelectArtist, conn);
                    SqlDataReader musicsByArtistOnlyReader = selectArtistCmd.ExecuteReader();
                    selectArtistOnlyTable.Load(musicsByArtistOnlyReader);
                    musicsByArtistOnlyReader.Close();
                    if(selectArtistTable.Rows.Count > 0)
                    {

                        JToken selectArtistOnlyRecord = JToken.FromObject(selectArtistOnlyTable)[0];
                        return new JsonResult(new
                        {
                            artist_id = selectArtistOnlyRecord.SelectToken("artist_id"),
                            title = selectArtistOnlyRecord.SelectToken("artist_name"),
                            artist_slug = selectArtistOnlyRecord.SelectToken("artist_slug"),
                            artist_thumbnail = selectArtistOnlyRecord.SelectToken("artist_thumbnail")
                        });
                    }
                    else
                    {
                        return new JsonResult(new { error = new { message = "No info about this artist." } });
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
