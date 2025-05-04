using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RestTestApp;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

//#if !NET6_0
    builder.Services.AddSwaggerGen();
//#endif

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
//#if !NET6_0
    app.UseSwagger();
    app.UseSwaggerUI();
//#endif
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


var measurmentsCreator = new MeasurmentsCreator("Demo1", RP.Prober.Singleton.HeaderType.Row, 1);
measurmentsCreator.StartGenerateLines();

var measurmentsCreator2 = new MeasurmentsCreator("Demo2", RP.Prober.Singleton.HeaderType.Column, 7);
measurmentsCreator2.StartGenerateLines();

var tt = new TT();
Task.Run(() => tt.PubSub_N_Records());
Task.Run(() => tt.PubSub_N_Records_UpdateAsInsertForLowLatency());

app.Run();
