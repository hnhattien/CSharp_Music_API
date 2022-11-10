using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
    public class AlbumController : ControllerBase
    {
        private readonly IConfiguration _iconfiguration;

        public AlbumController(IConfiguration configuration)
        {
            _iconfiguration = configuration;
        }

        [HttpGet]
        public JsonResult Get()
        {
            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();

            string sqlSelectAlbum = @$"SELECT * FROM album ";
            DataTable AlbumTable = new DataTable();
            SqlCommand cmd = new SqlCommand(sqlSelectAlbum, conn);
            SqlDataReader reader = cmd.ExecuteReader();
            AlbumTable.Load(reader);

            reader.Close();
            conn.Close();

            return new JsonResult(AlbumTable);


        }
    }
}
