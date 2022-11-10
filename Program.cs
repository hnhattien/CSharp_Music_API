using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;

namespace MusicAppAPI
{
    public class Program
    {
        public static SqlConnection GetSqlConnection(string sqlConnectionString)
        {
            return new SqlConnection(sqlConnectionString);
        }

        public static string GenerateRandomHexNumber(int digits)
        {
            Random random = new Random();
            byte[] buffer = new byte[digits / 2];
            random.NextBytes(buffer);
            string result = String.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
            if (digits % 2 == 0)
                return result;
            return result + random.Next(16).ToString("X");
        }

        public static string ComputeHash(string input, HashAlgorithm algorithm)
        {
            Byte[] inputBytes = Encoding.UTF8.GetBytes(input);

            Byte[] hashedBytes = algorithm.ComputeHash(inputBytes);

            return BitConverter.ToString(hashedBytes);
        }

        public static string RandomBytesHexString(int length = 30)
        {
            Random random = new Random();
            var bytes = new Byte[length];
            random.NextBytes(bytes);
            var hexArray = Array.ConvertAll(bytes, x => x.ToString("X2"));
            string hexStr = String.Concat(hexArray);

            return hexStr.ToLower();
        }
        
        public static async Task sendEmail(dynamic reqData)
        {
            DotNetEnv.Env.Load();
            var message = new MailMessage();
            message.To.Add(new MailAddress(reqData["toName"].ToString() + " <" + reqData["toEmail"].ToString() + ">"));
            message.From = new MailAddress("Admin <daike0770@gmail.com>");
            message.Subject = reqData["subject"].ToString();
            message.Body = @$"<h1>Reset Password For SE447-E Music App</h1>
            <h1><a href='http://{reqData["hostname"]}:3000/resetpassword?sel=${reqData["selector"]}&token=${reqData["token"]}'>Click here to reset password.</a></h1>
            ";
            message.IsBodyHtml = true;

            SmtpClient smtpclient = new SmtpClient();
            smtpclient.Port = 587;
            smtpclient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpclient.UseDefaultCredentials = false;
            smtpclient.Host = "smtp.gmail.com";
            smtpclient.EnableSsl = true;
            smtpclient.UseDefaultCredentials = false;
            smtpclient.Credentials = new System.Net.NetworkCredential()
            {
                UserName = "daike0770@gmail.com",
                Password = Environment.GetEnvironmentVariable("EMAIL_PASSWORD")
            };
            
            await smtpclient.SendMailAsync(message);
            await Task.FromResult(0);
        }

        public static JsonResult CheckAccountExisted(dynamic username, dynamic email, dynamic role, SqlConnection conn)
        {
            string sqlSelectUser = @$"SELECT username, email from UserTable WHERE (username ='{username}' OR email ='{email}') AND role = '{role}'";
            // const sqlSelectEmail = `SELECT id from user WHERE email=?`;
            DataTable sqlSelectUserTable = new DataTable();
            SqlCommand sqlSelectUserCmd = new SqlCommand(sqlSelectUser, conn);
            SqlDataReader sqlSelectUserReader = sqlSelectUserCmd.ExecuteReader();
            sqlSelectUserTable.Load(sqlSelectUserReader);
            
            if (sqlSelectUserTable.Rows.Count > 0)
            {
                JToken sqlSelectUserRecord = JToken.FromObject(sqlSelectUserTable)[0];
                if ((string)username == (string)sqlSelectUserRecord.SelectToken("username"))
                {
                    return new JsonResult(new { error = new { message = "Username has existed." } });
                }
                else
                {
                    return new JsonResult(new { error = new { message = "Signup isn't successfull. Has an account that have been registered by this email." } });
                }

            }
            else
            {
                return new JsonResult(new { ok = true });
            }
        }
        public static JToken GetFirstRowFromDataTableAsJson(DataTable data){
            try
            {
                return JToken.FromObject(data)[0];
            }catch(Exception err)
            {
                return JToken.FromObject(new { error = new { message = Convert.ToString(err)  } });
            }
        }
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
