using MoreNote.Logic.Database;
using MoreNote.Logic.Entity.ConfigFile;
using MoreNote.Logic.Service;
using Microsoft.EntityFrameworkCore;
using Autofac.Extensions.DependencyInjection;
using Autofac;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
   .ConfigureContainer<ContainerBuilder>(builder =>
   {
       builder.RegisterType<RandomImageService>();
       builder.RegisterType<ConfigFileService>().SingleInstance();
   });
// Add services to the container.
builder.Services.AddControllersWithViews();


var services = builder.Services;
ConfigFileService configFileService=new ConfigFileService();
WebSiteConfig config= configFileService.WebConfig;
//���ݿ�
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
var connection = config.PostgreSql.Connection;
services.AddEntityFrameworkNpgsql();
services.AddDbContextPool<DataContext>((serviceProvider, optionsBuilder) =>
{
    optionsBuilder.UseNpgsql(connection);
    optionsBuilder.UseInternalServiceProvider(serviceProvider);
    //���Ի����������SQL����̨��������ִ��ʱ�����10ms
});



//ͼƬ���·���
services.AddHostedService<MoreNoteWorkerService.UpdataImageURLWorker>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
