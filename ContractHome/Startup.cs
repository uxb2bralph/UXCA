using Microsoft.AspNetCore.Authentication.Cookies;
using ContractHome.Controllers.Filters;
using ContractHome.Helper;
using CommonLib.Core.Utility;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using ContractHome.Properties;
using ContractHome.Models.Email;
using ContractHome.Models.Email.Template;
using CommonLib.DataAccess;
using ContractHome.Models.DataEntity;
using ContractHome.Models.Helper;

namespace WebHome
{
    public class Startup
    {
        //public static IConfigurationSection? Properties { get; private set; }
        public static IWebHostEnvironment? Environment { get; private set; }
        public static  IConfiguration? GlobalConfiguration { get; private set; }

        public static String MapPath(String path, bool isStatic = true)
        {
            return isStatic
                ? Path.Combine(Environment?.WebRootPath ?? "", path.Replace("~/", "")
                    .TrimStart('/').Replace('/', Path.DirectorySeparatorChar))
                : Path.Combine(Environment?.ContentRootPath ?? "", path.Replace("~/", "")
                    .TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            GlobalConfiguration = configuration;
            //Properties = Configuration.GetSection("WebHome");
            _ = new CommonLib.Core.Startup(Configuration);

        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //var webHome = Configuration.GetSection("WebHome");

            //services.AddControllersWithViews();
            services.AddMemoryCache();

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(Settings.Default.SessionTimeoutInMinutes);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            services.AddMvc(config =>
            {
                config.Filters.Add(new SampleResultFilter());
                config.Filters.Add(new ExceptionFilter());
            }).ConfigureApiBehaviorOptions(options =>
            {
                options.SuppressMapClientErrors = true;
                options.SuppressModelStateInvalidFilter = true;
            })
            .AddRazorRuntimeCompilation();
                
            //services.AddDbContext<BFDataContext>(options =>
            //    {
            //        options
            //            .UseLazyLoadingProxies()
            //            .UseSqlServer(Configuration.GetConnectionString("BFDbConnection"),
            //                 sqlOptions => sqlOptions.CommandTimeout((int)TimeSpan.FromMinutes(30).TotalSeconds));
            //    });

            //從組態讀取登入逾時設定
            //註冊 CookieAuthentication，Scheme必填
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(option =>
            {
                //或許要從組態檔讀取，自己斟酌決定
                option.LoginPath = new PathString(Settings.Default.LoginUrl);//登入頁
                option.LogoutPath = new PathString(Settings.Default.LogoutUrl);//登出Action
                //用戶頁面停留太久，登入逾期，或Controller的Action裡用戶登入時，也可以設定↓
                option.ExpireTimeSpan = TimeSpan.FromMinutes(Settings.Default.LoginExpireMinutes);//沒給預設14天
                //↓資安建議false，白箱弱掃軟體會要求cookie不能延展效期，這時設false變成絕對逾期時間
                //↓如果你的客戶反應明明一直在使用系統卻容易被自動登出的話，你再設為true(然後弱掃policy請客戶略過此項檢查) 
                option.SlidingExpiration = true;
            });

            services.AddControllersWithViews(options => {
                //↓和CSRF資安有關，這裡就加入全域驗證範圍Filter的話，待會Controller就不必再加上[AutoValidateAntiforgeryToken]屬性
                //options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            });

            services.AddScoped<IViewRenderService, ViewRenderService>();

            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            services.AddOptions<MailSettings>().BindConfiguration("MailSettings");
            services.AddScoped<IMailService, MailService>();
            services.AddScoped<EmailFactory>();
            services.AddScoped<EmailBody>();
            services.AddScoped<ContractServices>();
            //services.AddSingleton<IRazorViewToStringRenderer, RazorViewToStringRenderer>();
            //services.AddSingleton<GenericManager<DCDataContext>>();
            //services.AddScoped<ContractRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                //app.UseExceptionHandler("/Home/Error");
                app.UseDeveloperExceptionPage();
                app.UseHsts();
            }
            app.UseStaticFiles();

            //app.UsePathBase("/ContractHome");
            app.UseRouting();

            //app.UseAuthorization();
            //留意寫Code順序，先執行驗證...
            app.UseAuthentication();
            app.UseAuthorization();//Controller、Action才能加上 [Authorize] 屬性
            app.UseSession();

            app.Use(next =>
                context =>
                {
                    context.Request.EnableBuffering();
                    return next(context);
                });

            app.UseEndpoints(endpoints =>
            {

                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                    name: "actionName",
                    pattern: "{controller}/{*actionName}",
                    defaults: new { action = "HandleUnknownAction" });

            });


            //call ConfigureLogger in a centralized place in the code
            ApplicationLogging.ConfigureLogger(loggerFactory);
            //set it as the primary LoggerFactory to use everywhere
            ApplicationLogging.LoggerFactory = loggerFactory;
            Environment = env;
        }
    }
}
