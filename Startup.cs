using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicAppAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        private string AllowOrignName = "allowOrigin";
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            
            services.AddCors((options) =>
            {
                options.AddPolicy(AllowOrignName, (builder) =>builder.WithOrigins("http://localhost:3000", "192.168.1.9:3000").AllowAnyMethod().AllowAnyHeader().SetIsOriginAllowed((host) => true).AllowCredentials());
            });
            services.AddControllers();

            services.AddMvc();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            //JSON Serialize
            services.Configure<JsonOptions>(options =>
            {
                options.JsonSerializerOptions.WriteIndented = true;
            });

            services.AddControllersWithViews()
            .AddNewtonsoftJson(
                options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            ).AddNewtonsoftJson(
                options => options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver()
             ) ;

            services.AddMvc().ConfigureApiBehaviorOptions(options => {
                options.SuppressInferBindingSourcesForParameters = true;
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            
            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors(AllowOrignName);
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseFileServer();
            app.UseStaticFiles();

            
        }
    }
}
