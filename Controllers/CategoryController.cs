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
    public class CategoryController : ControllerBase
    {
        private readonly IConfiguration _iconfiguration;

        public CategoryController(IConfiguration configuration)
        {
            _iconfiguration = configuration;
        }

        [HttpGet]
        public JsonResult Get()
        {
            SqlConnection conn = Program.GetSqlConnection(_iconfiguration.GetConnectionString("MusicApp"));
            conn.Open();
           
            string sqlSelectCategory = @$"SELECT * FROM category WHERE slug IS NOT NULL";
            DataTable categoryTable = new DataTable();
            SqlCommand cmd = new SqlCommand(sqlSelectCategory, conn);
            SqlDataReader reader = cmd.ExecuteReader();
            categoryTable.Load(reader);

            reader.Close();
            conn.Close();

            return new JsonResult(categoryTable);
            
            
        }
    }
}
